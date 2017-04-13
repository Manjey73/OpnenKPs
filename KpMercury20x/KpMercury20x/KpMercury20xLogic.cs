using Scada.Comm.Channels;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml;

namespace Scada.Comm.Devices
{

    public sealed class KpMercury20xLogic : KPLogic
    {
        string address; //входная строка для обработки, объявление переменной для пароля и уровня доступа
        string[] s_out = new string[3]; // массив введенных значений или значений по умолчанию
        bool Group_1, Group_2, Group_3, Group_4, Group_5, Group_6;
        bool slog = true;
        int tag = 0;

        /// <summary>
        /// Вызвать метод записи в журнал
        /// </summary>
        private void ExecWriteToLog(string text)
        {
            if (WriteToLog != null)
                WriteToLog(text);
        }

        public long BcdToDec(byte[] bcdNumber)
        {
            long result = 0;
            foreach (byte b in bcdNumber)
            {
                int digit1 = b >> 4;
                int digit2 = b & 0x0f;
                result = (result * 100) + digit1 * 10 + digit2;
            }
            return result;
        }

        private readonly static byte[] CRCHiTable;

        private readonly static byte[] CRCLoTable;

        static KpMercury20xLogic()
        {
            KpMercury20xLogic.CRCHiTable = new byte[] { 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64 };
            KpMercury20xLogic.CRCLoTable = new byte[] { 0, 192, 193, 1, 195, 3, 2, 194, 198, 6, 7, 199, 5, 197, 196, 4, 204, 12, 13, 205, 15, 207, 206, 14, 10, 202, 203, 11, 201, 9, 8, 200, 216, 24, 25, 217, 27, 219, 218, 26, 30, 222, 223, 31, 221, 29, 28, 220, 20, 212, 213, 21, 215, 23, 22, 214, 210, 18, 19, 211, 17, 209, 208, 16, 240, 48, 49, 241, 51, 243, 242, 50, 54, 246, 247, 55, 245, 53, 52, 244, 60, 252, 253, 61, 255, 63, 62, 254, 250, 58, 59, 251, 57, 249, 248, 56, 40, 232, 233, 41, 235, 43, 42, 234, 238, 46, 47, 239, 45, 237, 236, 44, 228, 36, 37, 229, 39, 231, 230, 38, 34, 226, 227, 35, 225, 33, 32, 224, 160, 96, 97, 161, 99, 163, 162, 98, 102, 166, 167, 103, 165, 101, 100, 164, 108, 172, 173, 109, 175, 111, 110, 174, 170, 106, 107, 171, 105, 169, 168, 104, 120, 184, 185, 121, 187, 123, 122, 186, 190, 126, 127, 191, 125, 189, 188, 124, 180, 116, 117, 181, 119, 183, 182, 118, 114, 178, 179, 115, 177, 113, 112, 176, 80, 144, 145, 81, 147, 83, 82, 146, 150, 86, 87, 151, 85, 149, 148, 84, 156, 92, 93, 157, 95, 159, 158, 94, 90, 154, 155, 91, 153, 89, 88, 152, 136, 72, 73, 137, 75, 139, 138, 74, 78, 142, 143, 79, 141, 77, 76, 140, 68, 132, 133, 69, 135, 71, 70, 134, 130, 66, 67, 131, 65, 129, 128, 64 };
        }
        //-------------------------



