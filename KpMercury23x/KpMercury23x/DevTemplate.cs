using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Scada.Comm.Devices
{
    [Serializable]
    public class DevTemplate
    {
        public DevTemplate()
        {
            SndGroups = new List<SndRequest>();
        }

        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public string password { get; set; }
        [XmlAttribute] public string readparam { get; set; }    // Параметр чтения '14h' или '16h'
        [XmlAttribute] public int mode { get; set; }            // Уровень доступа '1' или '2'
        [XmlAttribute] public string fixtime { get; set; }      // количество минут сравнения времени фиксации
        [XmlAttribute] public bool multicast { get; set; }      // true - посылка фиксации данных по адресу 0 для всех приборов на линии
        [XmlAttribute] public bool info { get; set; }           // true - считать данные о счетчике



        public List<SndRequest> SndGroups { get; set; }
        [XmlIgnore]
        public bool SndGroupsSpecified { get { return SndGroups.Count != 0; } }
        public List<CmdGroup> CmdGroups { get; set; }
        [XmlIgnore]
        public bool CmdGroupsSpecified { get { return CmdGroups.Count != 0; } }

        public class SndRequest
        {
            public SndRequest()
            {
            }

            public SndRequest(string Name, bool Active, int Bit)
            {
                this.Name = Name;
                this.Active = Active;
                this.Bit = Bit;
                value = new List<Vals>();
            }

            [XmlAttribute] public string Name { get; set; }
            [XmlAttribute] public bool Active { get; set; }
            [XmlAttribute] public int Bit { get; set; }
            [XmlElement] public List<Vals> value { get; set; }
            [XmlIgnore]
            public bool valueSpecified { get { return value.Count != 0; } }


            public class Vals
            {
                public Vals()
                {
                }

                public Vals(string name, int signal, bool active, double range)
                {
                    this.name = name;
                    this.signal = signal;
                    this.active = active;
                    this.range = range;
                }

                [XmlAttribute] public string name { get; set; }
                [XmlAttribute] public int signal { get; set; }
                [XmlAttribute] public bool active { get; set; }
                [XmlAttribute] public double range { get; set; }

            }
        }

        public class CmdGroup
        {
            public CmdGroup()
            {
            }

            public CmdGroup(bool CmdActive, int CmdCnt, string CmdName, string CmdType)
            {
                this.CmdActive = CmdActive;
                this.CmdCnt = CmdCnt;
                this.CmdName = CmdName;
                this.CmdType = CmdType;
            }
            [XmlAttribute] public bool CmdActive { get; set; }
            [XmlAttribute] public int CmdCnt { get; set; }
            [XmlAttribute] public string CmdName { get; set; }
            [XmlAttribute] public string CmdType { get; set; }
        }

        public class ArchGroup
        {
            public ArchGroup()
            {
            }

            public ArchGroup(bool Active, int Signal, string Name, string Type)
            {
                this.Active = Active;
                this.Signal = Signal;
                this.Name = Name;
                this.Type = Type;
            }
            [XmlAttribute] public bool Active { get; set; }
            [XmlAttribute] public int Signal { get; set; }
            [XmlAttribute] public string Name { get; set; }
            [XmlAttribute] public string Type { get; set; }
        }

    }
}



