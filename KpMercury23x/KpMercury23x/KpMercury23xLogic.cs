/*
 * Copyright 2020 Andrey Burakhin
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * Product  : Rapid SCADA
 * Module   : KpMercury23x
 * Summary  : Device communication logic
 * 
 * Author   : Andrey Burakhin
 * Created  : 2017
 * Modified : 2020
 */


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
using ScadaCommFunc;


namespace Scada.Comm.Devices
{

    public sealed class KpMercury23xLogic : KPLogic
    {
        private DevTemplate devTemplate = new DevTemplate();

        private string fileName = "";
        private string filePath = "";
        private bool fileyes = false;   // При отсутствии файла не выполнять опрос
        private int idgr = 0;           // переменная для индекса группы
        private string Allname;         // Полное имя тега

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

        int[] bwri = new int[13]; // BWRI для запроса параметром 14h
        int[] bwrc = new int[13]; // Разрешающая способность регистров хранения 
        int[] b_length = new int[13];   // количество байт в ответе счетчика
        int[] parb = new int[13];    // количество байт в параметре ответа (4 или 3)
        int[] parc = new int[13];    // количество параметров в ответе (4, 3 или 1)

        // Массив значений параметров BWRI счетчика + 'энергии от сброса параметр 14h
        int[] bwri_14 = new int[] { 0x00, 0x04, 0x08, 0x30, 0x10, 0x20, 0x50, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 14h
        int[] bwrc_14 = new int[] { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        int[] b_length_14 = new int[] { 19, 19, 19, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        int[] parb_14 = new int[] { 4, 4, 4, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        int[] parc_14 = new int[] { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)

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

        // Массив значений параметров BWRI счетчика + 'энергии от сброса для чтения параметром 16h
        int[] bwri_16 = { 0x00, 0x04, 0x08, 0x30, 0x11, 0x21, 0x51, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 16h
        int[] bwrc_16 = { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        int[] b_length_16 = { 15, 15, 15, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        int[] parb_16 = { 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        int[] parc_16 = { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)

        // Команды BWRI для запроса 16h:
        // 0x00 - Мощность P по сумме фаз, фазе 1, фазе 2, фазе 3   (Вт)
        // 0x04 - Мощность Q по сумме фаз, фазе 1, фазе 2, фазе 3   (вар)
        // 0x08 - Мощность S по сумме фаз, фазе 1, фазе 2, фазе 3   (ВА)
        // 0x11 - Напряжение по фазе 1, фазе 2, фазе 3              (В)
        // 0x30 - Косинус ф по сумме фаз, фазе 1, фазе 2, фазе3
        // 0x21 - Ток по фазе 1, фазе 2, фазе 3                     (А)
        // 0x40 - Частота сети
        // 0x51 - Угол м-ду ф. 1 и 2, 1 и 3, 2 и 3                  (градусы)

        // Массив значений энергии по фазам прямого направления и чтения энергии от сброса функцией 0x05 при чтении счетчика параметром 16h кода функции 0x08
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
        int mask_g1 = 0; // Входная переменная для выбора тегов
        bool slog = true;
        int[] parkui = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        string readparam, password, uroven; //входная строка для обработки, объявление переменной для пароля и уровня доступа

        public override void OnAddedToCommLine()
        {
            base.OnAddedToCommLine();

            devTemplate = null;

            fileName = ReqParams.CmdLine == null ? "" : ReqParams.CmdLine.Trim();
            filePath = AppDirs.ConfigDir + fileName;

            if (fileName == "") // Чтение файла шаблона
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                    "{0} Ошибка: Не задан шаблон устройства для {1}" :
                    "{0} Error: Template is undefined for the {1}", CommUtils.GetNowDT(), Caption));
            } // Чтение файла шаблона
            else
            {
                try
                {
                    devTemplate = FileFunc.LoadXml(typeof(DevTemplate), filePath) as DevTemplate;
                    fileyes = true;
                }
                catch (Exception err)
                {
                    WriteToLog(string.Format(Localization.UseRussian ?
                    "Ошибка: " + err.Message :
                    "Error: " + err.Message, CommUtils.GetNowDT(), Caption));
                }
            }

            int[] parkui_16 = new int[] { ki, ki, ki, 1, ku, ki, 1, 1, ki, ki, ki, ki, ki };

            int time_out = ReqParams.Timeout;

            if (devTemplate != null)
            {
                password = string.IsNullOrEmpty(devTemplate.password) ? "111111" : devTemplate.password;
                uroven = string.IsNullOrEmpty(devTemplate.mode.ToString()) ? "1" : devTemplate.mode.ToString(); // Преобразование int '1' или '2' к строке для совместимости с кодом драйвера, по умолчанию '1' если строка пуста
                readparam = string.IsNullOrEmpty(devTemplate.readparam) ? "14h" : devTemplate.readparam;
                slog = devTemplate.log;

                if (devTemplate.SndGroups.Count != 0) // Определить активные запросы объектов и записать в список индексы запросов для создания тегов
                {
                    for (int sg = 0; sg < devTemplate.SndGroups.Count; sg++)
                    {
                        if (devTemplate.SndGroups[sg].Active)
                        {
                            mask_g1 = BitFunc.SetBit(mask_g1, devTemplate.SndGroups[sg].Bit, true);
                        }
                    }
                }
            }

            if (readparam == "14h")
            {
                Array.Copy(parkui_16, parkui, 13);
                Array.Copy(bwri_14, bwri, 13);
                Array.Copy(bwrc_14, bwrc, 13);
                Array.Copy(b_length_14, b_length, 13);
                Array.Copy(parb_14, parb, 13);
                Array.Copy(parc_14, parc, 13);
            }
            if (readparam == "16h")
            {
                Array.Copy(bwri_16, bwri, 13);
                Array.Copy(bwrc_16, bwrc, 13);
                Array.Copy(b_length_16, b_length, 13);
                Array.Copy(parb_16, parb, 13);
                Array.Copy(parc_16, parc, 13);
            }

            int mgn_znac = mask_g1 & 0xFF; // отсечь мгновенные значения для организации отображения тегов
            int energy = mask_g1 & 0x3FF00; // Отсечь параметры энергии для организации отображения тегов

            List<TagGroup> tagGroups = new List<TagGroup>();
            TagGroup tagGroup;

            if (mgn_znac != 0)
            {
                tagGroup = new TagGroup("Мгновенные значения:");

                bool mgn_P = BitFunc.GetBit(mask_g1, 0) > 0; // Параметр Bit группы "Мощность P" должен быть равен 0 
                if (mgn_P)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 0); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_Q = BitFunc.GetBit(mask_g1, 1) > 0; // Параметр Bit группы "Мощность Q" должен быть равен 1
                if (mgn_Q)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 1); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_S = BitFunc.GetBit(mask_g1, 2) > 0; // Параметр Bit группы "Мощность S" должен быть равен 2
                if (mgn_S)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 2); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_cos = BitFunc.GetBit(mask_g1, 3) > 0; // Параметр Bit группы "COSф" должен быть равен 3
                if (mgn_cos)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 3); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_U = BitFunc.GetBit(mask_g1, 4) > 0; // Параметр Bit группы "Напряжение" должен быть равен 4
                if (mgn_U)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 4); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_I = BitFunc.GetBit(mask_g1, 5) > 0; // Параметр Bit группы "Ток" должен быть равен 5
                if (mgn_I)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 5); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_FU = BitFunc.GetBit(mask_g1, 6) > 0; // Параметр Bit группы "Угол м-ду ф." должен быть равен 6
                if (mgn_FU)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 6); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool mgn_F = BitFunc.GetBit(mask_g1, 7) > 0; // Параметр Bit группы "Частота" должен быть равен 7
                if (mgn_F)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 7); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                tagGroups.Add(tagGroup);
            }

            if (energy != 0)
            {
                tagGroup = new TagGroup("Энергия от сброса:");

                bool en_summ = BitFunc.GetBit(mask_g1, 8) > 0; // Параметр Bit группы "Сумма" должен быть равен 8
                if (en_summ)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 8); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar1 = BitFunc.GetBit(mask_g1, 9) > 0; // Параметр Bit группы "Тариф 1" должен быть равен 9
                if (en_tar1)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 9); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar2 = BitFunc.GetBit(mask_g1, 10) > 0; // Параметр Bit группы "Тариф 2" должен быть равен 10
                if (en_tar2)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 10); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar3 = BitFunc.GetBit(mask_g1, 11) > 0; // Параметр Bit группы "Тариф 3" должен быть равен 11
                if (en_tar3)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 11); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool en_tar4 = BitFunc.GetBit(mask_g1, 12) > 0; // Параметр Bit группы "Тариф 4" должен быть равен 12
                if (en_tar4)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 12); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_summ = BitFunc.GetBit(mask_g1, 13) > 0; // Параметр Bit группы "Сумма А+" должен быть равен 13
                if (enL_summ)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 13); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar1 = BitFunc.GetBit(mask_g1, 14) > 0; // Параметр Bit группы "Тариф 1 А+" должен быть равен 14
                if (enL_tar1)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 14); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar2 = BitFunc.GetBit(mask_g1, 15) > 0; // Параметр Bit группы "Тариф 2 А+" должен быть равен 15
                if (enL_tar2)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 15); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar3 = BitFunc.GetBit(mask_g1, 16) > 0; // Параметр Bit группы "Тариф 3 А+" должен быть равен 16
                if (enL_tar3)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 16); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                bool enL_tar4 = BitFunc.GetBit(mask_g1, 17) > 0; // Параметр Bit группы "Тариф 4 А+" должен быть равен 17
                if (enL_tar4)
                {
                    idgr = devTemplate.SndGroups.FindIndex(f => f.Bit == 17); // Ищем индекс с соответствующим битом группы
                    tagcreate(tagGroup, idgr);
                }

                tagGroups.Add(tagGroup);
            }

            tagGroup = new TagGroup("Статус:");
            tagGroup.KPTags.Add(new KPTag(70, "Код ошибки:"));
            tagGroup.KPTags.Add(new KPTag(71, "коэфф. трансформации тока:"));
            tagGroup.KPTags.Add(new KPTag(72, "коэфф. трансформации напряжения:"));
            tagGroups.Add(tagGroup);

            InitKPTags(tagGroups);
        }


        //---------------------------------------------------------------
        public KpMercury23xLogic(int number) : base(number)
        {
        }
        //---------------------------------------------------------------

        //-------------------------
        public override void Session()
        {

            base.Session();

            if (!fileyes)        // Если конфигурация не была загружена, выставляем все теги в невалидное состояние и выходим         
            {
                InvalidateCurData();
                return;
            }

            uint par14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // отсечь количество параметров для команды 08h и параметра 14h

            uint energyL = Convert.ToUInt32(mask_g1 & 0x3E000); // Проверка наличия опроса значений энергии прямого направления
            energyL = BitFunc.ROR(energyL, 13);

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
                Array.Resize(ref buf_out, i + 3);           //изменить размер массива
                buf_out[i] = 0x00;                          //команда запроса на тестирование канала
                res = CrcFunc.CalcCRC16(buf_out, i + 1);    //получить контрольную сумму
                buf_out[i + 1] = (byte)(res % 256);         //Добавить контрольную сумму к буферу посылки
                buf_out[i + 2] = (byte)(res / 256);
                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) WriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);

                buf_in = new byte[4];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) WriteToLog(logText);
                if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0)
                {
                    testcnl = true;
                    code = buf_in[1];
                    lastCommSucc = true;
                    if (slog) WriteToLog("OK!");
                }
                else
                {
                    code = 0x11;
                    WriteToLog(ToLogString(code));
                    if (slog) WriteToLog("Ошибка!");
                }
                FinishRequest();
            }

            if (testcnl) // Открытие канала с уровнем доступа согласно введенного пароля.
            {
                long t2 = Ticks();
                if ((t2 > (t1 + 240000)) || !opencnl)
                {
                    t1 = t2;
                    // Запрос на открытие канала
                    lastCommSucc = false;
                    string logText;
                    byte[] temp_pass = new byte[4];

                    string kpNum_pass = string.Concat(Number.ToString(), "_pass");
                    string pass = String.IsNullOrEmpty(CustomParams.GetStringParam(kpNum_pass, false, "")) ? password : CustomParams.GetStringParam(kpNum_pass, false, "");

                    byte[] buf_pass = Encoding.ASCII.GetBytes(pass);

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
                    res = CrcFunc.CalcCRC16(buf_out, buf_out.Length - 2); //получить контрольную сумму
                    buf_out[buf_out.Length - 2] = (byte)(res % 256); //Добавить контрольную сумму к буферу посылки
                    buf_out[buf_out.Length - 1] = (byte)(res / 256);
                    Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) WriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);
                    buf_in = new byte[4];
                    Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) WriteToLog(logText);

                    if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0)
                    {
                        // тут проверка на корректность пароля, смена статуса открытого канала
                        if (buf_in[1] == 0)
                        {
                            code = buf_in[1];
                            opencnl = true;
                            lastCommSucc = true;
                            if (slog) WriteToLog("OK!");
                        }
                        else
                        {
                            code = buf_in[1];
                            WriteToLog(ToLogString(code));
                            lastCommSucc = true;
                            if (slog) WriteToLog("OK!");
                        }
                    }
                    else
                    {
                        code = 0x10;
                        WriteToLog(ToLogString(code));
                        if (slog) WriteToLog("Ошибка!");
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
                res = CrcFunc.CalcCRC16(buf_out, buf_out.Length - 2); //получить контрольную сумму
                buf_out[buf_out.Length - 2] = (byte)(res % 256); //Добавить контрольную сумму к буферу посылки
                buf_out[buf_out.Length - 1] = (byte)(res / 256);
                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) WriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);

                buf_in = new byte[7];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) WriteToLog(logText);
                // тут проверка CRC и корректности ответа

                if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0)
                {
                    Kui = true;
                    //ku = Convert.ToInt32(Parametrs.ROR(BitConverter.ToUInt16(buf_in, 1), 8));
                    ku = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(buf_in, 1), 8));
                    ki = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(buf_in, 3), 8));
                    lastCommSucc = true;
                    if (slog) WriteToLog("OK!");
                }
                else
                {
                    WriteToLog("Ошибка: Недопустимая команда");
                    if (slog) WriteToLog("Ошибка!");
                }
                FinishRequest();
            }

            // ------------Получить мгновенные значения P,Q,S,U,I вариант 2
            uint com14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // проверка маски на необходимость чтения команды 08h с параметром 14h


            if (com14h != 0)
            {
                lastCommSucc = false;
                string logText;
                int znx = 1; // начальное положение первого массива байт в ответе
                float znac = 0;
                Array.Resize(ref buf_out, i + 4); //изменить размер массива
                buf_out[i] = 0x03; i++; // 1.3 Запрос на запись параметров
                buf_out[i] = 0x08; i++; // 08h фиксация данных.
                res = CrcFunc.CalcCRC16(buf_out, 3); //получить контрольную сумму
                buf_out[i] = (byte)(res % 256); i++;
                buf_out[i] = (byte)(res / 256);

                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                if (slog) WriteToLog(logText);
                System.Threading.Thread.Sleep(ReqParams.Delay);
                buf_in = new byte[4];
                Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                if (slog) WriteToLog(logText);
                // тут проверка CRC ответа и успешности команды фиксации данных

                if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0 && opencnl)
                {
                    code = buf_in[1];
                    lastCommSucc = true;
                    if (slog) WriteToLog("OK!");
                }
                else
                {
                    code = 0x10;
                    WriteToLog(ToLogString(code));
                    if (slog) WriteToLog("Ошибка!");
                }
                FinishRequest();


                i = 1;
                Array.Resize(ref buf_out, i + 5); //изменить размер массива

                buf_out[i] = 0x08; i++; // 2.3 Запрос на чтение параметров


                if (readparam == "14h")
                {
                    buf_out[i] = 0x14; i++; // 14h Чтение зафиксированных вспомогательных параметров: мгновенной активной, реактивной, полной мощности, напряжения, тока, коэффициента мощности и частоты.
                }
                if (readparam == "16h")
                {
                    buf_out[i] = 0x16; i++; // 16h Чтение вспомогательных параметров: мгновенной активной, реактивной, полной мощности, напряжения, тока, коэффициента мощности и частоты.
                }


                for (int f = 0; f < nbwri.Length; f++)
                {
                    lastCommSucc = false;
                    int bwrim = nbwri[f] & 0xf0;

                    if (readparam == "16h")
                    {
                        if (bwrim == 0xf0)
                        {
                            buf_out[i - 2] = 0x05; // Переход на функцию 0x05 для чтения энергии от сброса при использовании чтения счетчика кодом 0x08 и параметром 0x16
                            buf_out[i - 1] = 0x00;
                            buf_out[i] = Convert.ToByte(nbwri[f] & 0x0f); i++; // Запись # тарифа
                        }
                        else
                        {
                            buf_out[i] = Convert.ToByte(nbwri[f]); i++; // Запись BWRI кода
                        }
                    }
                    else
                    {
                        buf_out[i] = Convert.ToByte(nbwri[f]); i++; // Запись BWRI кода
                    }

                    byte[] zn_temp = new byte[4];

                    res = CrcFunc.CalcCRC16(buf_out, 4); //получить контрольную сумму
                    buf_out[i] = (byte)(res % 256); i++;
                    buf_out[i] = (byte)(res / 256);

                    Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) WriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);

                    buf_in = new byte[nb_length[f]];

                    Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) WriteToLog(logText);
                    // тут проверка CRC ответа

                    if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0 && opencnl)
                    {
                        lastCommSucc = true;
                        if (slog) WriteToLog("OK!");
                        for (int zn = 0; zn < nparc[f]; zn++)
                        {
                            Array.Copy(buf_in, znx, zn_temp, 0, nparb[f]);
                            uint znac_temp = BitConverter.ToUInt32(zn_temp, 0);
                            if (nparb[f] == 4)
                            {
                                znac_temp = BitFunc.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x04 && (znac_temp & 0x40000000) >= 1) ennapr = -1; // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffffff;              // наложение маски для удаления направления для получения значения
                            }
                            else
                            {
                                znac_temp = BitFunc.ROR(znac_temp, 8);
                                if (nbwri[f] == 0x04 && (znac_temp & 0x400000) >= 1) ennapr = -1;   // определение направления Реактивной мощности
                                if (bwrim != 0x04) znac_temp = znac_temp & 0x3fffff;                // наложение маски для удаления направления для получения значения

                                if (nbwri[f] == 0x30) znac_temp = znac_temp & 0x3ff;                // наложение маски на 3-х байтовую переменную косинуса
                            }
                            if (znac_temp == 0xffffffff && nparb[f] == 4)
                            {
                                znac = Convert.ToSingle(double.NaN);
                            }
                            else
                            {
                                znac = Convert.ToSingle(znac_temp) / nbwrc[f] * parkui[f]; //получение значения с учетом разрещшающей способности
                            }

                            if (znac != Convert.ToSingle(double.NaN))
                            {
                                SetCurData(tag - 1, znac * ennapr, 1);
                            }
                            else
                            {
                                InvalidateCurData(tag - 1, 1);
                            }

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
                        if (slog) WriteToLog("Ошибка!");
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

            //------------Получить пофазные значения накопленной энергии прямого направления  код запросв 0x05, параметр 0x60

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
                    res = CrcFunc.CalcCRC16(buf_out, 4); //получить контрольную сумму
                    buf_out[i] = (byte)(res % 256); i++;
                    buf_out[i] = (byte)(res / 256);
                    Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText); //послать запрос в порт
                    if (slog) WriteToLog(logText);
                    System.Threading.Thread.Sleep(ReqParams.Delay);
                    buf_in = new byte[15];
                    Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText); //считать значение из порта
                    if (slog) WriteToLog(logText);
                    // Тут проверка ответа на корректность

                    if (CrcFunc.CalcCRC16(buf_in, buf_in.Length) == 0 && opencnl)
                    {
                        code = buf_in[2];
                        lastCommSucc = true;
                        if (slog) WriteToLog("OK!");

                        int znx = 1;
                        for (int zn = 0; zn < 3; zn++)
                        {
                            uint znac_temp = BitConverter.ToUInt32(buf_in, znx);
                            znac_temp = BitFunc.ROR(znac_temp, 16);
                            float znac = Convert.ToSingle(znac_temp) / 1000 * ki;
                            SetCurData(tag - 1, znac, 1);
                            znx = znx + 4;
                            tag++;
                        }
                    }
                    else
                    {
                        code = 0x10;
                        WriteToLog(ToLogString(code));
                        if (slog) WriteToLog("Ошибка!");

                        InvalidateCurData(tag - 1, 3);
                        tag = tag + 3;
                    }
                    i = 3;
                }

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
            tag++;
            SetCurData(tag - 1, ki, 1);
            tag++;
            SetCurData(tag - 1, ku, 1);

            tag = 1;

            CalcSessStats(); // расчёт статистики
        }

        //-------------------------
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);


            CalcCmdStats(); // расчёт статистики
        }

        
        private void tagcreate(TagGroup taggroup, int idgr)
        {
            for (int i = 0; i < devTemplate.SndGroups[idgr].value.Count; i++)
            {
                Allname = string.Concat(devTemplate.SndGroups[idgr].Name, " ", devTemplate.SndGroups[idgr].value[i].name);
                taggroup.KPTags.Add(new KPTag(devTemplate.SndGroups[idgr].value[i].signal, Allname));
            }
        }

    }
}

