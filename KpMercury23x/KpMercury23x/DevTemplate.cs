using System;
using System.Collections.Generic;
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
            ProfileGroups = new List<PowerProfile>();
            CmdGroups = new List<CmdGroup>();
        }

        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public string password { get; set; }
        [XmlAttribute] public string AdmPass { get; set; }      // пароль для уровня администратора
        [XmlAttribute] public string readparam { get; set; }    // Параметр чтения '14h' или '16h'
        [XmlAttribute] public int mode { get; set; }            // Уровень доступа '1' или '2'
        [XmlAttribute] public int fixtime { get; set; }         // количество минут сравнения времени фиксации
        [XmlAttribute] public bool multicast { get; set; }      // true - посылка фиксации данных по адресу 0 для всех приборов на линии
        [XmlAttribute] public bool info { get; set; }           // true - считать данные о счетчике
        [XmlAttribute] public bool readStatus { get; set; }     // true - Читать статус счетчика
        [XmlAttribute] public int SaveTime { get; set; }        // Период сохранения БД в секундах
        [XmlAttribute] public bool SyncTime { get; set; }       // true - Синхронизировать время раз в сутки
        [XmlAttribute] public int halfArchStat { get; set; }    // Номер статуса для неполного среза средних мощностей


        public List<SndRequest> SndGroups { get; set; }
        [XmlIgnore]
        public bool SndGroupsSpecified { get { return SndGroups.Count != 0; } }

        public List<PowerProfile> ProfileGroups { get; set; }
        [XmlIgnore]
        public bool ProfileGroupsSpecified { get { return ProfileGroups.Count != 0; } }

        public List<CmdGroup> CmdGroups { get; set; }
        [XmlIgnore]
        public bool CmdGroupsSpecified { get { return CmdGroups.Count != 0; } }

        public List<ArchGroup> ArchGroups { get; set; }
        [XmlIgnore]
        public bool ArchGroupsSpecified { get { return ArchGroups.Count != 0; } }


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

                public Vals(string name, int signal, bool active, string range)
                {
                    this.name = name;
                    this.signal = signal;
                    this.active = active;
                    this.range = range;
                }

                [XmlAttribute] public string name { get; set; }
                [XmlAttribute] public int signal { get; set; }
                [XmlAttribute] public bool active { get; set; }
                [XmlAttribute] public string range { get; set; }
            }
        }

        public class PowerProfile
        {
            public PowerProfile()
            {
            }

            public PowerProfile(string Name, bool Active, string Range, string Energy)
            {
                this.Name = Name;
                this.Active = Active;
                this.Range = Range;
                this.Energy = Energy;
                value = new List<Vals1>();
            }

            [XmlAttribute] public string Name { get; set; }
            [XmlAttribute] public bool Active { get; set; }
            [XmlAttribute] public string Range { get; set; }
            [XmlAttribute] public string Energy { get; set; }

            [XmlElement] public List<Vals1> value { get; set; }
            [XmlIgnore]
            public bool valueSpecified { get { return value.Count != 0; } }

            public class Vals1
            {
                public Vals1()
                {
                }

                public Vals1(string name, int signal, bool active, string range)
                {
                    this.name = name;
                    this.signal = signal;
                    this.active = active;
                    this.range = range;
                }

                [XmlAttribute] public string name { get; set; }
                [XmlAttribute] public int signal { get; set; }
                [XmlAttribute] public bool active { get; set; }
                [XmlAttribute] public string range { get; set; }
            }
        }


        public class CmdGroup
        {
            public CmdGroup()
            {
            }

            public CmdGroup(string Name, bool Active, int Signal, bool Auto, int Mode, string Cmd, string Par, string Data, int inCnt)
            {
                this.Name = Name;
                this.Active = Active;
                this.Signal = Signal;
                this.Auto = Auto;
                this.Mode = Mode;
                this.Cmd = Cmd;
                this.Par = Par;
                this.Data = Data;
                this.inCnt = inCnt;
            }

            [XmlAttribute] public string Name { get; set; }
            [XmlAttribute] public bool Active { get; set; }
            [XmlAttribute] public int Signal { get; set; }
            [XmlAttribute] public bool Auto { get; set; }       // Автоматическое исполнение команды ???? надо ли ?
            [XmlAttribute] public int Mode { get; set; }        // Уровень доступа для выполнения команды 1 - user, 2 - admin
            [XmlAttribute] public string Cmd { get; set; }
            [XmlAttribute] public string Par { get; set; }
            [XmlAttribute] public string Data { get; set; }
            [XmlAttribute] public int inCnt { get; set; }       // Указать количество принимаемых байт
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
