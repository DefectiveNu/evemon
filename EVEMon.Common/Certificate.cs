﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace EVEMon.Common
{
    /// <summary>
    /// Represents a certificate grade.
    /// </summary>
    public enum CertificateGrade
    {
        Basic = 0,
        Standard = 1,
        Improved = 2,
        Elite = 3
    }

	/// <summary>
    /// Represents a certificate.
    /// </summary>
    /// <remarks>
    /// Every category (i.e. "Business and Industry") contains
    /// certificate classes (i.e. "Production Manager") which
    /// contain certificates (i.e. "Production Manager Basic").
    /// </remarks>
    public sealed class Certificate
    {
        private static readonly Dictionary<string, List<Certificate>> sm_shipRecomms = new Dictionary<string, List<Certificate>>();

        #region PrereqCertificate
        /// <summary>
        /// Structure used during initialization before we can resolve names
        /// </summary>
        private struct TempPrereqCertificate
        {
            public readonly string Name;
            public readonly CertificateGrade Grade;

            public TempPrereqCertificate(string name, CertificateGrade grade)
            {
                this.Name = name;
                this.Grade = grade;
            }
        }
        #endregion

        private List<TempPrereqCertificate> m_tempPrereqCertificates = new List<TempPrereqCertificate>();
        private readonly List<Certificate> m_prereqCertificates = new List<Certificate>();
        private readonly Dictionary<StaticSkill, int> m_prereqSkillsLevels = new Dictionary<StaticSkill, int>();
        private readonly List<string> m_recomForShips = new List<string>();

        public readonly int ID;
        public readonly string Description;
        public readonly CertificateGrade Grade;
        public readonly CertificateClass Class;

        /// <summary>
        /// Constructor from XML
        /// </summary>
        /// <param name="certClass"></param>
        /// <param name="element"></param>
        internal Certificate(CertificateClass certClass, XmlElement element)
        {
            this.Class = certClass;
            this.Description = element.GetAttribute("descr");
            this.ID = Int32.Parse(element.GetAttribute("id"));
            this.Grade = GetGrade(element.GetAttribute("grade"));

            if (element.HasChildNodes)
            {
                foreach (XmlElement child in element.ChildNodes)
                {
                    if (child.Name == "requires")
                    {
                        string name = child.GetAttribute("name");
                        string type = child.GetAttribute("type");
                        string level = child.GetAttribute("level");

                        // If it's a skill
                        if (String.CompareOrdinal(type, "type") == 0)
                        {
                            var skill = StaticSkill.GetStaticSkillByName(String.Intern(name));
                            if (skill != null)
                            {
                                this.m_prereqSkillsLevels[skill] = Int32.Parse(level);
                            }
                        }
                        else
                        {
                            this.m_tempPrereqCertificates.Add(new TempPrereqCertificate(String.Intern(name), GetGrade(level)));
                        }
                    }
                    else if (child.Name == "recommendation" && child.HasAttribute("ship"))
                    {
                        string name = child.GetAttribute("ship");
                        this.m_recomForShips.Add(name);
                        if (!sm_shipRecomms.ContainsKey(name))
                            sm_shipRecomms[name] = new List<Certificate>();
                        sm_shipRecomms[name].Add(this);
                    }
                }
            }
        }

        /// <summary>
        /// Completes the initialization by resolving prerequisites names
        /// </summary>
        /// <param name="classes"></param>
        internal void Complete(Dictionary<string, CertificateClass> classes)
        {
            foreach (var prereq in this.m_tempPrereqCertificates)
            {
                var certClass = classes[prereq.Name];
                var cert = certClass[prereq.Grade];
                if (cert != null) this.m_prereqCertificates.Add(cert);
            }
            this.m_tempPrereqCertificates = null;
        }

        /// <summary>
        /// Gets the grade from the provided grade key.
        /// </summary>
        /// <remarks>
        /// No need to previously intern the key, it will be interned in this method
        /// </remarks>
        /// <param name="key"></param>
        /// <returns></returns>
        private static CertificateGrade GetGrade(string key)
        {
            switch (String.Intern(key))
            {
                case "Basic":
                    return CertificateGrade.Basic;
                case "Standard":
                    return CertificateGrade.Standard;
                case "Improved":
                    return CertificateGrade.Improved;
                case "Elite":
                    return CertificateGrade.Elite;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Try to update the certificate's status for the given
        /// character with the given statuses dictionary. The
        /// dictionary is updated if the operation successes.
        /// </summary>
        /// <remarks>
        /// <para>This method has been designed to be used by
        /// <see cref="CharacterInfo.AssignFromSerializableCharacterSheet"/>.
        /// It assumes the statuses dictionary at least contains
        /// correct statuses for the granted certificates.</para>
        /// <para>To update its status, a certificate needs to have its
        /// prerequisites statuses. Therefore, it can fail and return
        /// false if not all prerequsiite's status were defined.</para>
        /// </remarks>
        /// <param name="character">The character to update from.</param>
        /// <param name="statuses">The dictionary providing the status of
        /// every certificate. This dictionary is updated when the operation
        /// is successful.</param>
        /// <returns>True if the status could be defined (if all
        /// pererequisites' statuses were defined), false otherwise.</returns>
        internal bool TryUpdateCertificateStatus(CharacterInfo character, IDictionary<Certificate, CertificateStatus> statuses)
        {

            CertificateStatus oldStatus;
            if (statuses.TryGetValue(this, out oldStatus) && oldStatus == CertificateStatus.Granted) return true;

            bool claimable = true;
            bool noPrereq = true;

            // Scan prerequisite certfiicates
            foreach (var prereqCert in m_prereqCertificates)
            {
                // Status not defined yet ? Then, we quit
                CertificateStatus status;
                if (!statuses.TryGetValue(prereqCert, out status)) return false;

                // Claimable only if every prereq certificate has been granted
                claimable &= (status == CertificateStatus.Granted);

                // Untrainable if no prereq is satisfied
                noPrereq &= (status == CertificateStatus.Untrained | status == CertificateStatus.PartiallyTrained);
            }

            // Scan prerequisite skills
            foreach (var prereqSkill in m_prereqSkillsLevels)
            {
                var skill = character.AllSkillsByTypeID[prereqSkill.Key.Id];

                // Claimable only if the skill's level is grater or equal than the minium level
                claimable &= (skill.LastConfirmedLvl >= prereqSkill.Value);

                // Untrainable if no prereq is satisfied
                noPrereq &= (skill.LastConfirmedLvl < prereqSkill.Value);
            }

            if (claimable) statuses[this] = CertificateStatus.Claimable;
            else if (noPrereq) statuses[this] = CertificateStatus.Untrained;
            else statuses[this] = CertificateStatus.PartiallyTrained;

            return true;
        }

        /// <summary>
        /// Gets the prerequisite skills
        /// </summary>
        public IEnumerable<StaticSkill.Prereq> PrerequisiteSkills
        {
            get
            {
                foreach (var prereq in m_prereqSkillsLevels)
                {
                    var skill = new StaticSkill.Prereq(prereq.Key.Name, prereq.Value);
                    skill.SetSkill(prereq.Key);
                    yield return skill;
                }
            }
        }

        /// <summary>
        /// Gets all the prerequisite skills, including the ones from
        /// prerequisite certificates. However, it does not include the
        /// skill's prerequisites.
        /// </summary>
        public IEnumerable<StaticSkill.Prereq> AllPrerequisiteSkills
        {
            get
            {
                var skillsToTrain = new List<StaticSkill.Prereq>();
                GatherSkillsToTrain(skillsToTrain);
                return skillsToTrain;
            }
        }

        /// <summary>
        /// Gets the prerequisite certificates
        /// </summary>
        public IEnumerable<Certificate> PrerequisiteCertificates
        {
            get { return m_prereqCertificates; }
        }

        /// <summary>
        /// Get all ships this certificate is recommended for
        /// </summary>
        public IEnumerable<Ship> RecommendedForShips
        {
            get
            {
                foreach (string name in m_recomForShips)
                {
                    yield return Ship.GetShip(name);
                }
            }
        }

        /// <summary>
        /// Get all certificates that are recommended for a specific ship
        /// </summary>
        /// <param name="name">name of the ship</param>
        /// <returns>a possibly empty List</returns>
        public static List<Certificate> GetCertsRecommendedForShip(string name)
        {
            if (!sm_shipRecomms.ContainsKey(name))
                return new List<Certificate>();
            return sm_shipRecomms[name];
        }

        /// <summary>
        /// Checks whether the provided skill is an immediate prerequisite
        /// </summary>
        /// <param name="skill">The skill to test</param>
        /// <param name="neededLevel">When this skill is an immediate
        /// prerequsiite, this parameter will hold the required level</param>
        /// <returns></returns>
        public bool HasAsImmediatePrerequisite(Skill skill, out int neededLevel)
        {
            return this.m_prereqSkillsLevels.TryGetValue(skill.StaticSkill, out neededLevel);
        }

        /// <summary>
        /// Checks whether the provided certificate is an immediate prerequisite
        /// </summary>
        /// <param name="certificate">The certificate to test</param>
        /// <returns></returns>
        public bool HasAsImmediatePrerequisite(Certificate certificate)
        {
            return this.m_prereqCertificates.Contains(certificate);
        }

        /// <summary>
        /// Gets the required training time for the provided character to train this certificate
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public TimeSpan ComputeTrainingTime(CharacterInfo character)
        {
            List<Pair<Skill, int>> skillsToTrain = new List<Pair<Skill,int>>();
            foreach(var prereq in AllPrerequisiteSkills) skillsToTrain.Add(new Pair<Skill,int>(character.GetSkill(prereq.Name), prereq.Level));
            return character.GetTrainingTimeToMultipleSkills(skillsToTrain);
        }

        /// <summary>
        /// Gathes all the skills to train, including the ones for the
        /// prerequisite certificates, and store them in the provided list.
        /// </summary>
        /// <param name="character">The trainign character</param>
        /// <param name="skills">The skills list where to store the skills</param>
        private void GatherSkillsToTrain(List<StaticSkill.Prereq> skills)
        {
            foreach (var prereqSkill in this.PrerequisiteSkills)
            {
                skills.Add(prereqSkill);
            }
            foreach (var prereqCert in this.PrerequisiteCertificates)
            {
                prereqCert.GatherSkillsToTrain(skills);
            }
        }

        /// <summary>
        /// Converts the certificate instance to a String representation
        /// </summary>
        /// <returns>Representation of certificate</returns>
        public override string ToString()
        {
            return this.Class.Name + " " + this.Grade.ToString();
        }
    }
}
