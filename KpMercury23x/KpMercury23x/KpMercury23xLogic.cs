/*
 * Copyright 2020 Andrey Burakhin
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * Product  : Rapid SCADA
 * Module   : KpMercury23x
 * Summary  : Device communication logic
 * 
 * Author   : Andrey Burakhin
 * Created  : 2017
 * Modified : 2020
 */


using Scada.Comm.Channels;
using Scada.Data.Models;
using Scada.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml;
using Scada.Data.Tables;
using System.Windows.Forms;
using ScadaCommFunc;
using System.Threading;
using System.Reflection;
using Scada.Data.Configuration;



namespace Scada.Comm.Devices
{

    public class KpMercury23xLogic : KPLogic
    {
        private DevTemplate devTemplate = new DevTemplate();

        private int read_cnt = 0;
        private string fileName = "";
        private string filePath = "";
        private bool fileyes = false;   // При отсутствии файла не выполнять опрос
        private int idgr = 0;           // переменная для индекса группы
        private string Allname;         // Полное имя тега
        private bool CommSucc = true;
        private byte[] inBuf;

        private Requests requests;

        private void InitRequests()
        {
            //MyDevice iprop = (MyDevice)CommonProps[address];
            requests.testCnlReq = Protocol.TestCnlReq(Address);
            requests.openCnlReq = Protocol.OpenCnlReq(Address, uroven, Pass());
            requests.kuiReq = Protocol.KuiReq(Address);

            // Подсчет количества запросов КП для одного прибора и формирование формата запроса
            // Формирование запроса фиксации данных в зависимости от параметра multicast
            if (devTemplate.multicast)
            {
                //iprop.kpCnt++;
                requests.fixDataReq = Protocol.FixDataReq(0xFE);
            }
            else
            {
                requests.fixDataReq = Protocol.FixDataReq(Address);
            }
            //CommonProps[address] = iprop;
        }

        private string Pass()
        {
            string kpNum_pass = string.Concat(Number.ToString(), "_pass");
            string pass = String.IsNullOrEmpty(CustomParams.GetStringParam(kpNum_pass, false, "")) ? password : CustomParams.GetStringParam(kpNum_pass, false, "");
            return pass;
        }

        public static long Ticks() // возвращает время в миллисекундах
        {
            DateTime now = DateTime.Now;
            long time = now.Ticks / 10000;
            return time;
        }

        public class MyDevice                   // переменные для устройств на линии связи
        {
            //public int kpCnt = 0;               // счетчик КП прибора с одним адресом для широковещательной команды
            public bool testcnl = false;        // фиксация команды тестирования канала
            public bool opencnl = false;        // фиксация команды авторизации и открытия канала
            public bool Kui = false;            // фиксация команды чтения коэффициентов трансформации
            public int ku = 1;                  // Коэффициент трансформации напряжения 
            public int ki = 1;                  // Коэффициент трансформации тока
            public int[] parkui = new int[13];  // массив для данных коэффициентов
            public long tik = 0;                // таймер времени открытого канала
            public int firstKp = 0;             // КП, являющийся первым КП в опросе, последующие не записывают номер КП, значение остается = 0
            public bool firstFix = false;       // Переменная для фиксации адреса первого КП
            public DateTime dt;                 // Время команды фиксации для сравнения 

            // Данные счетчика
            public bool readInfo = false;       // блокировка чтения информации счетчика
            public int serial = 0;              // Серийный номер счетчика
            public int Aconst = 500;            // Постоянная счетчика, по умолчанию 500 имп/квт*ч
            public DateTime made;               // Дата изготовления

            public override string ToString()
            {
                string outprops = string.Concat("SN_", serial.ToString(), " Изготовлен ", made.ToString("dd.MM.yyyy"), " Пост.счетчика ", Aconst.ToString(), " имп/квт*ч");
                return outprops;
            }

        }

        protected virtual string address
        {
            get
            {
                return  devTemplate.Name + "_" + Convert.ToString(Address); //  + "DevAddr_"
            }
        }

