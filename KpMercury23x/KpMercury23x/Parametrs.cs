using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scada.Comm.Devices
{
    class Parametrs
    {

        public static UInt32 ROL(UInt32 number, int shift)
        {
            shift %= 31;
            return ((number << shift) | (number >> (32 - shift)));
        }

        public static UInt32 ROR(UInt32 number, int shift)
        {
            shift %= 31;
            return ((number >> shift) | (number << (32 - shift)));
        }

        public static int GetBit(int val, int n)
        {
            int intVal = val;
            return (intVal >> n) & 1;
        }

        public static int SetBit(int n, int index, bool value)
        {
            return value ? n | (1 << index) : n & ~(1 << index);
        }

        public static string[] Parametr(string s_in)
        {
            string[] def = { "111111", "262143", "1", "1" }; //массив значений по умолчанию
            string sr = ";"; //символ разделителя записей
            // s_in - входная строка для обработки, объявление переменной для пароля и уровня доступа
            string[] s_out = new string[4]; // массив введенных значений или значений по умолчанию
            int i_curr, i_prev, j_curr; //текущее и предыдущее значение индекса литеры в строке, текущее значение индекса слова в строке

            i_curr = 0;
            j_curr = 0;
            i_prev = 0;
            i_curr = s_in.IndexOf(sr, i_prev);

            while (i_curr > -1) //выполняем цикл пока находятся символы разделителя
            {
                if (i_curr == i_prev) //если слово пустое, то меняем на дефолт
                {
                    try //выполняем, если нет ошибки индекса массива
                    {
                        s_out[j_curr] = def[j_curr];
                    }
                    catch //обрабатываем ошибку (размерность массива меньше кол-ва входящих слов)
                    {
                        break;
                    }
                }
                else //иначе подставляем текущее слово
                {
                    s_out[j_curr] = s_in.Substring(i_prev, i_curr - i_prev);
                }
                j_curr++;
                i_prev = i_curr + 1;
                i_curr = s_in.IndexOf(sr, i_prev); //запрашиваем позицию следующего разделителя
            }

            for (int i = j_curr; i < def.Length; i++) // добираем все неиспользуемые переменные по умолчанию
            {
                s_out[j_curr] = def[j_curr];
                j_curr++;
                i_prev = i_curr + 1;
                i_curr = s_in.IndexOf(sr, i_prev); //запрашиваем позицию следующего разделителя
            }

            return s_out;
        }

    }
}