        public override void OnAddedToCommLine()
        {
            base.OnAddedToCommLine();

            s_out = Parametrs.Parametr(ReqParams.CmdLine.Trim());

            address = s_out[0];

            if (s_out[2] == "0") slog = false;

            int chanel = Convert.ToInt32(s_out[1], 10);
            Group_1 = Convert.ToBoolean(Parametrs.GetBit(chanel, 0));
            Group_2 = Convert.ToBoolean(Parametrs.GetBit(chanel, 1));
            Group_3 = Convert.ToBoolean(Parametrs.GetBit(chanel, 2));
            Group_4 = Convert.ToBoolean(Parametrs.GetBit(chanel, 3));
            Group_5 = Convert.ToBoolean(Parametrs.GetBit(chanel, 4));
            Group_6 = Convert.ToBoolean(Parametrs.GetBit(chanel, 5));


            List<TagGroup> tagGroups = new List<TagGroup>();

            TagGroup tagGroup;

            if (Group_1)
            {
                tagGroup = new TagGroup("Основные параметры:");
                tagGroup.KPTags.Add(new KPTag(1, "Напряжение (В)"));
                tagGroup.KPTags.Add(new KPTag(2, "Ток (А)"));
                tagGroup.KPTags.Add(new KPTag(3, "Мощность активная (Вт)"));
                tagGroups.Add(tagGroup);
            }
            if (Group_2 || Group_3 || Group_4)
            {
                tagGroup = new TagGroup("Дополнительные параметры:");
                if (Group_2)
                {
                    tagGroup.KPTags.Add(new KPTag(4, "Мощность реактивная (Вар)"));
                }
                if (Group_3)
                {
                    tagGroup.KPTags.Add(new KPTag(5, "Мощность полная (ВА)"));
                    tagGroup.KPTags.Add(new KPTag(6, "COS"));
                }
                if (Group_4)
                {
                    tagGroup.KPTags.Add(new KPTag(7, "Частота (Гц)"));
                }
                tagGroups.Add(tagGroup);
            }
            if (Group_5 || Group_6)
            {
                tagGroup = new TagGroup("Энергия от сброса:");
                if (Group_5)
                {
                    tagGroup.KPTags.Add(new KPTag(8, "Активная Т1, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(9, "Активная Т2, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(10, "Активная Т3, (кВт*ч)"));
                    tagGroup.KPTags.Add(new KPTag(11, "Активная Т4, (кВт*ч)"));
                }
                if (Group_6)
                {
                    tagGroup.KPTags.Add(new KPTag(12, "Реактивная Т1, (кВар*ч)"));
                    tagGroup.KPTags.Add(new KPTag(13, "Реактивная Т2, (кВар*ч)"));
                    tagGroup.KPTags.Add(new KPTag(14, "Реактивная Т3, (кВар*ч)"));
                    tagGroup.KPTags.Add(new KPTag(15, "Реактивная Т4, (кВар*ч)"));
                }
                tagGroups.Add(tagGroup);
            }
            InitKPTags(tagGroups);
        }


        public KpMercury20xLogic(int number) : base(number)
        {
        }
        //-------------------------
       
           public static ushort CalcCRC16(byte[] buffer, int length)
        {
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
            byte[] buf_out = BitConverter.GetBytes(Convert.ToInt32(address, 10)); //получить адрес во входной массив
            int i = buf_out.Length; //получить размер массива с адресом
            if (BitConverter.IsLittleEndian) //развернуть массива адреса при необходимости
            {
                Array.Reverse(buf_out, 0, sizeof(Int32));
                BitConverter.ToInt32(buf_out, 0);
            }
            byte[] buf_in; //резервирование массива получения
            ushort crc;  //резервирование ответа контрольной суммы

                         //------------Получить I,U,P
            if (Group_1)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 3); //изменить размер массива
                buf_out[i] = 0x63; //команда
                crc = CalcCRC16(buf_out, i + 1); //получить контрольную сумму
                buf_out[i + 1] = (byte)(crc % 256);
                buf_out[i + 2] = (byte)(crc / 256);
                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);

