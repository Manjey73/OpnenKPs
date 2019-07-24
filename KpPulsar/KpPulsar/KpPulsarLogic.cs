/*
 * Copyright 2019 Andrey Burakhin
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
 * Module   : KpPulsar
 * Summary  : Device communication logic
 * 
 * Author   : Andrey Burakhin
 * Created  : 2019
 * Modified : 2019
 */

using ScadaCommFunc;
using Scada.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using Scada.Data.Models;
using Scada.Data.Configuration;
using Scada.Comm.Channels;

namespace Scada.Comm.Devices
{
    public class KpPulsarLogic : KPLogic
    {
        /// <summary>
        /// Вызвать метод записи в журнал
        /// </summary>
        public void ExecWriteToLog(string text)
        {
            WriteToLog?.Invoke(text);
        }

        private static string Error_code(byte error)
        {
            string err = "Неизвестный тип ошибки";
            switch (error)
            {
                case 0x01: err = "Отсутствует запрашиваемый код функции"; break;
                case 0x02: err = "Ошибка в битовой маске запроса"; break;
                case 0x03: err = "Ошибочная длина запроса"; break;
                case 0x04: err = "Отсутствует параметр"; break;
                case 0x05: err = "Запись заблокирована, требуется авторизация"; break;
                case 0x06: err = "Записываемое значение (параметр) находится вне заданного диапазона"; break;
                case 0x07: err = "Отсутствует запрашиваемый тип архива"; break;
                case 0x08: err = "Превышение максимального количества архивных значений за один пакет"; break;
            }
            return err;
        }

        private DevTemplate devTemplate = new DevTemplate();
        private DevTemplate.SndGroup sndGroup = new DevTemplate.SndGroup();
        private DevTemplate.CmdGroup cmdGroup = new DevTemplate.CmdGroup();
        private DevTemplate.Value Value = new DevTemplate.Value();
        private DevTemplate.Value.Val val = new DevTemplate.Value.Val();

        private Random rnd = new Random();      // Случайное число для формирования ID запросов
        string logText;                         // Переменная для вывода лога в Журнал Коммуникатора
        private int readcnt = 0;                // Переменная счетчика принятых байт
        private string fileName = "";

        private byte[] buf_out = new byte[1];   // Инициализация буфера для отправки в прибор, начальное значение
        private byte[] buf_in = new byte[1];    // Инициализация выходного буфера

        private byte[] idBCD = new byte[4];     // Буфер для Адреса прибора
        private byte[] byteID = new byte[2];    // Буфер байт для ID запроса
        private int crc = 0;                    // Переменная для контрольной суммы
        private bool activeuse = false;         // Переменная наличия активных запросов SndActive должен быть равен true для активации запроса
        private bool fileyes = false;           // При отсутствии файла не выполнять опрос

        private int startCnl = 1;               // Стартовый номер сигнала для Текущих параметров F=0x01, при формировании шаблона можно изменить в параметре SndData соответствующего запроса
        private int startCnlv = 41;             // Стартовый номер сигнала для Веса импульсов F=0x07, при формировании шаблона можно изменить в параметре SndData соответствующего запроса
        private int xValCnt01 = 0;              // Сюда записать номер запроса Текущих параметров для возможности создания маски опрашиваемых параметров
        private int xValCnt07 = 0;              // Сюда записать номер запроса Веса импульсов для возможности создания маски опрашиваемых параметров

        private byte[] byteIDres = new byte[2]; // Буфер для проверки ID запроса при ответе

        private int col;                        // количество байт в переменных ответов в Текущих параметрах в зависимости от типа переменных
        private int mask_ch = 0;                // переменная для параметра MASK_CH
        private int mask_chv = 0;               // переменная для параметра MASK_CH Вес импульса (Регистратор импульсов)
        private int mask_ch_wr = 0;             // переменная для параметра MASK_CH записи данных каналов (Регистратор импульсов)
        private int mask_chv_wr = 0;            // Переменная для параметра MASK_CH записи Веса импульсов (Регистратор импульсов)
        private int res_ch = 0;                 // Количество бит в 1 в маске Текущих параметров для расчета длины ответа
        private int res_chv = 0;                // Количество бит в 1 в маске Веса импульса для расчета длины ответа
        private int maxch;                      // максимальный номер канала Текущих параметров
        private int maxchv;                     // максимальный номер канала Веса импульсов
        private byte[] maskch = new byte[4];    // Инициализация массива для маски считываемых параметров
        private byte sndcode_;

        private int sigN = 1;

        //---------------------------------------------------------------
        public KpPulsarLogic(int number) : base(number)
        {
            CanSendCmd = true;
        }
        //---------------------------------------------------------------

