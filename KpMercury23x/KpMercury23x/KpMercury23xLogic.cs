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


namespace Scada.Comm.Devices
{

    public sealed class KpMercury23xLogic : KPLogic
    {

        public static long Ticks() // возвращает время в миллисекундах
        {
            DateTime now = DateTime.Now;
            long time = now.Ticks / 10000;
            return time;
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

        /// <summary>
        /// Вызвать метод записи в журнал
        /// </summary>
        private void ExecWriteToLog(string text)
        {
            if (WriteToLog != null)
                WriteToLog(text);
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

        // Массив значений параметров BWRI счетчика + 'энергии от сброса
        int[] bwri = new int[]     { 0x00, 0x04, 0x08, 0x30, 0x10, 0x20, 0x50, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 14h
        int[] bwrc = new int[]     { 100,  100,  100,  1000, 100,  1000, 100,  100,  1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        int[] b_length = new int[] { 19,   19,   19,   15,   12,   12,   12,   6,    19,   19,   19,   19,   19 };   // количество байт в ответе счетчика
        int[] parb = new int[]     { 4,    4,    4,    3,    3,    3,    3,    3,    4,    4,    4,    4,    4 };    // количество байт в параметре ответа (4 или 3)
        int[] parc = new int[]     { 4,    4,    4,    4,    3,    3,    3,    1,    4,    4,    4,    4,    4 };    // количество параметров в ответе (4, 3 или 1)

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

        // Массив значений энергии по фазам прямого направления
        int[] massenergy = new int[] { 0, 1, 2, 3, 4 };

        bool testcnl = false;
        bool opencnl = false;
        bool Kui = false;
        bool newmass = false;
        bool newenergy = false;
        long t1 = 0;
        int ki = 1; // Коэффициент трансформации тока
        int ku = 1; // Коэффициент трансформации напряжения
        int tag = 1;
        int[] nbwri = new int[1];
        int[] nbwrc = new int[1];
        int[] nb_length = new int[1];
        int[] nparb = new int[1];
        int[] nparc = new int[1];
        int[] nenergy = new int[1];
        byte code_err = 0x0;
        byte code = 0x0;
        int ennapr = 1;
        int mask_g1; // Входная переменная для выбора тегов
        bool slog = true;


        string password, uroven; //входная строка для обработки, объявление переменной для пароля и уровня доступа
        string[] s_out = new string[4]; // массив введенных значений или значений по умолчанию
//       int i_curr, i_prev, j_curr; //текущее и предыдущее значение индекса литеры в строке, текущее значение индекса слова в строке


        private readonly static byte[] CRCHiTable;

        private readonly static byte[] CRCLoTable;

        static KpMercury23xLogic()
        {
            KpMercury23xLogic.CRCHiTable = new byte[] { 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64 };
            KpMercury23xLogic.CRCLoTable = new byte[] { 0, 192, 193, 1, 195, 3, 2, 194, 198, 6, 7, 199, 5, 197, 196, 4, 204, 12, 13, 205, 15, 207, 206, 14, 10, 202, 203, 11, 201, 9, 8, 200, 216, 24, 25, 217, 27, 219, 218, 26, 30, 222, 223, 31, 221, 29, 28, 220, 20, 212, 213, 21, 215, 23, 22, 214, 210, 18, 19, 211, 17, 209, 208, 16, 240, 48, 49, 241, 51, 243, 242, 50, 54, 246, 247, 55, 245, 53, 52, 244, 60, 252, 253, 61, 255, 63, 62, 254, 250, 58, 59, 251, 57, 249, 248, 56, 40, 232, 233, 41, 235, 43, 42, 234, 238, 46, 47, 239, 45, 237, 236, 44, 228, 36, 37, 229, 39, 231, 230, 38, 34, 226, 227, 35, 225, 33, 32, 224, 160, 96, 97, 161, 99, 163, 162, 98, 102, 166, 167, 103, 165, 101, 100, 164, 108, 172, 173, 109, 175, 111, 110, 174, 170, 106, 107, 171, 105, 169, 168, 104, 120, 184, 185, 121, 187, 123, 122, 186, 190, 126, 127, 191, 125, 189, 188, 124, 180, 116, 117, 181, 119, 183, 182, 118, 114, 178, 179, 115, 177, 113, 112, 176, 80, 144, 145, 81, 147, 83, 82, 146, 150, 86, 87, 151, 85, 149, 148, 84, 156, 92, 93, 157, 95, 159, 158, 94, 90, 154, 155, 91, 153, 89, 88, 152, 136, 72, 73, 137, 75, 139, 138, 74, 78, 142, 143, 79, 141, 77, 76, 140, 68, 132, 133, 69, 135, 71, 70, 134, 130, 66, 67, 131, 65, 129, 128, 64 };
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------

        public override void OnAddedToCommLine()
        {
            base.OnAddedToCommLine();

            s_out = Parametrs.Parametr(ReqParams.CmdLine.Trim());
            password = s_out[0];
            mask_g1 = Convert.ToInt32(s_out[1], 10);
            uroven = s_out[2];
            if (s_out[3] == "0") slog = false;

            int mgn_znac = mask_g1 & 0xFF; // отсечь мгновенные значения для организации отображения тегов
            int energy = mask_g1 & 0x3FF00; // Отсечь параметры энергии для организации отображения тегов

            List<TagGroup> tagGroups = new List<TagGroup>();
            TagGroup tagGroup;

            if (mgn_znac != 0)
            {
                tagGroup = new TagGroup("Мгновенные значения:");

                bool mgn_P = Parametrs.GetBit(mask_g1, 0) > 0;
                if (mgn_P)
                {
                    tagGroup.KPTags.Add(new KPTag(1, "Мощность P Сумм (Вт)"));
                    tagGroup.KPTags.Add(new KPTag(2, "Мощность P L1   (Вт)"));
                    tagGroup.KPTags.Add(new KPTag(3, "Мощность P L2   (Вт)"));
                    tagGroup.KPTags.Add(new KPTag(4, "Мощность P L3   (Вт)"));
                }
                bool mgn_Q = Parametrs.GetBit(mask_g1, 1) > 0;
                if (mgn_Q)
                {
                    tagGroup.KPTags.Add(new KPTag(5, "Мощность Q Сумм (вар)"));
                    tagGroup.KPTags.Add(new KPTag(6, "Мощность Q L1   (вар)"));
                    tagGroup.KPTags.Add(new KPTag(7, "Мощность Q L2   (вар)"));
                    tagGroup.KPTags.Add(new KPTag(8, "Мощность Q L3   (вар)"));
                }
                bool mgn_S = Parametrs.GetBit(mask_g1, 2) > 0;
                if (mgn_S)
                {
                    tagGroup.KPTags.Add(new KPTag(9, "Мощность S Сумм (ВА)"));
                    tagGroup.KPTags.Add(new KPTag(10, "Мощность S L1  (ВА)"));
                    tagGroup.KPTags.Add(new KPTag(11, "Мощность S L2  (ВА)"));
                    tagGroup.KPTags.Add(new KPTag(12, "Мощность S L3  (ВА)"));
                }
                bool mgn_cos = Parametrs.GetBit(mask_g1, 3) > 0;
                if (mgn_cos)
                {
                    tagGroup.KPTags.Add(new KPTag(13, "COSф Сумм"));
                    tagGroup.KPTags.Add(new KPTag(14, "COSф L1"));
                    tagGroup.KPTags.Add(new KPTag(15, "COSф L2"));
                    tagGroup.KPTags.Add(new KPTag(16, "COSф L3"));
                }
                bool mgn_U = Parametrs.GetBit(mask_g1, 4) > 0;
                if (mgn_U)
                {
                    tagGroup.KPTags.Add(new KPTag(17, "Напряжение L1 (В)"));
                    tagGroup.KPTags.Add(new KPTag(18, "Напряжение L2 (В)"));
                    tagGroup.KPTags.Add(new KPTag(19, "Напряжение L3 (В)"));
                }
                bool mgn_I = Parametrs.GetBit(mask_g1, 5) > 0;
                if (mgn_I)
                {
                    tagGroup.KPTags.Add(new KPTag(20, "Ток L1 (А)"));
                    tagGroup.KPTags.Add(new KPTag(21, "Ток L2 (А)"));
                    tagGroup.KPTags.Add(new KPTag(22, "Ток L3 (А)"));
                }
                bool mgn_FU = Parametrs.GetBit(mask_g1, 6) > 0;
                if (mgn_FU)
                {
                    tagGroup.KPTags.Add(new KPTag(23, "Угол м-ду ф. 1 и 2"));
                    tagGroup.KPTags.Add(new KPTag(24, "Угол м-ду ф. 1 и 3"));
                    tagGroup.KPTags.Add(new KPTag(25, "Угол м-ду ф. 2 и 3"));
                }
                bool mgn_F = Parametrs.GetBit(mask_g1, 7) > 0;
                if (mgn_F)
                {
                    tagGroup.KPTags.Add(new KPTag(26, "Частота (Гц)"));
                }
                tagGroups.Add(tagGroup);
            }

            if (energy != 0)
            {
                tagGroup = new TagGroup("Энергия от сброса:");

                bool en_summ = Parametrs.GetBit(mask_g1, 8) > 0;
                if (en_summ)
                {
                    tagGroup.KPTags.Add(new KPTag(27, "Сумма А+,  (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(28, "Сумма А-,  (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(29, "Сумма R+,  (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(30, "Сумма R-,  (кВт*ч)"));
                }
                bool en_tar1 = Parametrs.GetBit(mask_g1, 9) > 0;
                if (en_tar1)
                {
                    tagGroup.KPTags.Add(new KPTag(31, "Тариф 1 А+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(32, "Тариф 1 А-, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(33, "Тариф 1 R+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(34, "Тариф 1 R-, (кВт*ч)"));
                }
                bool en_tar2 = Parametrs.GetBit(mask_g1, 10) > 0;
                if (en_tar2)
                {
                    tagGroup.KPTags.Add(new KPTag(35, "Тариф 2 А+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(36, "Тариф 2 А-, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(37, "Тариф 2 R+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(38, "Тариф 2 R-, (кВт*ч)"));
                }
                bool en_tar3 = Parametrs.GetBit(mask_g1, 11) > 0;
                if (en_tar3)
                {
                    tagGroup.KPTags.Add(new KPTag(39, "Тариф 3 А+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(40, "Тариф 3 А-, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(41, "Тариф 3 R+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(42, "Тариф 3 R-, (кВт*ч)"));
                }
                bool en_tar4 = Parametrs.GetBit(mask_g1, 12) > 0;
                if (en_tar4)
                {
                    tagGroup.KPTags.Add(new KPTag(43, "Тариф 4 А+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(44, "Тариф 4 А-, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(45, "Тариф 4 R+, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(46, "Тариф 4 R-, (кВт*ч)"));
                }
                bool enL_summ = Parametrs.GetBit(mask_g1, 13) > 0;
                if (enL_summ)
                {
                    tagGroup.KPTags.Add(new KPTag(47, "Сумма А+   (L1), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(48, "Сумма А+   (L2), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(49, "Сумма А+   (L3), (кВт*ч)"));
                }
                bool enL_tar1 = Parametrs.GetBit(mask_g1, 14) > 0;
                if (enL_tar1)
                {
                    tagGroup.KPTags.Add(new KPTag(50, "Тариф 1 А+ (L1), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(51, "Тариф 1 А+ (L2), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(52, "Тариф 1 А+ (L3), (кВт*ч)"));
                }
                bool enL_tar2 = Parametrs.GetBit(mask_g1, 15) > 0;
                if (enL_tar2)
                {
                    tagGroup.KPTags.Add(new KPTag(53, "Тариф 2 А+ (L1), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(54, "Тариф 2 А+ (L2), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(55, "Тариф 2 А+ (L3), (кВт*ч)"));
                }
                bool enL_tar3 = Parametrs.GetBit(mask_g1, 16) > 0;
                if (enL_tar3)
                {
                    tagGroup.KPTags.Add(new KPTag(56, "Тариф 3 А+ (L1), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(57, "Тариф 3 А+ (L2), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(58, "Тариф 3 А+ (L3), (кВт*ч)"));
                }
                bool enL_tar4 = Parametrs.GetBit(mask_g1, 17) > 0;
                if (enL_tar4)
                {
                    tagGroup.KPTags.Add(new KPTag(59, "Тариф 4 А+ (L1), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(60, "Тариф 4 А+ (L2), (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(61, "Тариф 4 А+ (L3), (кВт*ч)"));
                }
                tagGroups.Add(tagGroup);
            }
            {
                tagGroup = new TagGroup("Статус:");
                tagGroup.KPTags.Add(new KPTag(70, "Код ошибки:"));
                tagGroups.Add(tagGroup);
            }
            InitKPTags(tagGroups);
        
        }


        //---------------------------------------------------------------
        public KpMercury23xLogic(int number) : base(number)
        {
        }
        //---------------------------------------------------------------



        public static ushort CalcCRC16(byte[] buffer, int length)
        {
            //int length = buffer.Length;
            int offset = 0;
            byte crcHi = 0xFF;   // high byte of CRC initialized
            byte crcLo = 0xFF;   // low byte of CRC initialized
            int index;           // will index into CRC lookup table

            while (length-- > 0) // pass through message buffer
            {
                index = crcLo ^ buffer[offset++]; // calculate the CRC
                crcLo = (byte)(crcHi ^ CRCHiTable[index]);
                crcHi = CRCLoTable[index];
            }
            return (ushort)((crcHi << 8) | crcLo);
        }
//-------------------------
        public override void Session()
        {

            base.Session();
            uint par14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // отсечь количество параметров для команды 08h и параметра 14h

            uint energyL = Convert.ToUInt32(mask_g1 & 0x3E000); // Проверка наличия опроса значений энергии прямого направления
            energyL = Parametrs.ROR(energyL, 13);

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


            byte[] buf_out = new byte[1];
            buf_out[0] = Convert.ToByte(Address);
            int i = buf_out.Length; //получить размер массива с адресом
            byte[] buf_in; //резервирование массива получения
            ushort res;  //резервирование ответа контрольной суммы
 
            if (!testcnl)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 3); //изменить размер массива
                buf_out[i] = 0x00; //команда запроса на тестирование канала
                res = CalcCRC16(buf_out, i + 1); //получить контрольную сумму
                buf_out[i + 1] = (byte)(res % 256); //Добавить контрольную сумму к буферу посылки
                buf_out[i + 2] = (byte)(res / 256);
                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);

                buf_in = new byte[4];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                if (CalcCRC16(buf_in, buf_in.Length) == 0)
                {
                    testcnl = true;
                    code = buf_in[1];
                    lastCommSucc = true;
                    if (slog) ExecWriteToLog("OK!");
                }
                else
                {
                    code = 0x11;
                    WriteToLog(ToLogString(code));
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }

            if (testcnl) // Открытие канала с уровнем доступа согласно введенного пароля. (на данный момент уровень 1 - пользовательский)
            {
                long t2 = Ticks();
                if ((t2 > (t1 + 240000)) || !opencnl)
                {
                    t1 = t2;
                    // Запрос на открытие канала
                    lastCommSucc = false;
                    string logText;
                    byte[] temp_pass = new byte[4];
                    byte[] buf_pass = Encoding.ASCII.GetBytes(password);

                    for (int f = 0; f < 6; f++)
                    {
                        Array.Copy(buf_pass, f, temp_pass, 0, 1);
                        int temp_int = BitConverter.ToInt32(temp_pass, 0) - 48;
                        temp_pass = BitConverter.GetBytes(temp_int);
                        Array.Copy(temp_pass, 0, buf_pass, f, 1);
                    }

                    Array.Resize(ref buf_out, 11); //изменить размер массива
                    buf_out[1] = 0x01; //команда запроса на открытие канала
                    buf_out[2] = Convert.ToByte(Convert.ToInt16(uroven)); // Ввод уровня доступа пока без проверки, по умолчанию 1.... 0x01; //Уровень доступа 1
                    Array.Copy(buf_pass, 0, buf_out, 3, 6);
                    res = CalcCRC16(buf_out, buf_out.Length - 2); //получить контрольную сумму
                    buf_out[buf_out.Length - 2] = (byte)(res % 256); //Добавить контрольную сумму к буферу посылки
                    buf_out[buf_out.Length - 1] = (byte)(res / 256);
                    Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) ExecWriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);
                    buf_in = new byte[4];
                    Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) ExecWriteToLog(logText);

                    if (CalcCRC16(buf_in, buf_in.Length) == 0)
                    {
                        // тут проверка на корректность пароля, смена статуса открытого канала
                        if (buf_in[1] == 0)
                        {
                            code = buf_in[1];
                            opencnl = true;
                            lastCommSucc = true;
                            if (slog) ExecWriteToLog("OK!");
                        }
                        else
                        {
                            code = buf_in[1];
                            WriteToLog(ToLogString(code));
                            lastCommSucc = true;
                            if (slog) ExecWriteToLog("OK!");
                        }
                    }
                    else
                    {
                        code = 0x10;
                        WriteToLog(ToLogString(code));
                        if (slog) ExecWriteToLog("Oшибка!");
                    }
                    if (Convert.ToInt32(buf_in[1]) == 0x05) opencnl = false;
                    FinishRequest();
                }

            }

            // Запрос коэффициентов трансформации напряжения и тока

            if (opencnl && !Kui)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, 5); //изменить размер массива
                buf_out[1] = 0x08; // 2.3 Запрос на чтение параметров
                buf_out[2] = 0x02; // 2.3.3 Прочитать коэффициент трансформации счетчика
                res = CalcCRC16(buf_out, buf_out.Length - 2); //получить контрольную сумму
                buf_out[buf_out.Length - 2] = (byte)(res % 256); //Добавить контрольную сумму к буферу посылки
                buf_out[buf_out.Length - 1] = (byte)(res / 256);
                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);

                buf_in = new byte[7];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                // тут проверка CRC и корректности ответа

                if (CalcCRC16(buf_in, buf_in.Length) == 0)
                {
                    Kui = true;
                    ku = Convert.ToInt32(Parametrs.ROR (BitConverter.ToUInt16(buf_in, 1), 8));
                    ki = Convert.ToInt32(Parametrs.ROR (BitConverter.ToUInt16(buf_in, 3), 8));
                    lastCommSucc = true;
                    if (slog) ExecWriteToLog("OK!");
                }
                else
                {
                    WriteToLog("Недопустимая команда");
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }

            int[] parkui = new int[] { ki, ki, ki, 1, ku, ki, 1, 1, ki, ki, ki, ki, ki };


            // ------------Получить мгновенные значения P,Q,S,U,I вариант 2
            uint com14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // проверка маски на необходимость чтения команды 08h с параметром 14h


            if (com14h != 0)  //((com14h != 0) && opencnl)
            {
                lastCommSucc = false;
                string logText;
                int znx = 1; // начальное положение первого массива байт в ответе
                float znac = 0;          
                Array.Resize(ref buf_out, i + 4); //изменить размер массива
                buf_out[i] = 0x03; i++; // 1.3 Запрос на запись параметров
                buf_out[i] = 0x08; i++; // 08h фиксация данных.
                res = CalcCRC16(buf_out, 3); //получить контрольную сумму
                buf_out[i] = (byte)(res % 256); i++;
                buf_out[i] = (byte)(res / 256);

                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
                buf_in = new byte[4];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                // тут проверка CRC ответа и успешности команды фиксации данных

                if (CalcCRC16(buf_in, buf_in.Length) == 0 && opencnl)
                {
                    code = buf_in[1];
                    lastCommSucc = true;
                    if (slog) ExecWriteToLog("OK!");
                }
                else
                {
                    code = 0x10;
                    WriteToLog(ToLogString(code));
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();


                i = 1;
                Array.Resize(ref buf_out, i + 5); //изменить размер массива

                buf_out[i] = 0x08; i++; // 2.3 Запрос на чтение параметров
                buf_out[i] = 0x14; i++; // 14h Чтение зафиксированных вспомогательных параметров: мгновенной активной, реактивной, полной мощности, напряжения, тока, коэффициента мощности и частоты.

                for (int f = 0; f < nbwri.Length; f++)
                {
                    lastCommSucc = false;
                    int bwrim = nbwri[f] & 0xf0;
                    byte[] zn_temp = new byte[4];
                    buf_out[i] = Convert.ToByte(nbwri[f]); i++; // Запись BWRI кода
                    res = CalcCRC16(buf_out, 4); //получить контрольную сумму
                    buf_out[i] = (byte)(res % 256); i++;
                    buf_out[i] = (byte)(res / 256);

                    Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) ExecWriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);

                    buf_in = new byte[nb_length[f]];

                    Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) ExecWriteToLog(logText);
                    // тут проверка CRC ответа

                    if (CalcCRC16(buf_in, buf_in.Length) == 0 && opencnl)
                    {
                        lastCommSucc = true;
                        if (slog) ExecWriteToLog("OK!");
                        for (int zn = 0; zn < nparc[f]; zn++)
                        {
                            Array.Copy(buf_in, znx, zn_temp, 0, nparb[f]);
                            uint znac_temp = BitConverter.ToUInt32(zn_temp, 0);
                            if (nparb[f] == 4)
                            {
                                znac_temp = Parametrs.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x04 && (znac_temp & 0x40000000) >= 1) ennapr = -1; // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffffff; // наложение маски для удаления направления для получения значения
                            }
                            else
                            {
                                znac_temp = Parametrs.ROR(znac_temp, 8);
                                //if (nbwri[f] == 0x30 && (znac_temp & 0x400) >= 1) ennapr = -1;
                                if (nbwri[f] == 0x30) znac_temp = znac_temp & 0x3ff; // наложение маски на 3-х байтовую переменную косинуса
                            }
                            if (znac_temp == 0xffffffff && nparb[f] == 4)
                            {
                                znac = Convert.ToSingle(double.NaN);
                            }
                            else znac = Convert.ToSingle(znac_temp) / nbwrc[f] * parkui[f]; //получение значения с учетом разрещшающей способности
                            SetCurData(tag - 1, znac*ennapr, 1);
                            znx = znx + nparb[f];
                            tag++;
                            ennapr = 1;
                        }
                        i = 3;
                        znx = 1;
                    }
                    else
                    {
                        lastCommSucc = false;
                        opencnl = false;
                        if (slog) ExecWriteToLog("Oшибка!");
                        for (int zn = 0; zn < nparc[f]; zn++)
                        {
                                SetCurData(tag - 1, 0, 0);
                                tag++;
                        }
                        i = 3;
                        znx = 1;
                    }
                    FinishRequest();
                }
            }



            //------------Получить пофазные значения накопленной энергии прямого направления  0x60h
    
            if (energyL != 0)
            {

                lastCommSucc = false;
                string logText;
                i = 1;
                Array.Resize(ref buf_out, i + 5); //изменить размер массива
                buf_out[i] = 0x05; i++; // 2.2 Запросы на чтение массивов регистров накопленной энергии

                   buf_out[i] = 0x60; i++; // Параметр чтения накопленной энергии A+ от сброса по фазам

                    for (int f = 0; f < nenergy.Length; f++)
                    {
                        buf_out[i] = Convert.ToByte(nenergy[f]); i++;
                        res = CalcCRC16(buf_out, 4); //получить контрольную сумму
                        buf_out[i] = (byte)(res % 256); i++;
                        buf_out[i] = (byte)(res / 256);
                        Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) ExecWriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);
                        buf_in = new byte[15];
                        Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) ExecWriteToLog(logText);
                    // Тут проверка ответа на корректность

                    int znx = 1;
                        for (int zn = 0; zn < 3; zn++)
                        {
                        uint znac_temp = BitConverter.ToUInt32(buf_in, znx);
                            znac_temp = Parametrs.ROR(znac_temp, 16);                        
                            float znac = Convert.ToSingle(znac_temp) / 1000 * ki;
                            SetCurData(tag-1, znac, 1);
                            znx = znx + 4;
                            tag++;
                        }
                        i = 3;
                      }

                lastCommSucc = true;
                FinishRequest();
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


            tag = 1;
           
            CalcSessStats(); // расчёт статистики
        }

        //-------------------------
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);


            CalcCmdStats(); // расчёт статистики
        }

    }
}

