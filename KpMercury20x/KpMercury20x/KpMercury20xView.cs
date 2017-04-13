using Scada.Comm.Devices.KpMercury20x;
using Scada.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scada.Comm.Devices
{
    public sealed class KpMercury20xView : KPView
    {
        public KpMercury20xView()
            : this(0)
        {
        }

        public KpMercury20xView(int number)
            : base(number)
        {
            CanShowProps = true;
        }

        public override string KPDescr
        {
            get
            {
                return "Автор Бурахин Андрей email: aburakhin@bk.ru\n\n" +
                "Опрос электросчётчиков Меркурий (200.02, 206, ...).\n\n" +
                "Командная строка Коммуникатора, параметр должен заканчиваться разделителем ;\n" +
                "1-й параметр = Адрес счетчика\n" +
                "2-й параметр = битовая маска считываемых параметров\n" +
                "бит числа, выставленный в 1 разрешает чтение группы параметров\n\n" +
                "     Мгновенные значения:\n\n" +
                "bit 0 - Напряжение, Ток, Активная мощность\n" +
                "bit 1 - Реактивная мощность\n" +
                "bit 2 - Cos, Полная мощность\n" +
                "bit 3 - Частота сети\n" +
                "bit 4 - Энергия активная от сброса\n" +
                "bit 5 - Энергия реактивная от сброса\n\n" +
                
                "Тестировалось только на счетчике Меркурий 206";
            }
        }


        // Получить параметры опроса КП по умолчанию
        public override KPReqParams DefaultReqParams
        {
            get
            {
                return new KPReqParams() { Timeout = 1000, Delay = 200 };
            }
        }


        // Отобразить свойства КП
        public override void ShowProps()
        {
        FormSetting.ShowDialog(Number, KPProps, AppDirs);
        }
    }
}
