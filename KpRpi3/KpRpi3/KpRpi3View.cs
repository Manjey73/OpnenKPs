
using Scada.Comm.Devices.KpRpi3;
using Scada.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scada.Comm.Devices
{
    /// <summary>
    /// Device library user interface
    /// <para>Пользовательский интерфейс библиотеки КП</para>
    /// </summary>
    public sealed class KpRpi3View : KPView
    {

        public KpRpi3View() : this(0)
        {
        }

        public KpRpi3View(int number) : base(number)
        {
            CanShowProps = true;
        }


        public override string KPDescr
        {
            get
            {
                return Localization.UseRussian ?
                    "Автор Бурахин Андрей email: aburakhin@bk.ru\n\n" +
                    "Библиотека КП для GPIO Raspberry Pi 3.\n\n" +
                    "Команды ТУ:\n" +
                    "Стандартная - отправка данных 0/1\n" +
                    "Номер команды = номеру сигнала\n\n"+
                    "Параметры командной строки val1; val2; val3; val4; val5;\n" +
                    "командная строка может иметь пустое значение\n" +
                    "будет принято значение по умолчанию = '0'\n" +
                    "должна заканчиваться символом ';'\n" +
                    "val1 - битовая маска используемых GPIO\n" +
                    "val2 - битовая маска направления (IN/OUT)\n"+
                    "val3 - битовая маска pull Control (pudDown/pudUp)\n"+
                    "val4 - битовая маска активации уровня выхода (Low/High)\n" +
                    "val5 - bit0(активировать уровни выходов при инициализации)\n"+
                    "       bit1(сохранять выходы при рестарте ScadaComm)\n"+
                    "       при рестарте ПК или смене параметров выходы инициализируются\n"+
                    "       bit2(выбор формата нумерации BCM или wiringPi)" :

                    "By Andrey Burakhin email: aburakhin@bk.ru\n\n" +
                    "Device library for GPIO Raspberry Pi 3.\n\n" +
                    "Commands:\n" +
                    "Standart - send data as 0/1\n" +
                    "Command number = signal number\n\n" +
                    "Command line parameters val1; val2; val3; val4; val5;\n" +
                    "command line can have empty value\n" +
                    "will be accepted value by default = '0'\n" +
                    "must end with the symbol  ';'"+
                    "val1 - bit mask use GPIO\n" +
                    "val2 - bit mask direction (IN/OUT)\n" +
                    "val3 - bit mask pull Control (pudDown/pudUp)\n" +
                    "val4 - bit mask set level before activate OUT (Low/High)\n" +
                    "val5 - bit0(to activate the levels of the outputs when initializing)\n" +
                    "       bit1(to keep the outputs at restart ScadaComm)\n" +
                    "       when restarting the PC or changing settings, the outputs are initialized\n" +
                    "       bit2(the choice of the numbering format BCM or wiringPi)";
            }
        }

        // Получить параметры опроса КП по умолчанию
        public override KPReqParams DefaultReqParams
        {
            get
            {
                return new KPReqParams() { Timeout = 200, Delay = 100 };
            }
        }

        // Отобразить свойства КП
        public override void ShowProps()
        {
            FormRPi.ShowDialog(Number, KPProps, AppDirs);
        }

    }
}
