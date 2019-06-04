using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Scada.Comm.Devices
{
    [Serializable]
    public class DevTemplate // Сформированный класс шаюлона под задачу для сериализации XML
    {
        public DevTemplate()
        {
            SndGroups = new List<SndGroup>();   // Список каталога запросов
            CmdGroups = new List<CmdGroup>();   // Список каталога команд
            Values = new List<Value>();         // Список каталога переменных
        }

        [XmlAttribute] public string Name { get; set; }

        public List<SndGroup> SndGroups { get; set; } // это позволяет сделать сериализацию без создания соответствующих групп если они пустые
        [XmlIgnore]
        public bool SndGroupsSpecified { get { return SndGroups.Count != 0; } }
        public List<CmdGroup> CmdGroups { get; set; }
        [XmlIgnore]
        public bool CmdGroupsSpecified { get { return CmdGroups.Count != 0; } }
        public List<Value> Values { get; set; }
        [XmlIgnore]
        public bool ValuesSpecified { get { return Values.Count != 0; } }

        public class SndGroup
        {
            public SndGroup()
            {
            }

            public SndGroup( int SndCnt, bool SndActive, string SndName, string SndCode, string SndData)
            {
                this.SndCnt = SndCnt;
                this.SndActive = SndActive;
                this.SndName = SndName;
                this.SndCode = SndCode;
                this.SndData = SndData;
            }

            [XmlAttribute] public int SndCnt { get; set; }
            [XmlAttribute] public bool SndActive { get; set; }
            [XmlAttribute] public string SndName { get; set; }
            [XmlAttribute] public string SndCode { get; set; }
            [XmlAttribute] public string SndData { get; set; }
        }

        public class CmdGroup
        {
            public CmdGroup()
            {
            }

            public CmdGroup(int CmdCnl, bool CmdActive, string CmdName, string CmdCode, string CmdData, string CmdType)
            {
                this.CmdCnl = CmdCnl;
                this.CmdActive = CmdActive;
                this.CmdName = CmdName;
                this.CmdCode = CmdCode;
                this.CmdData = CmdData;
                this.CmdType = CmdType;
            }

            [XmlAttribute] public int CmdCnl { get; set; }
            [XmlAttribute] public bool CmdActive { get; set; }
            [XmlAttribute] public string CmdName { get; set; }
            [XmlAttribute] public string CmdCode { get; set; }
            [XmlAttribute] public string CmdData { get; set; }
            [XmlAttribute] public string CmdType { get; set; }
        }

        public class Value
        {
            public Value()
            {
            }

            public Value(int ValCnt, string ValMenu)
            {
                this.ValCnt = ValCnt;
                this.ValMenu = ValMenu;
                Vals = new List<Val>();
            }

            [XmlAttribute] public int ValCnt { get; set; }
            [XmlAttribute] public string ValMenu { get; set; }
            [XmlElement] public List<Val> Vals { get; set; }

            public class Val
            {
                public Val()
                {
                }

                public Val(int SigCnl, bool SigActive, string SigName, string SigType, double Range) //, double Value)
                {
                    this.SigCnl = SigCnl;
                    this.SigActive = SigActive;
                    this.SigName = SigName;
                    this.SigType = SigType;
                    this.Range = Range;
                }

                [XmlAttribute] public int SigCnl { get; set; }
                [XmlAttribute] public bool SigActive { get; set; }
                [XmlAttribute] public string SigName { get; set; }
                [XmlAttribute] public string SigType { get; set; }
                [XmlAttribute] public double Range { get; set; }
            }

        }
    }
}