        private MyDevice GetMyDevice()
        {
            MyDevice devaddr = CommonProps.ContainsKey(address) ?
                CommonProps[address] as MyDevice : null;

            if (devaddr == null)
            {
                devaddr = new MyDevice();
                if (!CommonProps.ContainsKey(address))
                {
                    CommonProps.Add(address, devaddr);
                }
            }
            return devaddr;
        }

        public static string ToLogString(byte errcode)
        {
            string logs = "Неизвестная ошибка";
            switch (errcode)
            {
                case 0x00: logs = "Связь восстановлена"; break;
                case 0x01: logs = "Недопустимая команда или параметр"; break;
                case 0x02: logs = "Внутренняя ошибка счетчика"; break;
                case 0x03: logs = "Недостаточен уровень доступа"; break;
                case 0x04: logs = "Внутренние часы корректировались"; break;
                case 0x05: logs = "Не открыт канал связи"; break;
                case 0x10: logs = "Нет ответа от прибора"; break;
                case 0x11: logs = "Нет устройства с таким адресом"; break;
            }
            return logs;
        }

        public static int ConstA(byte aconst)
        {
            aconst = (byte)(aconst & 0x0f);
            // Постоянная счетчика в имп/квт*ч  - 2.3.16 - Чтение варианта исполнения 2-й байт 0-3 биты
            int consta = 500;
            switch (aconst)
            {
                case 0x00: consta = 5000; break;
                case 0x01: consta = 25000; break;
                case 0x02: consta = 1250; break;
                case 0x03: consta = 500; break;
                case 0x04: consta = 1000; break;
                case 0x05: consta = 250; break;
            }
            return consta;
        }

        // Новый целочисленный массив с данными
        public int[] nmass_int(int[] mass_in, uint mask)
        {
            int c = 0;
            int b = 0;
            int[] par_num = new int[1];

            while (mask != 0)
            {
                if ((mask & 1) != 0)
                {
                    Array.Resize(ref par_num, c + 1); //изменить размер массива
                    par_num[c] = mass_in[b];
                    c++;
                }
                mask = mask >> 1;
                b++;
            }
            return par_num;
        }

        public bool chan_err(byte code) // Проверка изменения кода ошибки в ответе прибора и выставление флага для генерации события
        {
            bool q = false;
            if (code_err != code) q = !q;
            return q;
        }

        private int[] bwri = new int[13]; // BWRI для запроса параметром 14h
        private int[] bwrc = new int[13]; // Разрешающая способность регистров хранения 
        private int[] b_length = new int[13]; // количество байт в ответе счетчика
        private int[] parb = new int[13];    // количество байт в параметре ответа (4 или 3)
        private int[] parc = new int[13];    // количество параметров в ответе (4, 3 или 1)

        // Массив значений параметров BWRI счетчика + 'энергии от сброса параметр 14h
        // Команды BWRI для запроса 14h:
        // 0x00 - Мощность P по сумме фаз, фазе 1, фазе 2, фазе 3   (Вт)
        // 0x04 - Мощность Q по сумме фаз, фазе 1, фазе 2, фазе 3   (вар)
        // 0x08 - Мощность S по сумме фаз, фазе 1, фазе 2, фазе 3   (ВА)
        // 0x10 - Напряжение по фазе 1, фазе 2, фазе 3              (В)
        // 0x30 - Косинус ф по сумме фаз, фазе 1, фазе 2, фазе3
        // 0x20 - Ток по фазе 1, фазе 2, фазе 3                     (А)
        // 0x40 - Частота сети
        // 0x50 - Угол м-ду ф. 1 и 2, 1 и 3, 2 и 3                  (градусы)