                buf_in = new byte[14];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[3];
                    Array.Copy(buf_in, 5, znactmp, 0, 2);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 1000, 1); tag++;
                    Array.Copy(buf_in, 7, znactmp, 0, 2);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 10000, 1); tag++;
                    Array.Copy(buf_in, 9, znactmp, 0, 3);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)), 1); tag++;
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
            

            //------------Получить мощность реактивную
            if (Group_2)
            {
            lastCommSucc = false;
            string logText;
            Array.Resize(ref buf_out, i + 4); //изменить размер массива
            buf_out[i] = 0x86; //команда
            buf_out[i + 1] = 0x00; //параметр
            crc = CalcCRC16(buf_out, i + 2); //получить контрольную сумму
            buf_out[i + 2] = (byte)(crc % 256);
            buf_out[i + 3] = (byte)(crc / 256);
            Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
            buf_in = new byte[11];
            Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);

                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[3];
                    Array.Copy(buf_in, 6, znactmp, 0, 3);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)), 1); tag++;
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
            

            //------------Получить COS, мощность полную
            if (Group_3)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 4); //изменить размер массива
            buf_out[i] = 0x86; //команда
            buf_out[i + 1] = 0x02; //параметр
            crc = CalcCRC16(buf_out, i + 2); //получить контрольную сумму
            buf_out[i + 2] = (byte)(crc % 256);
            buf_out[i + 3] = (byte)(crc / 256);
            Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
            buf_in = new byte[13];
            Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[3];
                    Array.Copy(buf_in, 8, znactmp, 0, 3);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)), 1); tag++; // Мощность полная
                    Array.Copy(buf_in, 6, znactmp, 0, 2);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100000, 1); tag++; //косинус фи
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
           
            //------------Получить частоту
            if (Group_4)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 3); //изменить размер массива
            buf_out[i] = 0x81; //команда
            crc = CalcCRC16(buf_out, i + 1); //получить контрольную сумму
            buf_out[i + 1] = (byte)(crc % 256);
            buf_out[i + 2] = (byte)(crc / 256);
            Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
            buf_in = new byte[17];
            Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта

                if (slog) ExecWriteToLog(logText);
                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[3];
                    Array.Copy(buf_in, 5, znactmp, 0, 2);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 10000, 1); tag++; // Частота
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
            
            //------------Получить активную энергию
            if (Group_5)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 3); //изменить размер массива
            buf_out[i] = 0x27; //команда
            crc = CalcCRC16(buf_out, i + 1); //получить контрольную сумму
            buf_out[i + 1] = (byte)(crc % 256);
            buf_out[i + 2] = (byte)(crc / 256);
            Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
            buf_in = new byte[23];
            Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[4];
                    Array.Copy(buf_in, 5, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Активная энергия тариф 1
                    Array.Copy(buf_in, 9, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Активная энергия тариф 2
                    Array.Copy(buf_in, 13, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Активная энергия тариф 3 Реактивная энергия тариф 1
                    Array.Copy(buf_in, 17, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Активная энергия тариф 4
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
            
            //------------Получить реактивную энергию
            if (Group_6)
            {
                lastCommSucc = false;
                string logText;
                Array.Resize(ref buf_out, i + 3); //изменить размер массива
            buf_out[i] = 0x85; //команда
            crc = CalcCRC16(buf_out, i + 1); //получить контрольную сумму
            buf_out[i + 1] = (byte)(crc % 256);
            buf_out[i + 2] = (byte)(crc / 256);
            Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) ExecWriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
            buf_in = new byte[23];
            Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) ExecWriteToLog(logText);
                crc = CalcCRC16(buf_in, buf_in.Length);
                if (crc == 0)
                {
                    byte[] znactmp = new byte[4];
                    Array.Copy(buf_in, 5, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Реактивная энергия тариф 1
                    Array.Copy(buf_in, 9, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Реактивная энергия тариф 2
                    Array.Copy(buf_in, 13, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Реактивная энергия тариф 3
                    Array.Copy(buf_in, 17, znactmp, 0, 4);
                    SetCurData(tag, Convert.ToDouble(BcdToDec(znactmp)) / 100, 1); tag++; // Реактивная энергия тариф 4
                    if (slog) ExecWriteToLog("OK!");
                    lastCommSucc = true;
                }
                else
                {
                    if (slog) ExecWriteToLog("Oшибка!");
                }
                FinishRequest();
            }
            
            CalcSessStats(); // расчёт статистики
        }
        //-------------------------
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);


//            CalcCmdStats(); // расчёт статистики
        }

    }
}