        private class ActiveCnlList
        {
            public int Cnl { get; set; }                // Cnl      - Номер активного сигнала
            public string Name { get; set; }            // Name     - Имя активного сигнала
            public string Format { get; set; }          // Format   - Тип переменной = float, double, uint16, uint32, DateTime
            public int IdxValue { get; set; }           // IdxValue - Индекс группы, в которую входит сигнал
            public int IdxTag { get; set; }             // IdxTag   - Индекс тега в представлении KPTags для SetCurData
            public string MenuName { get; set; }        // MenuName - Имя меню, к которому принадлежит параметр
        }

        private Dictionary<int, string> myTagId = new Dictionary<int, string>();    // Идентификаторы переменных для конвертирования в строку отображения в Коммуникаторе
        private Dictionary<int, int> ActiveSnd = new Dictionary<int, int>();        // Ключ = Номер запроса SndCnt - Значение = Индекс Активного запроса SndCnt
        private Dictionary<int, int> ActiveCmd = new Dictionary<int, int>();        // Ключ = Номер Активной команды CmdCnt - Значение = Индекс Активной команды CmdCnt

        private List<ActiveCnlList> ActiveCnl = new List<ActiveCnlList>();          // Создание списка Активных сигналов, где ActiveCnl.Cnl - номер сигнала, ActiveCnl.Name - Имя сигнала, 
                                                                                    // ActiveCnl.Fotmat - Тип активной переменной, ActiveCnl.IdxTag индекс сигнала в KPTags, ActiveCnl.IdxValue - Индекс группы,
                                                                                    //  в которую входит сигнал, ActiveCnl.MenuName - Имя меню, которому принадлежит сигнал

        /// <summary>
        /// Преобразовать данные тега КП в строку
        /// </summary>
        protected override string ConvertTagDataToStr(int signal, SrezTableLight.CnlData tagData) // Необходимо продумать как передать сюда список типов переменных - текст, время, цифровое и т.д.
        {
            string strval = "";
            bool readstr = myTagId.TryGetValue(signal, out strval); // Чтение типа переменной, привязанной к сигналу

            if (tagData.Stat > 0)
            {
                if (strval == "DateTime")                           // Проверка сигнала на тип данных Время для отображения в текстовом виде в таблице Коммуникатора
                {
                    return ScadaUtils.DecodeDateTime(tagData.Val).ToString();
                }
            }
            return base.ConvertTagDataToStr(signal, tagData);
        }

