
namespace ScadaCommFunc
{
    public class CrcFunc
    {

        /// <summary>
        /// Рассчет однобайтовой контрольной суммы (применяется в MBus)
        /// Приборах АО НПФ Логика СПТ941, СПТ941.10/11, СПТ943
        /// с последующим инвертированием байта - применить в коде (byte)(~crc)
        /// Вызов Crc8 с передачей буфера байт в функцию
        /// </summary>
        /// <param name="bval"></param>
        /// <param name="cval"></param>
        /// <returns></returns>

        private static int F_crc_8(int bval, int cval)
        {
            return (bval + cval) % 256;
        }

        public static int Crc8(byte[] buffer, int offset = 0, int count = 0) // Расчет CRC по модулю 256
        {
            if (count == 0) count = buffer.Length;
            int crc = buffer[offset];
            for (int i = offset, last = offset + count; i < last - 1; i++)
            {
                crc = F_crc_8(crc, buffer[i + 1]);
            }
            return crc;
        }

        /// <summary>
        /// Рассчет CRC16/XMODEM - применяется в приборах АО НПФ Логика 
        /// Тепловычислители СПТ961, СПТ961М, СПТ961.1, СПТ961.2, СПТ962, СПТ963
        /// Корректоры расхода газа СПГ761, СПГ761.1, СПГ761.2, СПГ762, СПГ762.1, СПГ762.2, СПГ763, СПГ763.1, СПГ763.2
        /// Сумматоры электрической энергии и мощности СПЕ542
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static ushort CRC16_XModem(byte[] msg)
        {
            const ushort polinom = 0x1021;
            ushort code = 0x0000;

            for (int i = 0, size = msg.Length; i < size; ++i)
            {
                code ^= (ushort)(msg[i] << 8);
                for (uint j = 0; j < 8; ++j)
                {
                    if ((code & 0x8000) != 0) code = (ushort)((code << 1) ^ polinom);
                    else code <<= 1;
                }
            }
            return code;
        }

        /// <summary>
        /// Рассчет контрольной суммы
        /// CRC-16/ARC, CRC-16/IBM, CRC-16/DF1(Allen Bradley)
        /// циклический код с полиномом
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static ushort CRC16_IBM(byte[] msg)
        {
            const ushort polinom = 0xA001;
            ushort code = 0x0000;
            for (int i = 0, size = msg.Length; i < size; ++i)
            {
                code ^= msg[i];
                for (uint j = 0; j < 8; ++j)
                {
                    if ((code & 0x0001) > 0)
                    {
                        code >>= 1;
                        code ^= polinom;
                    }
                    else code >>= 1;
                }
            }
            return code;
        }

        /// <summary>
        /// Рассчет контрольной суммы CRC16 Modbus, циклический код с полиномом
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static ushort CRC16_Modbus(byte[] msg)
        {
            const ushort polinom = 0xa001;
            ushort code = 0xffff;

            for (int i = 0, size = msg.Length; i < size; ++i)
            {
                code ^= (ushort)(msg[i] << 8);

                for (uint j = 0; j < 8; ++j)
                {
                    code >>= 1;
                    if ((code & 0x01) != 0) code ^= polinom;
                }
            }
            return code;
        }


        /// <summary>
        /// Рассчет контрольной суммы 
        /// CRC-16/ARC, CRC-16/IBM, CRC-16/DF1(Allen Bradley)
        /// табличный вариант
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>

        public static ushort CRC16_DF1(byte[] buffer, int length)
        {
            int offset = 0;
            byte crcHi = 0x00;   // high byte of CRC initialized
            byte crcLo = 0x00;   // low byte of CRC initialized
            int index;           // will index into CRC lookup table

            while (length-- > 0) // pass through message buffer
            {
                index = crcLo ^ buffer[offset++]; // calculate the CRC
                crcLo = (byte)(crcHi ^ CRCHiTable[index]);
                crcHi = CRCLoTable[index];
            }
            return (ushort)((crcHi << 8) | crcLo);
        }

        /// <summary>
        /// Рассчет контрольной суммы CRC16 Modbus, табличный вариант
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns></returns>

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

        private readonly static byte[] CRCHiTable;
        private readonly static byte[] CRCLoTable;

        static CrcFunc()
        {
            CRCHiTable = new byte[] { 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64 };
            CRCLoTable = new byte[] { 0, 192, 193, 1, 195, 3, 2, 194, 198, 6, 7, 199, 5, 197, 196, 4, 204, 12, 13, 205, 15, 207, 206, 14, 10, 202, 203, 11, 201, 9, 8, 200, 216, 24, 25, 217, 27, 219, 218, 26, 30, 222, 223, 31, 221, 29, 28, 220, 20, 212, 213, 21, 215, 23, 22, 214, 210, 18, 19, 211, 17, 209, 208, 16, 240, 48, 49, 241, 51, 243, 242, 50, 54, 246, 247, 55, 245, 53, 52, 244, 60, 252, 253, 61, 255, 63, 62, 254, 250, 58, 59, 251, 57, 249, 248, 56, 40, 232, 233, 41, 235, 43, 42, 234, 238, 46, 47, 239, 45, 237, 236, 44, 228, 36, 37, 229, 39, 231, 230, 38, 34, 226, 227, 35, 225, 33, 32, 224, 160, 96, 97, 161, 99, 163, 162, 98, 102, 166, 167, 103, 165, 101, 100, 164, 108, 172, 173, 109, 175, 111, 110, 174, 170, 106, 107, 171, 105, 169, 168, 104, 120, 184, 185, 121, 187, 123, 122, 186, 190, 126, 127, 191, 125, 189, 188, 124, 180, 116, 117, 181, 119, 183, 182, 118, 114, 178, 179, 115, 177, 113, 112, 176, 80, 144, 145, 81, 147, 83, 82, 146, 150, 86, 87, 151, 85, 149, 148, 84, 156, 92, 93, 157, 95, 159, 158, 94, 90, 154, 155, 91, 153, 89, 88, 152, 136, 72, 73, 137, 75, 139, 138, 74, 78, 142, 143, 79, 141, 77, 76, 140, 68, 132, 133, 69, 135, 71, 70, 134, 130, 66, 67, 131, 65, 129, 128, 64 };
        }



        /// <summary>
        /// Расчет CRC протокола ОВЕН
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static ushort owenCRC16(char[] packet)
        {
            ushort crc = 0;
            for (int i = 0; i < packet.Length; ++i)
            {
                char b = packet[i];
                for (int j = 0; j < 8; ++j, b <<= 1)
                {
                    if (((b ^ (crc >> 8)) & 0x80) > 0 )
                    {
                        crc <<= 1;
                        crc ^= 0x8F57;
                    }
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }

    }
}