        // F0,-,F4 - Зафиксированная энергия от сброса
        private int[] bwri_14 = new int[] { 0x00, 0x04, 0x08, 0x30, 0x10, 0x20, 0x50, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 14h
        private int[] bwrc_14 = new int[] { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        private int[] b_length_14 = new int[] { 19, 19, 19, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        private int[] parb_14 = new int[] { 4, 4, 4, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        private int[] parc_14 = new int[] { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)


        // Массив значений параметров BWRI счетчика + 'энергии от сброса для чтения параметром 16h
        // Команды BWRI для запроса 16h:
        // 0x00 - Мощность P по сумме фаз, фазе 1, фазе 2, фазе 3   (Вт)
        // 0x04 - Мощность Q по сумме фаз, фазе 1, фазе 2, фазе 3   (вар)
        // 0x08 - Мощность S по сумме фаз, фазе 1, фазе 2, фазе 3   (ВА)
        // 0x11 - Напряжение по фазе 1, фазе 2, фазе 3              (В)
        // 0x30 - Косинус ф по сумме фаз, фазе 1, фазе 2, фазе3
        // 0x21 - Ток по фазе 1, фазе 2, фазе 3                     (А)
        // 0x40 - Частота сети
        // 0x51 - Угол м-ду ф. 1 и 2, 1 и 3, 2 и 3                  (градусы)

        private int[] bwri_16 = { 0x00, 0x04, 0x08, 0x30, 0x11, 0x21, 0x51, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 16h
        private int[] bwrc_16 = { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        private int[] b_length_16 = { 15, 15, 15, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        private int[] parb_16 = { 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        private int[] parc_16 = { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)

        // Массив значений энергии по фазам прямого направления и чтения энергии от сброса функцией 0x05 при чтении счетчика параметром 16h кода функции 0x08
        private int[] massenergy = new int[] { 0, 1, 2, 3, 4 };

        private bool newmass = false;
        private bool newenergy = false;

        private int tag = 1;
        private int[] nbwri = new int[1];
        private int[] nbwrc = new int[1];
        private int[] nb_length = new int[1];
        private int[] nparb = new int[1];
        private int[] nparc = new int[1];
        private int[] nenergy = new int[1];
        private byte code_err = 0x0;
        private byte code = 0x0;
        private int Napr = 1;       // Направление Активной или Реактивной мощности 1 = прямое (бит направления = 0), при обратном значение = -1
        private double fixTime;     // по умолчанию разница времени фиксации 1 минута

        private int mask_g1 = 0; // Входная переменная для выбора тегов

        private string readparam, password, uroven; //входная строка для параметра команды 0x08, объявление переменной для пароля и уровня доступа

        public override void OnAddedToCommLine()
        {
            base.OnAddedToCommLine();

            devTemplate = null;

            fileName = ReqParams.CmdLine == null ? "" : ReqParams.CmdLine.Trim();
            filePath = AppDirs.ConfigDir + fileName;

            if (fileName == "") // Чтение файла шаблона
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                    "{0} Ошибка: Не задан шаблон устройства для {1}" :
                    "{0} Error: Template is undefined for the {1}", CommUtils.GetNowDT(), Caption));
            } // Чтение файла шаблона
            else
            {
                try
                {
                    devTemplate = FileFunc.LoadXml(typeof(DevTemplate), filePath) as DevTemplate;
                    fileyes = true;
                }
                catch (Exception err)
                {
                    WriteToLog(string.Format(Localization.UseRussian ?
                    "Ошибка: " + err.Message :
                    "Error: " + err.Message, CommUtils.GetNowDT(), Caption));
                }
            }

            if (devTemplate != null)
            {
                password = string.IsNullOrEmpty(devTemplate.password) ? "111111" : devTemplate.password;
                uroven = string.IsNullOrEmpty(devTemplate.mode.ToString()) ? "1" : devTemplate.mode.ToString(); // Преобразование int '1' или '2' к строке для совместимости с кодом драйвера, по умолчанию '1' если строка пуста
                readparam = string.IsNullOrEmpty(devTemplate.readparam) ? "14h" : devTemplate.readparam;

                double result = 1;
                // чтение параметра времени сравнения фиксации в минутах
                if (!double.TryParse(devTemplate.fixtime, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), out result))
                {
                    if (!double.TryParse(devTemplate.fixtime, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out result)) ;
                }
                fixTime = result;

                if (devTemplate.SndGroups.Count != 0) // Определить активные запросы объектов и записать в список индексы запросов для создания тегов
                {
                    for (int sg = 0; sg < devTemplate.SndGroups.Count; sg++)
                    {
                        if (devTemplate.SndGroups[sg].Active)
                        {
                            mask_g1 = BitFunc.SetBit(mask_g1, devTemplate.SndGroups[sg].Bit, true);
                        }
                    }
                }
            }

            if (readparam == "14h")
            {
                Array.Copy(bwri_14, bwri, 13);
                Array.Copy(bwrc_14, bwrc, 13);
                Array.Copy(b_length_14, b_length, 13);
                Array.Copy(parb_14, parb, 13);
                Array.Copy(parc_14, parc, 13);
            }
            if (readparam == "16h")
            {
                Array.Copy(bwri_16, bwri, 13);
                Array.Copy(bwrc_16, bwrc, 13);
                Array.Copy(b_length_16, b_length, 13);
                Array.Copy(parb_16, parb, 13);
                Array.Copy(parc_16, parc, 13);
            }

            int mgn_znac = mask_g1 & 0xFF; // отсечь мгновенные значения для организации отображения тегов
            int energy = mask_g1 & 0x3FF00; // Отсечь параметры энергии для организации отображения тегов

            List<TagGroup> tagGroups = new List<TagGroup>();
            TagGroup tagGroup;

            if (mgn_znac != 0)
            {
                tagGroup = new TagGroup("Мгновенные значения:");

                bool mgn_P = BitFunc.GetBit(mask_g1, 0) > 0; // Параметр Bit группы "Мощность P" должен быть равен 0 
                if (mgn_P)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 0); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_Q = BitFunc.GetBit(mask_g1, 1) > 0; // Параметр Bit группы "Мощность Q" должен быть равен 1
                if (mgn_Q)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 1); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_S = BitFunc.GetBit(mask_g1, 2) > 0; // Параметр Bit группы "Мощность S" должен быть равен 2
                if (mgn_S)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 2); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_cos = BitFunc.GetBit(mask_g1, 3) > 0; // Параметр Bit группы "COSф" должен быть равен 3
                if (mgn_cos)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 3); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_U = BitFunc.GetBit(mask_g1, 4) > 0; // Параметр Bit группы "Напряжение" должен быть равен 4
                if (mgn_U)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 4); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_I = BitFunc.GetBit(mask_g1, 5) > 0; // Параметр Bit группы "Ток" должен быть равен 5
                if (mgn_I)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 5); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_FU = BitFunc.GetBit(mask_g1, 6) > 0; // Параметр Bit группы "Угол м-ду ф." должен быть равен 6
                if (mgn_FU)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 6); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_F = BitFunc.GetBit(mask_g1, 7) > 0; // Параметр Bit группы "Частота" должен быть равен 7
                if (mgn_F)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 7); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                tagGroups.Add(tagGroup);
            }

            if (energy != 0)
            {
                tagGroup = new TagGroup("Энергия от сброса:");

                bool en_summ = BitFunc.GetBit(mask_g1, 8) > 0; // Параметр Bit группы "Сумма" должен быть равен 8
                if (en_summ)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 8); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar1 = BitFunc.GetBit(mask_g1, 9) > 0; // Параметр Bit группы "Тариф 1" должен быть равен 9
                if (en_tar1)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 9); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar2 = BitFunc.GetBit(mask_g1, 10) > 0; // Параметр Bit группы "Тариф 2" должен быть равен 10
                if (en_tar2)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 10); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar3 = BitFunc.GetBit(mask_g1, 11) > 0; // Параметр Bit группы "Тариф 3" должен быть равен 11
                if (en_tar3)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 11); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar4 = BitFunc.GetBit(mask_g1, 12) > 0; // Параметр Bit группы "Тариф 4" должен быть равен 12
                if (en_tar4)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 12); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_summ = BitFunc.GetBit(mask_g1, 13) > 0; // Параметр Bit группы "Сумма А+" должен быть равен 13
                if (enL_summ)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 13); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar1 = BitFunc.GetBit(mask_g1, 14) > 0; // Параметр Bit группы "Тариф 1 А+" должен быть равен 14
                if (enL_tar1)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 14); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar2 = BitFunc.GetBit(mask_g1, 15) > 0; // Параметр Bit группы "Тариф 2 А+" должен быть равен 15
                if (enL_tar2)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 15); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar3 = BitFunc.GetBit(mask_g1, 16) > 0; // Параметр Bit группы "Тариф 3 А+" должен быть равен 16
                if (enL_tar3)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 16); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar4 = BitFunc.GetBit(mask_g1, 17) > 0; // Параметр Bit группы "Тариф 4 А+" должен быть равен 17
                if (enL_tar4)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 17); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                tagGroups.Add(tagGroup);
            }

            tagGroup = new TagGroup("Статус:");
            tagGroup.KPTags.Add(new KPTag(70, "Код ошибки:"));
            tagGroup.KPTags.Add(new KPTag(71, "коэфф. трансформации тока:"));
            tagGroup.KPTags.Add(new KPTag(72, "коэфф. трансформации напряжения:"));
            tagGroups.Add(tagGroup);

            InitKPTags(tagGroups);
        }


        //---------------------------------------------------------------
        public KpMercury23xLogic(int number) : base(number)
        {
            inBuf = new byte[100];
            requests = new Requests();
            CanSendCmd = true;
            ConnRequired = true;
        }
        //---------------------------------------------------------------

        /// <summary>
        /// Выполнить действия при запуске линии связи
        /// </summary>
        public override void OnCommLineStart()
        {
            GetMyDevice();
            InitRequests();
        }

        /// <summary>
        /// Отправить запрос и сделать запись в журнал
        /// </summary>
        private void Write(byte[] request)
        {
            string logText;
            WriteRequest(request, out logText);
            WriteToLog(logText);
        }

        private void WriteRequest(byte[] request, out string logText)
        {
            logText = null;
            Connection.Write(request, 0, request.Length, CommUtils.ProtocolLogFormats.Hex, out logText);
        }

        /// <summary>
        /// Считать ответ из последовательного порта
        /// </summary>
        private void ReadAnswer(byte[] buffer, int count, out string logText)
        {
            read_cnt = Connection.Read(buffer, 0, count, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
        }

        //-------------------------
        public override void Session()
        {
            base.Session();

            if (!fileyes)        // Если конфигурация не была загружена, выставляем все теги в невалидное состояние и выходим         
            {
                InvalidateCurData();
                return;
            }

            MyDevice prop = (MyDevice)CommonProps[address];

            // код работает один раз
            if (devTemplate.multicast && !prop.firstFix)
            {
                // Если параметр firstFix = true, то первый КП на линии посылает команду фиксации данных по широковещательному
                // адресу 0xFE в линию, иначе каждый КП посылает команду своему прибору при параметре multicast = false
                prop.firstKp = Number; // в качестве стартового используем номер КП, остальные будут = 0
                for (int x = 0; x < CommonProps.Count; x++)
                {
                    var Val = (MyDevice)CommonProps.ElementAt(x).Value;
                    Val.firstFix = true;
                }
                CommonProps[address] = prop;
            }

            CommSucc = true;

            uint par14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // отсечь количество параметров для команды 08h и параметра 14h

            uint energyL = Convert.ToUInt32(mask_g1 & 0x3E000); // Проверка наличия опроса значений энергии прямого направления
            energyL = BitFunc.ROR(energyL, 13);

            if (!newmass)
            {
                nbwri = nmass_int(bwri, par14h); // Создание новых массивов для команды 08h параметр 14h для чтения согласно битовой маске.
                nbwrc = nmass_int(bwrc, par14h);
                nb_length = nmass_int(b_length, par14h);
                nparb = nmass_int(parb, par14h);
                nparc = nmass_int(parc, par14h);
                newmass = true;
            }
            if (!newenergy)
            {
                nenergy = nmass_int(massenergy, energyL); // Создание нового массива значений энергии по фазам от сброса согласно битовой маске
                newenergy = true;
            }

            if (!prop.testcnl)
            {
                Request(requests.testCnlReq, 4);

                if (lastCommSucc)
                {
                    prop.testcnl = true;
                    code = inBuf[1];
                }
                else
                {
                    code = 0x11;
                    WriteToLog(ToLogString(code));
                    CommSucc = false;
                }
                //CommonProps[address] = prop;
            }

            if (prop.testcnl) // Открытие канала с уровнем доступа согласно введенного пароля.
            {
                long t2 = Ticks();
                if ((t2 > (prop.tik + 240000)) || !prop.opencnl)
                {
                    // Запрос на открытие канала
                    Request(requests.openCnlReq, 4);

                    if (lastCommSucc)
                    {
                        code = inBuf[1];
                        // тут проверка на корректность пароля, смена статуса открытого канала
                        if (inBuf[1] == 0)
                        {
                            prop.opencnl = true;
                        }
                        else
                        {
                            WriteToLog(ToLogString(code));
                            prop.opencnl = false;
                        }
                    }
                    else
                    {
                        code = 0x10;
                        string er = string.Concat(CommPhrases.ResponseError, " ", ToLogString(code));
                        WriteToLog(er);
                    }

                    prop.tik = t2;
                    CommonProps[address] = prop;
                }
            }

            // Запрос информации счетчика - Серийный номер, дата выпуска, версия ПО, вариант исполнения
            if (devTemplate.info && prop.opencnl && !prop.readInfo)
            {
                requests.infoReq = Protocol.InfoReq(Address);

                Request(requests.infoReq, 19);

                if (lastCommSucc)
                {
                    int snNum = 0;
                    int multip = 1;

                    for (int d = 4; d > 0; d--)
                    {
                        int ch = Convert.ToInt32(inBuf[d]);
                        snNum = (ch * multip) + snNum;
                        multip = ch > 99 ? multip = multip * 1000 : multip = multip * 100;
                    }

                    prop.serial = snNum;                                           // Сохраняем серийный номер
                    prop.made = new DateTime(2000 + inBuf[7], inBuf[6], inBuf[5]); // Сохраняем дату изготовления
                    prop.Aconst = ConstA(inBuf[12]);                               // Сохраняем постоянну счетчика имп/квт*ч

                }
                else
                {
                    // Данные со счетчика не считаны, чтение профилей средней мощности невозможно (заделка на будущее)
                }
                prop.readInfo = true;
                CommonProps[address] = prop;
            }

            // Запрос коэффициентов трансформации напряжения и тока
            if (prop.opencnl && !prop.Kui)
            {
                Request(requests.kuiReq, 7);

                if (lastCommSucc)
                {
                    prop.Kui = true;
                    prop.ku = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(inBuf, 1), 8));
                    prop.ki = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(inBuf, 3), 8));
                    prop.parkui = new int[] { prop.ki, prop.ki, prop.ki, 1, prop.ku, prop.ki, 1, 1, prop.ki, prop.ki, prop.ki, prop.ki, prop.ki };
                }
                else
                {
                    string er = string.Concat(CommPhrases.ResponseError, " Недопустимая команда");
                    WriteToLog(er);
                }
                CommonProps[address] = prop;
            }

            // ------------Получить мгновенные значения P,Q,S,U,I вариант 2
            uint com14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // проверка маски на необходимость чтения команды 08h с параметром 14h

            if (com14h != 0)
            {
                int znx = 1; // начальное положение первого массива байт в ответе
                double znac = 0;

                if (readparam == "14h") // TEST 14h При чтении параметром 16 не нужна фиксация данных
                {
                    if (devTemplate.multicast)
                    {
                        DateTime datetime = DateTime.Now;

                        if (prop.firstKp == Number) // Address
                        {
                            // запись для всех КП времени фиксации и посылка команды фиксации данных
                            for (int x = 0; x < CommonProps.Count; x++)
                            {
                                var Val = (MyDevice)CommonProps.ElementAt(x).Value;
                                Val.dt = datetime;
                            }
                            CommonProps[address] = prop;
                            Write(requests.fixDataReq);
                            Thread.Sleep(ReqParams.Delay);
                        }
                        else
                        {
                            // Тут сравнение времени фиксации и при необходимости отправка команды фиксации данных
                            if (datetime.Subtract(prop.dt).TotalMinutes > fixTime)
                            {
                                //string time = string.Concat("Текущее время - ", Convert.ToString(datetime), " Разница - ", Convert.ToString(datetime.Subtract(prop.dt).TotalMinutes));
                                //WriteToLog(time); // TEST TEST TEST
                                
                                Write(requests.fixDataReq);
                                Thread.Sleep(ReqParams.Delay);
                            }
                        }
                    }
                    else // используем команду фиксации по адресу счетчика
                    {
                        Request(requests.fixDataReq, 4);
                        if (lastCommSucc && prop.opencnl)
                        {
                            code = inBuf[1];
                        }
                        else
                        {
                            code = 0x10;
                            string er = string.Concat(CommPhrases.ResponseError, "  ", ToLogString(code));
                            WriteToLog(er);
                        }
                    }
                }

                // --------- формирование запросов P,Q,S,U,I и энергия от сброса при параметре 14h или 16h
                for (int f = 0; f < nbwri.Length; f++)
                {
                    int bwrim = nbwri[f] & 0xf0;

                    requests.dataReq = Protocol.DataReq(Address, readparam, nbwri[f]);

                    Request(requests.dataReq, nb_length[f]);

                    if (lastCommSucc && prop.opencnl)
                    {
                        for (int zn = 0; zn < nparc[f]; zn++)
                        {
                            byte[] zn_temp = new byte[4];
                            uint znac_temp = 0;

                            if (nparb[f] == 4)
                            {
                                Array.Copy(inBuf, znx, zn_temp, 0, nparb[f]);                       // Копирование количества байт nparb[f] во временный буфер
                                znac_temp = BitConverter.ToUInt32(zn_temp, 0);

                                znac_temp = BitFunc.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x00 && (znac_temp & 0x80000000) >= 1) Napr = -1;   // определение направления Активной   мощности
                                if (nbwri[f] == 0x04 && (znac_temp & 0x40000000) >= 1) Napr = -1;   // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffffff;              // наложение маски для удаления направления для получения значения
                            }
                            else
                            {   // тут исправления ошибка чтения мощности командой 16h
                                Array.Copy(inBuf, znx, zn_temp, 0, 1);
                                Array.Copy(inBuf, znx + 1, zn_temp, 2, 2);
                                znac_temp = BitConverter.ToUInt32(zn_temp, 0);

                                znac_temp = BitFunc.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x00 && (znac_temp & 0x800000) >= 1) Napr = -1;   // определение направления Активной   мощности
                                if (nbwri[f] == 0x04 && (znac_temp & 0x400000) >= 1) Napr = -1;   // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffff;              // наложение маски для удаления направления для получения значения
                                if (nbwri[f] == 0x30) znac_temp = znac_temp & 0x3ff;              // наложение маски на 3-х байтовую переменную косинуса
                            }

                            if (znac_temp == 0xffffffff && nparb[f] == 4)
                            {
                                InvalidateCurData(tag - 1, 1);
                            }
                            else
                            {
                                znac = Convert.ToDouble(znac_temp) / nbwrc[f] * prop.parkui[f]; //получение значения с учетом разрещшающей способности
                                SetCurData(tag - 1, znac * Napr, 1);
                            }

                            znx = znx + nparb[f];
                            tag++;
                            Napr = 1;
                        }
                        znx = 1;
                    }
                    else
                    {
                        prop.opencnl = false;
                        CommonProps[address] = prop;

                        InvalidateCurData(tag - 1, nparc[f]);
                        tag = tag + nparc[f];
                        znx = 1;
                    }
                }
            }

            //------------Получить пофазные значения накопленной энергии прямого направления  код запросв 0x05, параметр 0x60
            if (energyL != 0)
            {
                for (int f = 0; f < nenergy.Length; f++)
                {
                    requests.energyPReq = Protocol.EnergyPReq(Address, nenergy[f]);
                    Request(requests.energyPReq, 15);

                    // Тут проверка ответа на корректность и разбор значений
                    if (lastCommSucc && prop.opencnl)
                    {
                        code = inBuf[2];
                        int znx = 1;
                        for (int zn = 0; zn < 3; zn++)
                        {
                            uint znac_temp = BitConverter.ToUInt32(inBuf, znx);
                            znac_temp = BitFunc.ROR(znac_temp, 16);
                            double znac = Convert.ToDouble(znac_temp) / 1000 * prop.ki;
                            SetCurData(tag - 1, znac, 1);
                            znx = znx + 4;
                            tag++;
                        }
                    }
                    else
                    {
                        code = 0x10;
                        string er = string.Concat(CommPhrases.ResponseError, "  ", ToLogString(code));
                        WriteToLog(er);

                        InvalidateCurData(tag - 1, 3);
                        tag = tag + 3;
                    }
                }


            }

            bool change = chan_err(code);

            if (change)
            {
                int even = 2;
                if (code != 0) even = 15;
                // генерация события
                KPEvent kpEvent = new KPEvent(DateTime.Now, Number, KPTags[tag - 1]);
                kpEvent.NewData = new SrezTableLight.CnlData(curData[tag - 1].Val, even);
                kpEvent.Descr = ToLogString(code);
                AddEvent(kpEvent);
                CommLineSvc.FlushArcData(this);
                change = false;
                code_err = code;
            }
            SetCurData(tag - 1, code, 1);
            tag++;
            SetCurData(tag - 1, prop.ki, 1);
            tag++;
            SetCurData(tag - 1, prop.ku, 1);

            tag = 1;

            CalcSessStats(); // расчёт статистики
        }

        //-------------------------
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);

            CalcCmdStats(); // расчёт статистики
        }

        private void Request(byte[] request, int Cnt)
        {
            int tryNum = 0;
            if (CommSucc) // Заменить на обработчик ошибок, которые не связаны с ошибками CRC, timeout и т.д.
            {
                lastCommSucc = false;
                while (RequestNeeded(ref tryNum))
                {
                    string logText;
                    WriteRequest(request, out logText);
                    WriteToLog(logText);

                    ReadAnswer(inBuf, Cnt, out logText);
                    WriteToLog(logText);

                    checkCRC(Cnt);

                    if (!lastCommSucc)
                    {
                        Thread.Sleep(ReqParams.Delay);
                    }
                    FinishRequest();
                    tryNum++;
                }
            }
        }

        private void checkCRC(int count)
        {
            if (read_cnt > 0)
            {
                ushort crc = CrcFunc.CalcCRC16(inBuf, count);
                if (crc == 0)
                {
                    WriteToLog(CommPhrases.ResponseOK);
                    lastCommSucc = true;
                }
                else WriteToLog(CommPhrases.ResponseCrcError);
            }
            else WriteToLog(CommPhrases.ResponseError);
        }

        private class Requests
        {
            public byte[] testCnlReq;   // запрос тестирования канала

            public byte[] openCnlReq;   // запрос на открытие канала

            public byte[] fixDataReq;   // запрос на фиксацию данных

            public byte[] dataReq;      // запрос чтения данных

            public byte[] energyPReq;   // запрос на чтение пофазной энергии А+ (0x05 0x60 тариф)

            public byte[] kuiReq;       // запрос значений трансформации тока и напряжения

            public byte[] infoReq;      // Запрос информации счетчика в ускоренном режиме (0x08 0x01) - 16 байт

            public Requests()
            {
            }
        }

        private void tagcreate(TagGroup taggroup, int idgr)
        {
            for (int i = 0; i < devTemplate.SndGroups[idgr].value.Count; i++)
            {
                Allname = string.Concat(devTemplate.SndGroups[idgr].Name, " ", devTemplate.SndGroups[idgr].value[i].name);
                taggroup.KPTags.Add(new KPTag(devTemplate.SndGroups[idgr].value[i].signal, Allname));
            }
        }

    }
}

