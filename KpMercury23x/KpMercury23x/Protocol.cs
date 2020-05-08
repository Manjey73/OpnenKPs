using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScadaCommFunc;
using Scada.Comm.Devices.KpMercury23x;

namespace Scada.Comm.Devices
{

    internal static class Protocol
    {
        public static ushort res;  //резервирование ответа контрольной суммы

        public static byte[] TestCnlReq(int devAddr)
        {
            byte[] testCnl = new byte[4];
            testCnl[0] = (byte)devAddr;
            testCnl[1] = 0x00;                                  //команда запроса на тестирование канала
            res = CrcFunc.CalcCRC16(testCnl, 2);                //получить контрольную сумму
            testCnl[testCnl.Length - 2] = (byte)(res % 256);    //Добавить контрольную сумму к буферу посылки
            testCnl[testCnl.Length - 1] = (byte)(res / 256);
            return testCnl;
        }

        public static byte[] OpenCnlReq(int devAddr, string uroven,string pass)
        {
            byte[] temp_pass = new byte[4];
            byte[] buf_pass = Encoding.ASCII.GetBytes(pass);

            for (int f = 0; f < 6; f++)
            {
                Array.Copy(buf_pass, f, temp_pass, 0, 1);
                int temp_int = BitConverter.ToInt32(temp_pass, 0) - 48;
                temp_pass = BitConverter.GetBytes(temp_int);
                Array.Copy(temp_pass, 0, buf_pass, f, 1);
            }

            byte[] openCnl = new byte[11];
            openCnl[0] = (byte)devAddr;
            openCnl[1] = 0x01;                                      //команда запроса на открытие канала
            openCnl[2] = Convert.ToByte(Convert.ToInt16(uroven));   // Ввод уровня доступа пока без проверки, по умолчанию 1.... 0x01; //Уровень доступа 1

            Array.Copy(buf_pass, 0, openCnl, 3, 6);

            res = CrcFunc.CalcCRC16(openCnl, 9);                //получить контрольную сумму
            openCnl[openCnl.Length - 2] = (byte)(res % 256);    //Добавить контрольную сумму к буферу посылки
            openCnl[openCnl.Length - 1] = (byte)(res / 256);
            return openCnl;
        }

        public static byte[] FixDataReq(int devAddr)
        {
            byte[] fixData = new byte[5];
            fixData[0] = (byte)devAddr;
            fixData[1] = 0x03;                                  //команда записи
            fixData[2] = 0x08;                                  //параметр фиксации данных
            res = CrcFunc.CalcCRC16(fixData, 3);                //получить контрольную сумму
            fixData[fixData.Length - 2] = (byte)(res % 256);    //Добавить контрольную сумму к буферу посылки
            fixData[fixData.Length - 1] = (byte)(res / 256);
            return fixData;
        }

        public static byte[] KuiReq(int devAddr)
        {
            byte[] kui = new byte[5];
            kui[0] = (byte)devAddr;
            kui[1] = 0x08;                              // 2.3 Запрос на чтение параметров
            kui[2] = 0x02;                              // 2.3.3 Прочитать коэффициент трансформации счетчика
            res = CrcFunc.CalcCRC16(kui, 3);            //получить контрольную сумму
            kui[kui.Length - 2] = (byte)(res % 256);    //Добавить контрольную сумму к буферу посылки
            kui[kui.Length - 1] = (byte)(res / 256);
            return kui;
        }

        public static byte[] DataReq(int devAddr, string Param, int bwri)
        {
            byte[] data = new byte[6];
            int bwrim = bwri & 0xf0;
            data[0] = (byte)devAddr;
            data[1] = 0x08;                               //команда чтения зафиксированных данных
            if (Param == "14h")
            {
                data[2] = 0x14;
                data[3] = (byte)bwri;                    //параметр фиксации данных
            }
            else
            {
                if (bwrim == 0xf0)
                {
                    data[1] = 0x05; // Переход на функцию 0x05 для чтения энергии от сброса при использовании чтения счетчика кодом 0x08 и параметром 0x16
                    data[2] = 0x00;
                    data[3] = Convert.ToByte(bwri & 0x0f); // Запись # тарифа
                }
                else
                {
                    data[2] = 0x16;
                    data[3] = Convert.ToByte(bwri);         // Запись BWRI кода
                }
            }
            res = CrcFunc.CalcCRC16(data, 4);               //получить контрольную сумму
            data[data.Length - 2] = (byte)(res % 256);      //Добавить контрольную сумму к буферу посылки
            data[data.Length - 1] = (byte)(res / 256);
            return data;
        }

        public static byte[] EnergyPReq(int devAddr, int tarif)
        {
            byte[] energy = new byte[6];
            energy[0] = (byte)devAddr;
            energy[1] = 0x05;                                   // 2.2 Запросы на чтение массивов регистров накопленной энергии
            energy[2] = 0x60;                                   // Параметр чтения накопленной энергии A+ от сброса по фазам
            energy[3] = (byte)tarif;                            // Параметр чтения накопленной энергии A+ от сброса по фазам
            res = CrcFunc.CalcCRC16(energy, 4);                 //получить контрольную сумму
            energy[energy.Length - 2] = (byte)(res % 256);      //Добавить контрольную сумму к буферу посылки
            energy[energy.Length - 1] = (byte)(res / 256);
            return energy;
        }

        public static byte[] InfoReq(int devAddr)
        {
            byte[] info = new byte[5];
            info[0] = (byte)devAddr;
            info[1] = 0x08;                             // 2.3.2 Ускоренный режим чтения индивидуальных параметров
            info[2] = 0x01;                             // Серийный номер, дата выпуска, версия ПО, вариант исполнения
            res = CrcFunc.CalcCRC16(info, 3);           //получить контрольную сумму
            info[info.Length - 2] = (byte)(res % 256);  //Добавить контрольную сумму к буферу посылки
            info[info.Length - 1] = (byte)(res / 256);
            return info;
        }

    }
}
