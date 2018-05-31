using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaCommFunc
{
    public class ConvFunc
    {
        public static int DecToBCD(int dec) // Конвертирование десятичного числа в формат BCD (Используется в MBus, приборах Пульсар)
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

        public static long BcdToDec(byte[] bcdNumber) // Конвертирование числа в формате BCD в десятичный формат (Используется в MBus, приборах Пульсар)
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
    }
}
