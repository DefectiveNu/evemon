﻿using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a corporation's sheet. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPICorporationSheet
    {
        private readonly Collection<SerializableDivision> m_divisions;
        private readonly Collection<SerializableWalletDivision> m_walletDivisions;

        public SerializableAPICorporationSheet()
        {
            m_divisions = new Collection<SerializableDivision>();
            m_walletDivisions = new Collection<SerializableWalletDivision>();
        }

        [XmlElement("corporationID")]
        public long ID { get; set; }

        [XmlElement("corporationName")]
        public string NameXml
        {
            get { return Name; }
            set { Name = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("ticker")]
        public string Ticker { get; set; }

        [XmlElement("ceoID")]
        public long CeoID { get; set; }

        [XmlElement("ceoName")]
        public string CeoNameXml
        {
            get { return CeoName; }
            set { CeoName = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("stationID")]
        public long HQStationID { get; set; }

        [XmlElement("stationName")]
        public string HQStationNameXml
        {
            get { return HQStationName; }
            set { HQStationName = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("description")]
        public string DescriptionXml
        {
            get { return Description; }
            set { Description = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("url")]
        public string WebUrl { get; set; }

        [XmlElement("allianceID")]
        public long AllianceID { get; set; }

        [XmlElement("allianceName")]
        public string AllianceNameXml
        {
            get { return AllianceName; }
            set { AllianceName = value?.HtmlDecode() ?? String.Empty; }
        }

        [XmlElement("factionID")]
        public int FactionID { get; set; }

        [XmlElement("factionName")]
        public string FactionName { get; set; }

        [XmlElement("taxRate")]
        public float TaxRate { get; set; }

        [XmlElement("memberCount")]
        public int MemberCount { get; set; }

        [XmlElement("memberLimit")]
        public int MemberLimit { get; set; }

        [XmlElement("shares")]
        public int Shares { get; set; }

        [XmlArray("divisions")]
        [XmlArrayItem("division")]
        public Collection<SerializableDivision> Divisions => m_divisions;

        [XmlArray("walletDivisions")]
        [XmlArrayItem("walletDivision")]
        public Collection<SerializableWalletDivision> WalletDivisions => m_walletDivisions;

        [XmlIgnore]
        public string Name { get; set; }

        [XmlIgnore]
        public string CeoName { get; set; }

        [XmlIgnore]
        public string HQStationName { get; set; }

        [XmlIgnore]
        public string AllianceName { get; set; }

        [XmlIgnore]
        public string Description { get; set; }
    }
}
