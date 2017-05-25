using Scada.Comm.Channels;
using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scada.Comm.Devices
{
    /// <summary>
    /// Device communication logic
    /// <para>Логика работы КП</para>
    /// </summary>
    public sealed class KpRpi3Logic : KPLogic
    {
        public static string StatusGPIO(int inout)
        {
            string status_gpio = "";
            switch (inout)
            {
                // Настройка текста перечисления статуса GPIO 
                case 0: status_gpio = "IN"; break; // IN 
                case 1: status_gpio = "OUT"; break; // OUT
            }
            return status_gpio;
        }

        public static string pullControl(int pullC)
        {
            string pull_ctrl = "";
            switch (pullC)
            {
                // Настройка текста перечисления pull Control 
                case 0: pull_ctrl = "pudDown"; break; // pull Down 
                case 1: pull_ctrl = "pudUp"; break; // pull Up
            }
            return pull_ctrl;
        }

        string[] s_out = new string[5]; // массив введенных значений или значений по умолчанию
        string fileName = "RpiGPIO.txt"; // Переменная имени файла для retain переменных драйвера при перезапуске ScadaComm
        int[] wPiNum = new int[24] { 7, 21, 22, 11, 10, 13, 12, 14, 26, 23, 15, 16, 27, 0,  1,  24, 28, 29, 3,  4,  5,  6,  25, 2  }; // Соответствие виртуальных gpio WiringPi BCM номерам с 4 по 27
        int[] bcmNum = new int[24] { 4, 5,  6,  7,  8,  9,  10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 }; // Нумерация согласно BCM GPIO
        int mask_gpio, inout_gpio, pud_gpio, level_gpio, comand, tag, result = 0;
        string FormatGpio = "";
        int signal = 1;
        int[] gpio_num, gpio_inout, gpio_pud, gpio_level = new int[1];
        bool init = false;
        bool res = true;
        int[] gpioNum = new int[24];
        bool active, retain, formatNum = false;
         
        /// <summary>
        /// Вызвать метод записи в журнал
        /// </summary>
        private void ExecWriteToLog(string text)
        {
            if (WriteToLog != null)
                WriteToLog(text);
        }

        public override void OnAddedToCommLine()
        {
            base.OnAddedToCommLine();
            s_out = Parametrs.Parametr(ReqParams.CmdLine.Trim()); // Чтение командной строки

            if (!File.Exists(AppDirs.LogDir + fileName))
            {
                // Create a file to write to.
                File.WriteAllLines(AppDirs.LogDir + fileName, s_out, Encoding.UTF8);
            }
            else
            {
                string[] files_out = File.ReadAllLines(AppDirs.LogDir + fileName); // Чтение параметров из файла для режима Retain
                init = Enumerable.SequenceEqual(s_out, files_out); 
                if (!init)
                {
                    File.WriteAllLines(AppDirs.LogDir + fileName, s_out, Encoding.UTF8); // При измененных параметрах командной строки сохранить новый файл параметров
                }
            }

            mask_gpio  = Convert.ToInt32(s_out[0], 10); // Битовая маска разрешенных GPIO
            inout_gpio = Convert.ToInt32(s_out[1], 10); // Битовая маска отпределения входов/выходов 
            pud_gpio   = Convert.ToInt32(s_out[2], 10); // Битовая маска определения подтяжки
            level_gpio = Convert.ToInt32(s_out[3], 10); // Битовая маска уровня выхода
            comand     = Convert.ToInt32(s_out[4], 10); // Битовая маска дополнительных параметров

            active = Parametrs.GetBit(comand, 0) > 0; // Активировать уровень выхода до инициализации
            retain = Parametrs.GetBit(comand, 1) > 0; // Сохранять состояние выхода при перезапуске ScadaComm ? директория ScadaComm\Log должна быть в tmpfs 
            formatNum = Parametrs.GetBit(comand, 2) > 0; // Тип нумерации GPIO - BCM или wPi

            if (formatNum)
            {
                Array.Copy(wPiNum, 0, gpioNum, 0, gpioNum.Length); //  Нумерация согласно WiringPi GPIO
                FormatGpio = "wPi";
            }
            else
            {
                Array.Copy(bcmNum, 0, gpioNum, 0, gpioNum.Length); // Нумерация согласно BCM GPIO
                FormatGpio = "Bcm";
            }

            gpio_num = Parametrs.nmass_int(gpioNum, mask_gpio); // Новый массив активированных GPIO с нумерацией BCM

            bool[] temp_mass = new BitArray(new int[] { inout_gpio }).Cast<bool>().ToArray() ; // Определение направления работы GPIO (true - OUT, false - IN)
            int[] temp_inout = new int[temp_mass.Length];
            for (int i = 0; i < temp_mass.Length; i++) // Преобразование true, false в целочисленное 1,0 (1=OUT, 2=IN)
            {
                temp_inout[i] = Convert.ToInt32(temp_mass[i]);
            }

            gpio_inout = Parametrs.nmass_int(temp_inout, mask_gpio); // Новый массив направлений, согласно активированным GPIO

            temp_mass = new BitArray(new int[] { pud_gpio }).Cast<bool>().ToArray(); // Определение уровня подтяжки GPIO (true - pullup, false - pulldown)
            int[] temp_pud = new int[temp_mass.Length];
            for (int i = 0; i < temp_mass.Length; i++) // Преобразование true, false в целочисленное 1,0 (1=PUD_DOWN, 2=PUD_UP)
            {
                temp_pud[i] = Convert.ToInt32(temp_mass[i]);
            }

            gpio_pud = Parametrs.nmass_int(temp_pud, mask_gpio); // Новый массив уровней подтяжки, согласно активированным GPIO

            temp_mass = new BitArray(new int[] { level_gpio }).Cast<bool>().ToArray(); // Определение уровня выхода GPIO (true - High, false - low)
            int[] temp_level = new int[temp_mass.Length];
            for (int i = 0; i < temp_mass.Length; i++) // Преобразование true, false в целочисленное 1,0
            {
                temp_level[i] = Convert.ToInt32(temp_mass[i]);
            }

            gpio_level = Parametrs.nmass_int(temp_level, mask_gpio); // Новый массив уровней выходов, согласно активированным GPIO

            result = formatNum ? WiringPi.Core.Setup() : WiringPi.Core.SetupGpio();

            if (result == -1)
            {
                WriteToLog("WiringPi init failed!");
                res = false;
            }

            if (!init)
            {
                for (int i = 0; i < gpio_num.Length; i++) // Инициализация используемых GPIO
                {
                    WiringPi.Core.PullUpDnControl(gpio_num[i], gpio_pud[i]+1); // Активировать PullDnUpControl
                    if (active && gpio_inout[i] == 1)  // Если необходимо записать уровень перед активацией выхода
                    {
                        WiringPi.Core.DigitalWrite(gpio_num[i], gpio_level[i]);
                    }
                    WiringPi.Core.PinMode(gpio_num[i], gpio_inout[i]);
                }
                init = true;
            }

            List<TagGroup> tagGroups = new List<TagGroup>();
            TagGroup tagGroup;
            tagGroup = new TagGroup(Localization.UseRussian ? "Список используемых GPIO:" : "List of used GPIO:"); 

            for (int i = 0; i < 24; i++)
            {
                if (Parametrs.GetBit(mask_gpio, i) > 0)
                {
                    tagGroup.KPTags.Add(new KPTag(signal, FormatGpio + "." + gpio_num[signal-1] + "_" + StatusGPIO(gpio_inout[signal-1]) + "_" + pullControl(gpio_pud[signal-1])));
                    signal++;
                }
            }
            tagGroups.Add(tagGroup);
            InitKPTags(tagGroups);
        }


        public KpRpi3Logic(int number) : base(number)
        {
            ConnRequired = false;
            CanSendCmd = true;
        }

        public override void Session()
        {
            //base.Session();

            if (res)
            {
                if (init) // Читаем состояние GPIO .. Read GPIO level
                {
                    for (int i = 0; i < gpio_num.Length; i++)
                    {
                        WiringPi.Core.DigitalRead(gpio_num[i]);
                        SetCurData(tag, WiringPi.Core.DigitalRead(gpio_num[i]), 1);
                        tag++;
                    }
                }

                lastCommSucc = true;
                CalcSessStats();
                tag = 0;
            }
        }

        /// <summary>
        /// Отправить команду ТУ
        /// </summary>
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);
            if (cmd.CmdTypeID == BaseValues.CmdTypes.Standard) // || cmd.CmdTypeID == BaseValues.CmdTypes.Binary) 
            {
                double can_in = cmd.CmdVal;
                int num_com = cmd.CmdNum;

                if (can_in == double.NaN)
                    WriteToLog(CommPhrases.IncorrectCmdData);
                else
                {
                    if (gpio_inout[num_com - 1] == 1) // Команда управления выходом
                    {
                        lastCommSucc = false;
                        WiringPi.Core.DigitalWrite(gpio_num[num_com - 1], (Int32)can_in);
                        WriteToLog((Localization.UseRussian ? "Запись значения " : "Write value ") + (Int32)can_in + (Localization.UseRussian ? " - сигнал " : " - signal ") + num_com);
                        // тут проверка изменения выхода должна быть (бесполезная)
                        int okey = WiringPi.Core.DigitalRead(gpio_num[num_com - 1]);
                        if (okey == (Int32)can_in)
                        {
                            WriteToLog("OK");
                            lastCommSucc = true;
                        }
                        else
                        {
                            WriteToLog("Error");
                        }
                    }
                }
            }
            CalcCmdStats();
        }

        public override void OnCommLineTerminate() // Запись безопасных состояний выходов
        {
            if (!retain)
            {
                for (int i = 0; i < gpio_num.Length; i++)
                {
                    if (gpio_inout[i] == 1)
                    {
                        WiringPi.Core.DigitalWrite(gpio_num[i], gpio_level[i]);
                    }
                }
            }
        }

    }
}
