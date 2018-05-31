using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaCommFunc
{
    public class BitFunc
    {
        public static UInt32 ROL(UInt32 number, int shift) // цикличиский сдвиг битов влево до 32-х бит, на входе переменная UInt32
        {
            shift %= 31;
            return ((number << shift) | (number >> (32 - shift)));
        }

        public static UInt32 ROR(UInt32 number, int shift) // цикличиский сдвиг битов вправо до 32-х бит, на входе переменная UInt32
        {
            shift %= 31;
            return ((number >> shift) | (number << (32 - shift)));
        }

        public static int GetBit(int val, int n) // Получить бит
        {
            int intVal = val;
            return (intVal >> n) & 1;
        }

        public static int SetBit(int n, int index, bool value) // вставить бит
        {
            return value ? n | (1 << index) : n & ~(1 << index);
        }
    }
}