        public override void OnAddedToCommLine()                    // Выполняем действия при добавлении Линии связи - Чтение шаблона, создание списка Тегов
        {
            base.OnAddedToCommLine();
            devTemplate = null;

            fileName = ReqParams.CmdLine == null ? "" : ReqParams.CmdLine.Trim();
            string filePath = AppDirs.ConfigDir + fileName;

            if (fileName == "")
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                    "{0} Ошибка: Не задан шаблон устройства для {1}" :
                    "{0} Error: Template is undefined for the {1}", CommUtils.GetNowDT(), Caption));
            }
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

            // Проверка на наличие конфигурации XML
            if (devTemplate != null)
            {
                // Определить Номера активных запросов, посылку команд проводить согласно списку активных запросов.

                if (devTemplate.SndGroups.Count != 0) // Определить активные запросы и записать в массив номера запросов для создания тегов по номерам телеграмм
                                                      // Можно упростить до определения индекса
                {
                    for (int snd = 0; snd < devTemplate.SndGroups.Count; snd++)
                    {
                        if (devTemplate.SndGroups[snd].SndActive)                                                       // Если запрос активен, заносим его номер Cnt в Словарь
                        {
                            if (!ActiveSnd.ContainsKey(devTemplate.SndGroups[snd].SndCnt))                              // Ключ = SndCnt - Значение = Индекс Активного запроса SndCnt
                            {
                                ActiveSnd.Add(devTemplate.SndGroups[snd].SndCnt, devTemplate.SndGroups.FindIndex(x => x.SndCnt == devTemplate.SndGroups[snd].SndCnt));
                            }

                            byte[] sndcode = ScadaUtils.HexToBytes(devTemplate.SndGroups[snd].SndCode, true);            // Чтение строки HEX из параметра SndCode
                            sndcode_ = sndcode[0];

                            if (sndcode_ == 0x01)                                                                        // Проверяем какой номер запроса у параметра SndCode - F=0x01 Текущие параметры
                            {
                                xValCnt01 = devTemplate.SndGroups[snd].SndCnt;                                           // Сохраняем номер запроса (SndCnt)
                                if (devTemplate.SndGroups[snd].SndData != "")
                                {
                                    startCnl = Convert.ToInt32(ScadaUtils.StrToDouble(devTemplate.SndGroups[snd].SndData.Trim())); // Сохранить начальный номер сигнала Текущих параметров
                                }
                            }
                            else if (sndcode_ == 0x07)                                                                   // Или F=0x07 (Вес импульса для Регистратора импульсов)
                            {
                                xValCnt07 = devTemplate.SndGroups[snd].SndCnt;                                           // Сохраняем номер запроса (SndCnt)
                                if (devTemplate.SndGroups[snd].SndData != "")
                                {
                                    startCnlv = Convert.ToInt32(ScadaUtils.StrToDouble(devTemplate.SndGroups[snd].SndData.Trim())); // Сохранить начальный номер сигнала Весов импульсов (Регистратор импульсов)
                                }
                            }
                            activeuse = true;                                                                            // Есть активные запросы
                        }
                    }
                }

                if (devTemplate.CmdGroups.Count != 0) // Определяем наличие активных команд и заносим в словарь Индексов команд
                {
                    for (int cmd = 0; cmd < devTemplate.CmdGroups.Count; cmd++)
                    {
                        if (devTemplate.CmdGroups[cmd].CmdActive)
                        {
                            if (!ActiveCmd.ContainsKey(devTemplate.CmdGroups[cmd].CmdCnl))                              // Ключ = номер команды CmdCnl - Значение = Индекс Активной команды CmdCnl
                            {
                                ActiveCmd.Add(devTemplate.CmdGroups[cmd].CmdCnl, devTemplate.CmdGroups.FindIndex(x => x.CmdCnl == devTemplate.CmdGroups[cmd].CmdCnl));
                            }
                        }
                    }
                }

                if (devTemplate.Values.Count != 0) // Проверка наличия записей переменных в конфигурации
                {
                    if (activeuse)
                    {
                        // ------------------- Сформировать Список параметров по меню ------------------
                        for (int ac = 0; ac < ActiveSnd.Count; ac++)
                        {
                            var valCnt_ = devTemplate.Values.FindIndex(x => x.ValCnt == ActiveSnd.ElementAt(ac).Key);

                            for (int val = 0; val < devTemplate.Values[valCnt_].Vals.Count; val++)  // МЕНЯЕМ valCnt_ на уже проиндексированный Словарь 
                            {
                                if (devTemplate.Values[valCnt_].Vals[val].SigActive)    // Проверяем переменную на активность
                                {
                                    sigN = devTemplate.Values[valCnt_].Vals[val].SigCnl;        // читаем номер сигнала переменной

                                    ActiveCnl.Add(new ActiveCnlList()
                                    {
                                        Cnl = sigN,                                             // Номер текущего активного сигнала
                                        Name = devTemplate.Values[valCnt_].Vals[val].SigName,   // Имя текущего активного сигнала
                                        Format = devTemplate.Values[valCnt_].Vals[val].SigType, // Тип переменной активного сигнала
                                        IdxValue = valCnt_,                                     // Индекс группы ответа (ValCnt), в которой находится сигнал
                                        MenuName = devTemplate.Values[valCnt_].ValMenu
                                    });

                                    // Проверяем номер запроса с параметром SndCode = F=0x01 и создаем маску запросов 
                                    if (devTemplate.Values[valCnt_].ValCnt == xValCnt01)
                                    {   // Заносим в маску номер сигнала - startCnl (1 по умолчанию) бит по расположению.
                                        mask_ch = BitFunc.SetBit(mask_ch, devTemplate.Values[valCnt_].Vals[val].SigCnl - startCnl, devTemplate.Values[valCnt_].Vals[val].SigActive);
                                        //maxch = ActiveCnl.FindLast(s => s.IdxValue == ActiveCnl.Find(d => d.Cnl == sigN).IdxValue).Cnl; //  Поиск Максимального номер канала для Текущих параметров

                                    }   // SigCnl - startCnl (1 по умолчанию) определяет какой бит 32-х разрядного числа выставить в 1 (единицу)

                                    if (devTemplate.Values[valCnt_].ValCnt == xValCnt07)
                                    {   // Заносим в маску номер сигнала - startCnlv (41 по умолчанию) бит по расположению.
                                        // Номера сигналов для запроса F=0x07, Вес импульса Регистратора импульсов должны начинаться с 41-ого если не задан в SndData
                                        mask_chv = BitFunc.SetBit(mask_chv, devTemplate.Values[valCnt_].Vals[val].SigCnl - startCnlv, devTemplate.Values[valCnt_].Vals[val].SigActive);

                                    }   // SigCnl - startCnlv (41 по умолчанию) определяет какой бит 32-х разрядного числа выставить в 1 (единицу)
                                }
                            }
                        }

                        // ------------ Создание тегов на основе созданного Списка Активных переменных  ------------ 

                        List<TagGroup> tagGroups = new List<TagGroup>();
                        TagGroup tagGroup;

                        var categoryCounts =                                            // Считаем количество Меню и количество переменных в Меню в шаблоне
                            from p in ActiveCnl
                            group p by p.MenuName into g
                            select new { NameMenu = g.Key, counts = g.Count() };

                        int cnt = 0;
                        foreach (var menu in categoryCounts)
                        {
                            tagGroup = new TagGroup(menu.NameMenu);                                                 // Создание меню Тегов

                            var actcnl = ActiveCnl.FindAll(s => s.MenuName == menu.NameMenu).OrderBy(d => d.Cnl);   // Сортировка активных каналов по каждому меню

                            foreach (var tags in actcnl)
                            {
                                sigN = ActiveCnl.Find(f => f.Cnl == tags.Cnl).Cnl;
                                tagGroup.KPTags.Add(new KPTag(sigN, ActiveCnl.Find(f => f.Cnl == tags.Cnl).Name));  // Заносим в тег Коммуникатора
                                ActiveCnl.Find(s => s.Cnl == sigN).IdxTag = cnt;                                    // Заносим номер тега Коммуникатора в Список
                                cnt++;                                                                              // Увеличиваем счетчик тегов
                            }
                            tagGroups.Add(tagGroup);                                                                // Добавляем группу тегов
                        }
                        InitKPTags(tagGroups);                                                                      // Инициализация всех тегов

                        // Определяем диапазон каналов в группах  Текущие параметры и Вес импульса

                        if (xValCnt01 != 0) // Если запрос с кодом 0x01 активен, переменная xValCnt01 содержит номер запроса
                        {
                            int idx = devTemplate.SndGroups.FindIndex(f => f.SndCnt == xValCnt01);
                            maxch = ActiveCnl.FindLast(d => d.IdxValue == idx).Cnl;     // Максимальный номер канала для Текущих параметров
                            res_ch = BitFunc.CountBit32(mask_ch);                       // Определяем количество бит = 1 в маске текущих параметров
                            string format = ActiveCnl.Find(d => d.IdxValue == idx).Format;

                            if (format == "float" || format == "uint32")
                            {
                                col = 4;
                            }
                            if (format == "double")
                            {
                                col = 8;
                            }
                        }

                        if (xValCnt07 != 0) // Если запрос с кодом 0x07 активен, переменная xValCnt07 содержит номер запроса
                        {
                            int idx = devTemplate.SndGroups.FindIndex(f => f.SndCnt == xValCnt07);
                            maxchv = ActiveCnl.FindLast(d => d.IdxValue == idx).Cnl;    // Максимальный номер канала для Веса импульсов
                            res_chv = BitFunc.CountBit32(mask_chv);                     // Определяем количество бит = 1 в маске Веса импульсов
                        }
                    }
                }
            }
        }

        // --------------------------------------------- Формирование буфера для команд чтения и команд записи
        private void Buf_Out(int Num, byte Fcode, byte[] bData, bool read) // формирование буфера отправки в порт Num = Номер индекса запроса или команды  
        {                                                                   // Fcode = параметр команды SndCode или CmdCode, read = true - чтение, выполняются запросы Snd или read = false, выполняются команды
            if (read)
            {                                                               // Тут собраны команды чтения
                if (Fcode == 0x01 || Fcode == 0x07)                         // Если код равен F=0x01 - Текущие параметры или F=0x07 - Вес омпульсов
                {
                    Array.Resize(ref buf_out, 14);                          // Меняем размер буфера для запроса Текущих параметров и Веса импульсов
                    if (Fcode == 0x01)
                    {
                        maskch = BitConverter.GetBytes(mask_ch);            // запись битовой маски Текущих параметров в массив байт
                        Array.Resize(ref buf_in, col * res_ch + 10);        // длина ответа 4 * n каналов (или 8 * n каналов) + 10 байт
                    }
                    else if (Fcode == 0x07)
                    {
                        maskch = BitConverter.GetBytes(mask_chv);           // запись битовой маски Веса импульсов в массив байт
                        Array.Resize(ref buf_in, 4 * res_chv + 10);         // длина ответа 4 * n каналов + 10 байт
                    }

                    Array.Copy(maskch, 0, buf_out, 6, maskch.Length);       // Копирование маски в буфер запроса
                }
                else if (Fcode == 0x04)                                     // Если код равен F=0x04 - Системное время
                {
                    Array.Resize(ref buf_out, 10);                          // Меняем размер буфера для запроса Системного времмени
                    Array.Resize(ref buf_in, 16);                           // длина ответа 16 байт
                }
                else if (Fcode == 0x0A)                                     // Если код равен F=0x0A - Параметры прибора
                {
                    Array.Resize(ref buf_out, 12);                          // Меняем размер буфера для запроса Параметров прибора
                    byte[] snddata = ScadaUtils.HexToBytes(devTemplate.SndGroups[Num].SndData, true);       // Чтение строки HEX из параметра SndData
                    buf_out[6] = snddata[0];                                                                // требуется 1 байт, код параметра
                    buf_out[7] = 0x00;                                                                      // второй байт будет со значением 0
                    Array.Resize(ref buf_in, 18);                           // длина ответа 18 байт
                }
            }
            else                                                            // Тут собраны команды записи
            {
                if (Fcode == 0x03 || Fcode == 0x08)                                                                     // Если код равен F=0x03 – код функции записи текущих показаний
                {                                                                                                       // Или F=0x08 - Вес импульса для Регистраторов импульса
                    Array.Resize(ref buf_out, 0x0E + bData.Length);                                                     // Меняем размер буфера для запроса Текущих параметров
                    maskch = Fcode == 0x03 ? BitConverter.GetBytes(mask_ch_wr) : BitConverter.GetBytes(mask_chv_wr);    // запись битовой маски редактируемого канала в массив байт
                    Array.Copy(maskch, 0, buf_out, 6, maskch.Length);                                                   // Копирование маски в буфер запроса
                    Array.Copy(bData, 0, buf_out, 10, bData.Length);                                                    // Копируем значение cmdVal в буфер запроса
                    Array.Resize(ref buf_in, 14);                                                                       // длина ответа 14 байт
                }
                else if (Fcode == 0x05)                                                                                 // Запись времени в прибор
                {
                    Array.Resize(ref buf_out, 10 + bData.Length);
                    Array.Copy(bData, 0, buf_out, 6, bData.Length);
                    Array.Resize(ref buf_in, 14);                                                                       // длина ответа 14 байт
                }
                else if (Fcode == 0x0B)                                                                                 // Запись параметров в прибор
                {
                    Array.Resize(ref buf_out, 12 + bData.Length);
                    Array.Copy(bData, 0, buf_out, 8, bData.Length);
                    byte[] cmddata = ScadaUtils.HexToBytes(devTemplate.CmdGroups[Num].CmdData, true);       // Чтение строки HEX из параметра SndData
                    buf_out[6] = cmddata[0];                                                                // требуется 1 байт, код параметра
                    buf_out[7] = 0x00;                                                                      // второй байт будет со значением 0
                    Array.Resize(ref buf_in, 12);                                                           // длина ответа 12 байт
                }

            }

            buf_out[4] = Fcode;                                         // Копируем в буфер код запроса F
            buf_out[5] = Convert.ToByte(buf_out.Length);                // Запись длины массива запроса - параметр L
            idBCD = BitConverter.GetBytes(ConvFunc.DecToBCD(Address));  // Преобразование адреса в BCD формат
            ConvFunc.Reverse_array(idBCD, false);                       // Переворот буфера старшим байтом вперед
            Array.Copy(idBCD, 0, buf_out, 0, idBCD.Length);             // Копирование адреса в буфер запроса

            byteID = BitConverter.GetBytes((ushort)rnd.Next(1, 65535)); // Сформировать случайный ID запроса
            buf_out[buf_out.Length - 4] = byteID[0];
            buf_out[buf_out.Length - 3] = byteID[1];

            crc = CrcFunc.CalcCRC16(buf_out, buf_out.Length - 2);       // Рассчет контрольной суммы CRC16
            buf_out[buf_out.Length - 2] = (byte)(crc % 256);            // Запись младшего байта контрольной суммы в буфер
            buf_out[buf_out.Length - 1] = (byte)(crc / 256);            // Запись старшего байта контрольной суммы в буфер
        }

        // Сессия опроса ------------------------------------------------------------------------------------------------------------------------------------
        public override void Session()
        {
            base.Session();             // Опрос должен происходить согласно активности списка запросов по Словарю ActiveSnd
            if (!fileyes)               // Если конфигурация не была загружена, выставляем все теги в невалидное состояние и выходим         
            {
                InvalidateCurData();
                return;
            }

            for (int i = 0; i < ActiveSnd.Count; i++)
            {
                int sndCnt_ = ActiveSnd.Values.ElementAt(i);                // Выполняем запросы поочередно по индексам из словаря Активных запросов

                byte[] sndcode = ScadaUtils.HexToBytes(devTemplate.SndGroups[sndCnt_].SndCode, true); // Чтение строки HEX из параметра SndCode
                sndcode_ = sndcode[0];

                // ------------------  Тут вызвать формирование буфера запроса --------------------------------------
                Buf_Out(sndCnt_, sndcode_, null, true);                                             // отправить в функцию Номер и Байт запроса

                if (lastCommSucc)
                {
                    lastCommSucc = false;
                    int tryNum = 0; // Счетчик для корректных ответов

                    // Выполняем опрос если был загружен файл конфигурации
                    while (RequestNeeded(ref tryNum))
                    {
                        Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText);    //послать запрос в порт
                        ExecWriteToLog(logText);                                                                        // вывести запрос в Журнал линии связи

                        readcnt = Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText);  //считать значение из порта
                        ExecWriteToLog(logText);                                                                                                // вывести запрос в Журнал линии связи

                        // ------------------------------Тут проверка на корректность ответа - ID запроса и CRC -------------------------------------------------------------------
                        var valCnt_ = devTemplate.Values.FindIndex(x => x.ValCnt == ActiveSnd.ElementAt(i).Key); // Разбираем ответ поочередно по индексам из Списка Активных запросов

                        if (readcnt == buf_in.Length || readcnt == 11)
                        {
                            crc = CrcFunc.CalcCRC16(buf_in, readcnt);           // Рассчет CRC16 полученного ответа, при совпадении должно вернуть 0 при расчете CRC16(Modbus) и полного буфера вместе с CRC
                            byte fCode = buf_in[4];
                            Array.Copy(buf_in, readcnt - 4, byteIDres, 0, 2);

                            if (!(crc == 0 & fCode != 0 & byteID.SequenceEqual(byteIDres)))                // Проверка CRC, параметра F и ID запроса
                            {
                                if (crc != 0)
                                {
                                    ExecWriteToLog(CommPhrases.ResponseCrcError);
                                }
                                else if (fCode == 0)
                                {
                                    string err = Error_code(buf_in[6]);
                                    ExecWriteToLog(CommPhrases.IncorrectCmdData + " - " + err);                // При некорректном запросе F будет равен 0x00
                                }
                                else if (!byteID.SequenceEqual(byteIDres))
                                {
                                    ExecWriteToLog("ID ответа не совпадает с ID запроса");                     // При несовпадении ID
                                }
                                FinishRequest();
                                invalidData(valCnt_);                                                          // выставить сигналы в невалидное состояние
                            }
                            else
                            {
                                int index_bufin = 6;                                                            // Индекс первой переменной в ответе прибора

                                for (int sig = 0; sig < devTemplate.Values[valCnt_].Vals.Count; sig++)          // Разбор по количеству переменных Vals в ответе
                                {
                                    if (devTemplate.Values[valCnt_].Vals[sig].SigActive)                        // Если переменная активна, читаем и разбираем ее
                                    {
                                        string sig_type = devTemplate.Values[valCnt_].Vals[sig].SigType;        // читаем тип переменной
                                        double range = devTemplate.Values[valCnt_].Vals[sig].Range;             // читаем множитель (мало ли, вдруг пригодится) :)

                                        int k = ActiveCnl.Find(s => s.Cnl == devTemplate.Values[valCnt_].Vals[sig].SigCnl).IdxTag; // Находим в списке Индекс переменной и Указываем индекс Тега

                                        if (sig_type == "float")
                                        {
                                            SetCurData(k, BitConverter.ToSingle(buf_in, index_bufin) * range, 1);       // Конвертируем буфер байт в переменную float
                                        }
                                        else if (sig_type == "double")
                                        {
                                            SetCurData(k, BitConverter.ToDouble(buf_in, index_bufin) * range, 1);       // Конвертируем буфер байт в переменную double
                                        }
                                        else if (sig_type == "uint16")
                                        {
                                            SetCurData(k, BitConverter.ToUInt16(buf_in, index_bufin) * range, 1);       // Конвертируем буфер байт в переменную UInt16
                                        }
                                        else if (sig_type == "uint32")
                                        {
                                            SetCurData(k, BitConverter.ToUInt32(buf_in, index_bufin) * range, 1);       // Конвертируем буфер байт в переменную UInt32
                                        }
                                        else if (sig_type == "DateTime")                                                // Определяем системное время и конвертируем в double для Scada
                                        {
                                            if (!myTagId.ContainsKey(devTemplate.Values[valCnt_].Vals[sig].SigCnl))     // Указываем номер сигнала для преобразования в текстовую строку  
                                            {                                                                           // в окне Данных КП Коммуникатора
                                                myTagId.Add(devTemplate.Values[valCnt_].Vals[sig].SigCnl, "DateTime");
                                            }

                                            int year = Convert.ToInt32(buf_in[index_bufin]) + 2000;     // Читаем из ответа переменные года
                                            int month = Convert.ToInt32(buf_in[index_bufin + 1]);       // месяца
                                            int day = Convert.ToInt32(buf_in[index_bufin + 2]);         // дня
                                            int hour = Convert.ToInt32(buf_in[index_bufin + 3]);        // часа
                                            int minute = Convert.ToInt32(buf_in[index_bufin + 4]);      // минут
                                            int second = Convert.ToInt32(buf_in[index_bufin + 5]);      // секунд
                                            DateTime dateTime = new DateTime(year, month, day, hour, minute, second); //  формируем переменную времени в формате DateTime
                                            SetCurData(k, dateTime.ToOADate(), 1);
                                        }

                                        if (devTemplate.Values[valCnt_].ValCnt == xValCnt01 || devTemplate.Values[valCnt_].ValCnt == xValCnt07)
                                        {
                                            if (sig_type == "float")
                                            {
                                                index_bufin = index_bufin + 4;           // Увеличиваем индекс переменной для следующего текущего параметра для float
                                            }
                                            else if (sig_type == "double")
                                            {
                                                index_bufin = index_bufin + 8;           // Увеличиваем индекс переменной для следующего текущего параметра для double
                                            }
                                        }
                                    }
                                }
                                ExecWriteToLog(CommPhrases.ResponseOK);

                                lastCommSucc = true;
                                FinishRequest();
                            }
                        }
                        else
                        {
                            if (readcnt == 0)
                            {
                                ExecWriteToLog(CommPhrases.ResponseError);              // Нет ответа по Timeout - Ошибка связи!
                            }
                            else
                            {
                                ExecWriteToLog(CommPhrases.IncorrectResponseLength);    // Некорректная длина ответа 
                            }
                            FinishRequest();
                            invalidData(valCnt_);                           // выставить сигналы в невалидное состояние
                        }
                        // завершение запроса
                        tryNum++;
                    }
                }
            }
            CalcSessStats(); // расчёт статистики
        }

        private void invalidData(int cnt) // Расчитать количество невалидных сигналов и определить индекс первого сигнала, выставить сигналы в невалидное состояние
        {
            // Найти индексы начального и конечного номера Тега в Списке, где первое и последнее вхождение номер индекса ответа IdxValue, где произошла ошибка
            var m = ActiveCnl.Find(n => n.IdxValue == cnt).IdxTag;      // Прочитали Номер Тега (IdxTag) первого найденного  в IdxValue списке (стартовый тег)
            var t = ActiveCnl.FindLast(d => d.IdxValue == cnt).IdxTag;  // Прочитали Номер Тега (IdxTag) последней переменной в группе IdxValue (конечный тег)

            // Определяем количество тегов в запросе по формуле (t-m)+1 
            InvalidateCurData(m, (t - m) + 1);
        }

        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);
            lastCommSucc = false;

            bool WriteOk = false;               // Идентификатор успешной записи
            mask_ch_wr = 0;                     // переменная для параметра MASK_CH записи данных каналов (Регистратор импульсов)
            mask_chv_wr = 0;                    // Переменная для параметра MASK_CH записи Веса импульсов (Регистратор импульсов)

            byte cmdCode = 0x00;                // переменная для байта запроса CmdCode - параметр F протокола (номера для записи)

            byte[] byteData = new byte[1];      // Буфер для значения переменной
            double cmdVal = cmd.CmdVal;
            int cmdNum = cmd.CmdNum;

            int cmdCnl = ActiveCmd[cmdNum];     // Чтение индекса команды по ключу из Словаря

            if (cmd.CmdTypeID == BaseValues.CmdTypes.Standard)
            {
                byte[] cmdcode = ScadaUtils.HexToBytes(devTemplate.CmdGroups[cmdCnl].CmdCode, true); // Чтение строки HEX из параметра CmdCode
                cmdCode = cmdcode[0];

                string cmdtype = devTemplate.CmdGroups[cmdCnl].CmdType; // Чтение строки Типа переменной команды

                // Определив диапазон проверяем к какому из них относятся Текущие параметры и Веса импульса для составления маски

                if ((cmdNum >= startCnl && cmdNum <= maxch) || (cmdNum >= startCnlv && cmdNum <= maxchv))
                {
                    if ((cmdNum >= startCnl && cmdNum <= maxch) && !(cmdNum >= startCnlv && cmdNum <= maxchv))
                    {
                        mask_ch_wr = BitFunc.SetBit(mask_ch_wr, cmdNum - startCnl, true);       // Если каналы относятся к Текущим данным, то формируем маску для  записи маски текущих данных
                    }
                    else
                    {
                        mask_chv_wr = BitFunc.SetBit(mask_chv_wr, cmdNum - startCnlv, true);    // Иначе для записи маски Весов импульсов
                    }
                }
                if (cmdtype == "uint16")
                {
                    Array.Resize(ref byteData, 2);
                    byteData = BitConverter.GetBytes(Convert.ToUInt16(cmdVal));
                }
                else if (cmdtype == "float")
                {
                    Array.Resize(ref byteData, 4);
                    byteData = BitConverter.GetBytes(Convert.ToSingle(cmdVal));
                }
                else if (cmdtype == "double")
                {
                    Array.Resize(ref byteData, 8);
                    byteData = BitConverter.GetBytes(cmdVal);
                }
                else if (cmdtype == "DateTime")
                {
                    Array.Resize(ref byteData, 6);
                    DateTime dateTime = DateTime.FromOADate(cmdVal);
                    byteData[0] = Convert.ToByte(dateTime.Year - 2000);
                    byteData[1] = Convert.ToByte(dateTime.Month);
                    byteData[2] = Convert.ToByte(dateTime.Day);
                    byteData[3] = Convert.ToByte(dateTime.Hour);
                    byteData[4] = Convert.ToByte(dateTime.Minute);
                    byteData[5] = Convert.ToByte(dateTime.Second);
                }

                if (cmdCode == 0x0B) Array.Resize(ref byteData, 8); // Увеличить размер буфера до 8 байт записываемого параметра F=0x0B PARAM_VAL_NEW

                Buf_Out(cmdCnl, cmdCode, byteData, false);                                  // отправить в функцию Номер индекса команды управления и Байт запроса

                Connection.Write(buf_out, 0, buf_out.Length, CommUtils.ProtocolLogFormats.Hex, out logText);    //послать запрос в порт
                ExecWriteToLog(logText);                                                                        // вывести запрос в Журнал линии связи 

                readcnt = Connection.Read(buf_in, 0, buf_in.Length, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText);  //считать значение из порта
                ExecWriteToLog(logText);                                                                                                // вывести запрос в Журнал линии связи

                // Проверка выполнения команды прибором - определяется по ответу прибора на запись команды

                if (readcnt == buf_in.Length || readcnt == 11)
                {
                    crc = CrcFunc.CalcCRC16(buf_in, readcnt);           // Рассчет CRC16 полученного ответа, при совпадении должно вернуть 0 при расчете CRC16(Modbus) и полного буфера вместе с CRC
                    byte fCode = buf_in[4];                             // Чтение кода команды F
                    Array.Copy(buf_in, readcnt - 4, byteIDres, 0, 2);

                    if (!(crc == 0 & fCode != 0 & byteID.SequenceEqual(byteIDres)))                    // Проверка CRC, параметра F и ID запроса
                    {
                        if (crc != 0)
                        {
                            ExecWriteToLog(CommPhrases.ResponseCrcError);
                        }
                        else if (fCode == 0)
                        {
                            string err = Error_code(buf_in[6]);
                            ExecWriteToLog(CommPhrases.IncorrectCmdData + " - " + err);                // При некорректном запросе F будет равен 0x00
                        }
                        else if (!byteID.SequenceEqual(byteIDres))
                        {
                            ExecWriteToLog("ID ответа не совпадает с ID запроса");                     // При несовпадении ID
                        }
                        FinishRequest();
                    }
                    else
                    {
                        if (fCode == 0x03 || fCode == 0x08)
                        {
                            byte[] maskchRes = new byte[4];
                            Array.Copy(buf_in, 6, maskchRes, 0, 4);
                            if (maskch.SequenceEqual(maskchRes)) WriteOk = true;
                        }
                        if (fCode == 0x05)
                        {
                            if (buf_in[6] != 0) WriteOk = true;
                        }
                        if (fCode == 0x0B)
                        {
                            UInt16 Result_WR = BitConverter.ToUInt16(buf_in, 6);
                            if (Result_WR == 0) WriteOk = true;
                        }

                        if (WriteOk)
                        {
                            lastCommSucc = true;
                            string nameCnl = ActiveCnl.Find(c => c.Cnl == cmdNum).Name;
                            ExecWriteToLog($"Запись команды {nameCnl} - " + CommPhrases.ResponseOK);
                        }
                        else
                        {
                            ExecWriteToLog(CommPhrases.WriteDataError);
                        }
                        FinishRequest();
                    }
                }
                else
                {
                    if (readcnt == 0)
                    {
                        ExecWriteToLog(CommPhrases.ResponseError);              // Нет ответа по Timeout - Ошибка связи!
                    }
                    else
                    {
                        ExecWriteToLog(CommPhrases.IncorrectResponseLength);    // Некорректная длина ответа 
                    }
                    FinishRequest();
                }
            }
            else
            {
                WriteToLog(CommPhrases.IllegalCommand);
            }

            CalcCmdStats();
        }
    }
}
