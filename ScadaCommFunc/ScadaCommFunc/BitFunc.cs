using System;

namespace ScadaCommFunc
{
    public static class BitFunc
    {
        /// <summary>
        /// цикличиский сдвиг битов влево до 32-х бит, на входе переменная UInt32
        /// </summary>
        /// <param name="number"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        /// 
        public static UInt32 ROL(UInt32 number, int shift)
        {
            shift %= 31;
            return ((number << shift) | (number >> (32 - shift)));
        }

        /// <summary>
        /// цикличиский сдвиг битов вправо до 32-х бит, на входе переменная UInt32
        /// </summary>
        /// <param name="number"></param>
        /// <param name="shift"></param>
        /// <returns></returns>

        public static UInt32 ROR(UInt32 number, int shift)
        {
            shift %= 31;
            return ((number >> shift) | (number << (32 - shift)));
        }

        /// <summary>
        /// Получить бит из числа int
        /// </summary>
        /// <param name="val"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        /// 
        public static int GetBit(int val, int n)
        {
            int intVal = val;
            return (intVal >> n) & 1;
        }

        /// <summary>
        /// вставить бит в чисто int
        /// </summary>
        /// <param name="n"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// 
        public static int SetBit(int n, int index, bool value) 
        {
            return value ? n | (1 << index) : n & ~(1 << index);
        }
    }
}
