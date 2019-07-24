using System;

namespace ScadaCommFunc
{
    public static class ConvFunc
    {
        /// <summary>
        /// Конвертирование десятичного числа в формат BCD (Используется в MBus, приборах Пульсар)
        /// </summary>
        /// <param name="dec"></param>
        /// <returns></returns>
        public static int DecToBCD(int dec)
        {
            int num = 0;
            int num1 = 0;
            while (dec != 0)
            {
                num = num | dec % 10 << (num1 & 31);
                num1 += 4;
                dec /= 10;
            }
            return num;
        }

        /// <summary>
        /// Конвертирование числа в формате BCD в десятичный формат (Используется в MBus, приборах Пульсар)
        /// </summary>
        /// <param name="bcdNumber"></param>
        /// <returns></returns>

        public static long BcdToDec(byte[] bcdNumber)
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

        /// <summary>
        /// Проверка строки на соответствие HEX символам 0-9, A-F
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        
        public static bool IsHex(this string s)
        {
            foreach (char c in s)
                if (!Uri.IsHexDigit(c))
                    return false;
            return true;
        }

        /// <summary>
        /// Конвертирование строки HEX в массив байт, допустимые символы 0123456789ABCDEFabcdef, запись слитно или с пробелами.
        /// Возвращает массив c нулевой длиной при ошибке выполнения (или строка содержит не HEX символы или строка не кратна двум)
        /// </summary>
        /// <param name="inChar"></param>
        /// <returns></returns>

        public static byte[] StringToHex(string inChar)
        {
            string s = inChar.Replace(" ","");
            try
            {
                int NumberChars = s.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
                return bytes;
            }
            catch
            {
               return new byte[] { }; // возвращает массив нулевой длины для контроля
            }
        }


        /// <summary>
        /// Преобразование массива байт в HEX текст, удаляя "-" и меняя " "
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>

        public static string HexToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", " ");
        }

        /// <summary>
        /// Перевернуть массив младшим (out_LE = true) или старшим (out_LE = false) в зависимости от архитектуры
        /// </summary>
        /// <param name="buf_rev"></param>
        /// <param name="out_LE"></param>
        /// 
        public static void Reverse_array(byte[] buf_rev, bool out_LE)
        {
            if (!out_LE & BitConverter.IsLittleEndian)
            {
                Array.Reverse(buf_rev);
            }
            if (out_LE & !BitConverter.IsLittleEndian)
            {
                Array.Reverse(buf_rev);
            }
        }

    }
}
