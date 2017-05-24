using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scada.Comm.Devices
{
    class Parametrs
    {
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
            string[] def = { "0", "0", "0", "0", "0" }; //массив значений по умолчанию: 1-й - Количество используемых GPIO, 2-й - Назначение GPIO (0 = вход, 1 = выход)
            // 3 -й - подтяжка (0 = pulldown, 1 = pullup), 4-й - инициализация уровня выхода (0=Low, 1=High), 5-й - Инициализировать уровень перед активацией выхода
            string sr = ";"; //символ разделителя записей
            string[] s_out = new string[5]; // массив введенных значений или значений по умолчанию
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

        // Новый целочисленный массив с данными
        public static int[] nmass_int(int[] mass_in, int mask)
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

    }
}
