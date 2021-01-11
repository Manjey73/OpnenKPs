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
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Scada.Data.Tables;
using ScadaCommFunc;
using System.Threading;
using Scada.Data.Configuration;
using System.Text;
using System.Reflection;

namespace Scada.Comm.Devices
{
    public class KpMercury23xLogic : KPLogic
    {
        private DevTemplate devTemplate = new DevTemplate();

        //private SaveParam saveParam = new SaveParam();

        private int read_cnt = 0;
        private string fileName = "";
        private string filePath = "";
        private bool fileyes = false;   // При отсутствии файла не выполнять опрос
        private int idgr = 0;           // переменная для индекса группы
        private string Allname;         // Полное имя тега
        private bool CommSucc = true;
        private byte[] inBuf;

        // TEST TEST TEST
        private bool settLoaded;   // загрузка настроек выполнена
        private DateTime hrBegDT;  // дата и час начала часовых архивов
        private DateTime dayBegDT; // дата и час начала суточных архивов
        private DateTime hrReqDT;  // дата и час запроса часовых архивов
        private DateTime dayReqDT; // дата запроса суточных архивов
        // TEST TEST TEST

        private Requests requests;

        private void InitRequests()
        {
            requests.testCnlReq = Protocol.TestCnlReq(Address);
            requests.openCnlReq = Protocol.OpenCnlReq(Address, uroven, Pass());
            requests.openAdmReq = Protocol.OpenCnlReq(Address, "2", PassA());
            requests.closeCnlReq = Protocol.WriteComReq(Address, 0x02);
            requests.readTimeReq = Protocol.WriteCompReq(Address, 0x04, 0x00);
            requests.lastSyncReq = Protocol.WriteCompReq(Address, 0x04, 0x02, new byte[] { 0xFF }); // Чтение последней записи журнала синхронизации времени
            requests.kuiReq = Protocol.KuiReq(Address);
            requests.infoReq = Protocol.InfoReq(Address);
            requests.curTimeReq = Protocol.CurTimeReq(Address);
            requests.wordStatReq = Protocol.WriteCompReq(Address, 0x04, 0x14, new byte[] { 0xFF }); // Чтение последней записи журнала кода словосостояния прибора

            // Формирование запроса фиксации данных в зависимости от параметра multicast
            if (devTemplate.multicast)
            {
                requests.fixDataReq = Protocol.FixDataReq(0xFE);
            }
            else
            {
                requests.fixDataReq = Protocol.FixDataReq(Address);
            }
        }

        private string Pass()
        {
            string kpNum_pass = string.Concat(Number.ToString(), "_pass");
            string pass = String.IsNullOrEmpty(CustomParams.GetStringParam(kpNum_pass, false, "")) ? password : CustomParams.GetStringParam(kpNum_pass, false, "");
            return pass;
        }

        private string PassA()
        {
            string kpNum_passA = string.Concat(Number.ToString(), "_passA");
            string pass = String.IsNullOrEmpty(CustomParams.GetStringParam(kpNum_passA, false, "")) ? passwordA : CustomParams.GetStringParam(kpNum_passA, false, "");
            return pass;
        }

        // Пример создания пользовательского параметра, привязанного к КП
        // Вызоа в секциях OnAddedToCommLine и OnCommLineStart GetMyProps("name")
        // Имя по аналогии с kpLastDt, сформировано по номеру КП и имени параметра
        // Если имя будет без номера КП, переменная будет общей для всех КП на линии
        // Переменная имеет только формат string
        // --------------------------------------------------------------
        //protected virtual string kpLastDt
        //{
        //    get { return Number.ToString() + "_lastDt"; }
        //}
        //private string GetMyProps(string myProps)
        //{
        //    string myValue = CustomParams.ContainsKey(myProps) ?
        //        CustomParams[myProps] as string : null;
        //    if (myValue == null)
        //    {
        //        if (!CustomParams.ContainsKey(myProps))
        //        {
        //            CustomParams.Add(myProps, myValue);
        //        }
        //    }
        //    return myValue;
        //}
        // --------------------------------------------------------------
        // Пример создания пользовательского параметра, привязанного к КП

        public static long Ticks() // возвращает время в миллисекундах
        {
            DateTime now = DateTime.Now;
            long time = now.Ticks / 10000;
            return time;
        }

        public class MyDevice                   // переменные для устройств на линии связи
        {
            public SaveParam saveParam = new SaveParam();
            public bool testcnl = false;        // фиксация команды тестирования канала
            public bool opencnl = false;        // фиксация команды авторизации и открытия канала
            public bool Kui = false;            // фиксация команды чтения коэффициентов трансформации
            public int ku = 1;                  // Коэффициент трансформации напряжения 
            public int ki = 1;                  // Коэффициент трансформации тока
            public int[] parkui = new int[13];  // массив для данных коэффициентов
            public long tik = 0;                // таймер времени открытого канала
            public int firstKp = 0;             // КП, являющийся первым КП в опросе, последующие не записывают номер КП, значение остается = 0
            public bool firstFix = false;       // Переменная для фиксации адреса первого КП
            public DateTime dt;                 // Время команды фиксации для сравнения 

            // Данные счетчика
            public bool readInfo = false;       // блокировка чтения информации счетчика
            public int serial = 0;              // Серийный номер счетчика
            public int Aconst = 500;            // Постоянная счетчика, по умолчанию 500 имп/квт*ч
            public DateTime made;               // Дата изготовления
            public DateTime srezDt;             // Дата чтения архива
            public DateTime LastSyncDt;         // Время последней синхронизации в приборе

            public override string ToString()
            {
                string outprops = string.Concat("SN_", serial.ToString(), " Изготовлен ", made.ToString("dd.MM.yyyy"), " Время архива ", srezDt.ToString());
                return outprops;
            }

        }

        protected virtual string address
        {
            get
            {
                return devTemplate.Name + "_" + Convert.ToString(Address);
            }
        }

        private MyDevice GetMyDevice()
        {
            MyDevice devaddr = CommonProps.ContainsKey(address) ?
                CommonProps[address] as MyDevice : null;

            if (devaddr == null)
            {
                devaddr = new MyDevice();
                if (!CommonProps.ContainsKey(address))
                {
                    CommonProps.Add(address, devaddr);
                }
            }
            return devaddr;
        }

        public static string ToLogString(byte errcode)
        {
            string logs = "";
            switch (errcode)
            {
                case 0x00: logs = "Связь восстановлена"; break;
                case 0x01: logs = "Недопустимая команда или параметр"; break;
                case 0x02: logs = "Внутренняя ошибка счетчика"; break;
                case 0x03: logs = "Недостаточен уровень доступа"; break;
                case 0x04: logs = "Внутренние часы корректировались"; break;
                case 0x05: logs = "Не открыт канал связи"; break;
                case 0x09: logs = "Потеря связи"; break; // Добавленный код 9 для потери связи
            }
            return logs;
        }

        public static int ConstA(byte aconst)
        {
            aconst = (byte)(aconst & 0x0f);
            // Постоянная счетчика в имп/квт*ч  - 2.3.16 - Чтение варианта исполнения 2-й байт 0-3 биты
            int consta = 500;
            switch (aconst)
            {
                case 0x00: consta = 5000; break;
                case 0x01: consta = 25000; break;
                case 0x02: consta = 1250; break;
                case 0x03: consta = 500; break;
                case 0x04: consta = 1000; break;
                case 0x05: consta = 250; break;
            }
            return consta;
        }

        /// <summary>
        /// Преобразовать данные тега КП в строку
        /// </summary>
        protected override string ConvertTagDataToStr(int signal, SrezTableLight.CnlData tagData) // Необходимо продумать как передать сюда список типов переменных - текст, время, цифровое и т.д.
        {
            if (tagData.Stat > 0 && signal == 73)
            {
                byte[] data = BitConverter.GetBytes(Convert.ToUInt64(tagData.Val));
                long N = BitConverter.ToInt64(data, 0);

                Array.Resize(ref data, 6);
                Array.Reverse(data);
                return ScadaUtils.BytesToHex(data).ToString() + " hex";
            }
            return base.ConvertTagDataToStr(signal, tagData);
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

        private int[] bwri = new int[13]; // BWRI для запроса параметром 14h
        private int[] bwrc = new int[13]; // Разрешающая способность регистров хранения 
        private int[] b_length = new int[13]; // количество байт в ответе счетчика
        private int[] parb = new int[13];    // количество байт в параметре ответа (4 или 3)
        private int[] parc = new int[13];    // количество параметров в ответе (4, 3 или 1)

        // Массив значений параметров BWRI счетчика + 'энергии от сброса параметр 14h
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
        private int[] bwri_14 = new int[] { 0x00, 0x04, 0x08, 0x30, 0x10, 0x20, 0x50, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 14h
        private int[] bwrc_14 = new int[] { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        private int[] b_length_14 = new int[] { 19, 19, 19, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        private int[] parb_14 = new int[] { 4, 4, 4, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        private int[] parc_14 = new int[] { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)


        // Массив значений параметров BWRI счетчика + 'энергии от сброса для чтения параметром 16h
        // Команды BWRI для запроса 16h:
        // 0x00 - Мощность P по сумме фаз, фазе 1, фазе 2, фазе 3   (Вт)
        // 0x04 - Мощность Q по сумме фаз, фазе 1, фазе 2, фазе 3   (вар)
        // 0x08 - Мощность S по сумме фаз, фазе 1, фазе 2, фазе 3   (ВА)
        // 0x11 - Напряжение по фазе 1, фазе 2, фазе 3              (В)
        // 0x30 - Косинус ф по сумме фаз, фазе 1, фазе 2, фазе3
        // 0x21 - Ток по фазе 1, фазе 2, фазе 3                     (А)
        // 0x40 - Частота сети
        // 0x51 - Угол м-ду ф. 1 и 2, 1 и 3, 2 и 3                  (градусы)

        private int[] bwri_16 = { 0x00, 0x04, 0x08, 0x30, 0x11, 0x21, 0x51, 0x40, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4 }; // BWRI для запроса параметром 16h
        private int[] bwrc_16 = { 100, 100, 100, 1000, 100, 1000, 100, 100, 1000, 1000, 1000, 1000, 1000 }; // Разрешающая способность регистров хранения 
        private int[] b_length_16 = { 15, 15, 15, 15, 12, 12, 12, 6, 19, 19, 19, 19, 19 };   // количество байт в ответе счетчика
        private int[] parb_16 = { 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4 };    // количество байт в параметре ответа (4 или 3)
        private int[] parc_16 = { 4, 4, 4, 4, 3, 3, 3, 1, 4, 4, 4, 4, 4 };    // количество параметров в ответе (4, 3 или 1)

        // Массив значений энергии по фазам прямого направления и чтения энергии от сброса функцией 0x05 при чтении счетчика параметром 16h кода функции 0x08
        private int[] massenergy = new int[] { 0, 1, 2, 3, 4 };

        private bool newmass = false;
        private bool newenergy = false;

        private int tag = 1;
        private int[] nbwri = new int[1];
        private int[] nbwrc = new int[1];
        private int[] nb_length = new int[1];
        private int[] nparb = new int[1];
        private int[] nparc = new int[1];
        private int[] nenergy = new int[1];
        private bool readstatus = false;    // Читать статус счетчика
        private byte code_err = 0x0;
        private byte code = 0x0;
        private int Napr = 1;               // Направление Активной или Реактивной мощности 1 = прямое (бит направления = 0), при обратном значение = -1
        private int fixTime = 30;           // по умолчанию разница времени фиксации 30 секунд
        private int saveTime;               // Период сохранения данных БД
        private bool timeSync = false;
        private uint readPQSUI = 0;
        private uint energyL = 0;
        private int halfArch = 2;               // По умолчанию номер архивного среза
        private int srezPeriod = 30;            // Период среза средних мощностей, по умолчанию 30 минут
        private byte[] wordStat = new byte[8];  // массив байт для словосостояния прибора

        private byte CMD;           // Command code (Код команды)
        private byte PAR;           // Parametr code (Код параметра)

        private int mask_g1 = 0; // Входная переменная для выбора тегов

        private string readparam, password, passwordA, uroven; //входная строка для параметра команды 0x08, объявление переменной для пароля и уровня доступа

        /// <summary>
        /// Параметр множителя переменных запросов
        /// </summary>
        public class ValParam
        {
            public double range;
        }
        private List<ValParam> ValPar = new List<ValParam>();

        public class Profile
        {
            public string profileName;
            public int signal;
            public double range;
            public int offset;
        }
        private List<Profile> profile = new List<Profile>();

        // Активные команды шаблона
        public class SetCommand
        {
            public string Name;
            public int signal;
            public int mode;
            public byte[] setCommand;
            public int scCnt;           // Количество принимаемы байт
            public int datalen;         // длина блока данных в байтах
        }
        private List<SetCommand> setComm = new List<SetCommand>();

        private Dictionary<int, SetCommand> ActiveCmd = new Dictionary<int, SetCommand>(); // Ключ = номер команды (сигнала) - Значение = Индекс Команды в списке активных команд

        private Dictionary<int, KPTag> myTags = new Dictionary<int, KPTag>();

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

            if (devTemplate != null)
            {
                readstatus = devTemplate.readStatus;
                password = string.IsNullOrEmpty(devTemplate.password) ? "111111" : devTemplate.password;
                passwordA = string.IsNullOrEmpty(devTemplate.AdmPass) ? "222222" : devTemplate.AdmPass;

                uroven = string.IsNullOrEmpty(devTemplate.mode.ToString()) ? "1" : devTemplate.mode.ToString(); // Преобразование int '1' или '2' к строке для совместимости с кодом драйвера, по умолчанию '1' если строка пуста
                readparam = string.IsNullOrEmpty(devTemplate.readparam) ? "14h" : devTemplate.readparam;

                saveTime = devTemplate.SaveTime;
                timeSync = devTemplate.SyncTime;
                halfArch = devTemplate.halfArchStat == 0 ? halfArch : devTemplate.halfArchStat;

                fixTime = devTemplate.fixtime == 0 ? fixTime : devTemplate.fixtime;

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

                if (devTemplate.ProfileGroups.Count != 0) //Определить наличие запросов профилей
                {
                    for (int i = 0; i < devTemplate.ProfileGroups.Count; i++)
                    {
                        if (devTemplate.ProfileGroups[i].Active)
                        {
                            for (int k = 0; k < devTemplate.ProfileGroups[i].value.Count; k++)
                            {
                                if (devTemplate.ProfileGroups[i].value[k].active)
                                {
                                    profile.Add(new Profile
                                    {
                                        profileName = devTemplate.ProfileGroups[i].value[k].name,
                                        signal = devTemplate.ProfileGroups[i].value[k].signal,
                                        range = SToDouble(devTemplate.ProfileGroups[i].value[k].range),
                                        offset = k * 2
                                    });
                                }
                            }
                        }
                    }
                }

                if (devTemplate.CmdGroups.Count > 0)
                {
                    for (int c = 0; c < devTemplate.CmdGroups.Count; c++)
                    {
                        int errorCnt = 0;
                        byte[] comm = null;
                        byte[] cData = null;
                        string paras = null;
                        int lendata = 0;

                        if (devTemplate.CmdGroups[c].Active)
                        {
                            int cnt_ = devTemplate.CmdGroups[c].inCnt;

                            try
                            {
                                byte[] cmd_ = ScadaUtils.HexToBytes(devTemplate.CmdGroups[c].Cmd, true);
                                CMD = cmd_[0];
                            }
                            catch
                            {
                                WriteToLog(string.Format(Localization.UseRussian ?
                                    "Ошибка задания кода команды. Строка команды не является Hex или пуста. Индекс CmdGroup = {0}" :
                                    "Error setting command сode. The command line is not Hex or empty. Index CmdGroup = {0}", c));
                                errorCnt++;
                            }

                            try
                            {
                                paras = string.IsNullOrEmpty(devTemplate.CmdGroups[c].Par) ? null : devTemplate.CmdGroups[c].Par;
                                if (paras != null)
                                {
                                    byte[] par_ = ScadaUtils.HexToBytes(devTemplate.CmdGroups[c].Par, true);
                                    PAR = par_[0];
                                }
                            }
                            catch
                            {
                                WriteToLog(string.Format(Localization.UseRussian ?
                                    "Ошибка задания параметра команды. Строка параметров не является Hex. Индекс CmdGroup = {0}" :
                                    "Error setting the command parameter. The parameter string is not Hex. Index CmdGroup = {0}", c));
                                errorCnt++;
                            }

                            try
                            {
                                string datas = string.IsNullOrEmpty(devTemplate.CmdGroups[c].Data) ? null : devTemplate.CmdGroups[c].Data;
                                if (datas != null)
                                {
                                    byte[] data_ = ScadaUtils.HexToBytes(datas, true);
                                    Array.Resize(ref cData, data_.Length);
                                    Array.Copy(data_, cData, data_.Length);
                                    lendata = data_.Length;
                                }
                            }
                            catch
                            {
                                WriteToLog(string.Format(Localization.UseRussian ?
                                    "Ошибка: строка данных не является Hex. Индекс CmdGroup = {0}" :
                                    "Error: the data string is not Hex. Index CmdGroup = {0}", c));
                                errorCnt++;
                            }

                            if (paras != null)
                            {
                                comm = Protocol.WriteCompReq(Address, CMD, PAR, cData);
                            }
                            else
                            {
                                comm = Protocol.WriteComReq(Address, CMD, cData);
                            }

                            if (errorCnt == 0)
                            {
                                setComm.Add(new SetCommand
                                {
                                    Name = devTemplate.CmdGroups[c].Name,
                                    signal = devTemplate.CmdGroups[c].Signal,
                                    mode = devTemplate.CmdGroups[c].Mode,
                                    setCommand = comm,
                                    scCnt = cnt_,
                                    datalen = lendata
                                });
                            }
                        }
                    }
                }

                // Сохранить в словаре сигналы  и CmdGroup активных команд
                foreach (var activecmd in setComm)
                {
                    if (!ActiveCmd.ContainsKey(activecmd.signal))
                    {
                        ActiveCmd.Add(activecmd.signal, activecmd);
                    }
                }
            }

            if (readparam == "14h")
            {
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


            readPQSUI = Convert.ToUInt32(mask_g1 & 0x1FFF); // проверка маски на необходимость чтения мгновенных значений

            int mgn_znac = mask_g1 & 0xFF; // отсечь мгновенные значения для организации отображения тегов
            int energy = mask_g1 & 0x3FF00; // Отсечь параметры энергии для организации отображения тегов

            uint par14h = Convert.ToUInt32(mask_g1 & 0x1FFF); // отсечь количество параметров для команды 08h и параметра 14h

            energyL = Convert.ToUInt32(mask_g1 & 0x3E000); // Проверка наличия опроса значений энергии прямого направления
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

            if (profile.Count > 0) // Нужен индекс группы профилей ?
            {
                tagGroup = new TagGroup("Профили мощностей");

                for (int p = 0; p < profile.Count; p++)
                {
                    tagGroup.KPTags.Add(new KPTag(profile[p].signal, profile[p].profileName));
                }
                tagGroups.Add(tagGroup);
            }

            if (readstatus)
            {
                tagGroup = new TagGroup("Статус:");
                tagGroup.KPTags.Add(new KPTag(70, "Код ошибки:"));
                tagGroup.KPTags.Add(new KPTag(71, "коэфф. трансформации тока:"));
                tagGroup.KPTags.Add(new KPTag(72, "коэфф. трансформации напряжения:"));
                tagGroup.KPTags.Add(new KPTag(73, "Слово состояния:")); // На будущее
                tagGroups.Add(tagGroup);
            }


            InitKPTags(tagGroups);

            // Добавить в словарь активные сигналы и KPTags, соответсвующие сигналам
            // для дальнейшего использования
            foreach (var tag in KPTags)
            {
                if (!myTags.ContainsKey(tag.Signal))
                {
                    myTags.Add(tag.Signal, tag);
                }
            }
        }

        //---------------------------------------------------------------
        public KpMercury23xLogic(int number) : base(number)
        {
            inBuf = new byte[300];
            requests = new Requests();
            CanSendCmd = true;
            ConnRequired = true;
        }
        //---------------------------------------------------------------

        /// <summary>
        /// Выполнить действия при запуске линии связи
        /// </summary>
        public override void OnCommLineStart()
        {
            GetMyDevice();
            InitRequests();
            MyDevice prop = (MyDevice)CommonProps[address];

            // Чтение параметров счетчика из файла по переменной readinfo
            if (!prop.readInfo)
            {
                hrReqDT = DateTime.MinValue;

                LoadSettings();

                if (settLoaded)
                {
                    bool t1 = DateTime.TryParse(prop.saveParam.madeDt, out prop.made);
                    bool t2 = int.TryParse(prop.saveParam.serial, out prop.serial);
                    bool t3 = int.TryParse(prop.saveParam.constA, out prop.Aconst);

                    // Если все переменные считаны и корректны то запрос чтения параметров выполняться не будет
                    prop.readInfo = (t1 && t2 && t3) ;

                    bool t4 = true;
                    if (saveTime == 0) t4 = DateTime.TryParse(prop.saveParam.arcDt, out prop.srezDt); // TEST

                }
            }
        }

        /// <summary>
        /// Отправить запрос и сделать запись в журнал
        /// </summary>
        private void Write(byte[] request)
        {
            string logText;
            WriteRequest(request, out logText);
            WriteToLog(logText);
        }

        private void WriteRequest(byte[] request, out string logText)
        {
            logText = null;
            Connection.Write(request, 0, request.Length, CommUtils.ProtocolLogFormats.Hex, out logText);
        }

        /// <summary>
        /// Считать ответ из последовательного порта
        /// </summary>
        private void ReadAnswer(byte[] buffer, int count, out string logText)
        {
            read_cnt = Connection.Read(buffer, 0, count, ReqParams.Timeout, CommUtils.ProtocolLogFormats.Hex, out logText);
        }

        //-------------------------
        public override void Session()
        {
            base.Session();

            if (!fileyes)        // Если конфигурация не была загружена, выставляем все теги в невалидное состояние и выходим         
            {
                InvalidateCurData();
                return;
            }

            CommSucc = true;
            MyDevice prop = (MyDevice)CommonProps[address];

            // код работает один раз при запуске линии
            if (devTemplate.multicast && !prop.firstFix)
            {
                // Если параметр firstFix = true, то первый КП на линии посылает команду фиксации данных по широковещательному
                // адресу 0xFE в линию, иначе каждый КП посылает команду своему прибору при параметре multicast = false
                prop.firstKp = Number; // в качестве стартового используем номер КП, остальные будут = 0
                for (int x = 0; x < CommonProps.Count; x++)
                {
                    var Val = (MyDevice)CommonProps.ElementAt(x).Value;
                    Val.firstFix = true;
                }
            }

            if (!prop.testcnl)
            {
                Request(requests.testCnlReq, 4);

                if (lastCommSucc)
                {
                    prop.testcnl = true;
                    //code = inBuf[1];
                }
                else
                {
                    // Если тест канала не прошел, больше не опрашиваем
                    CommSucc = false;
                }
            }

            // Открытие канала с уровнем доступа согласно введенного пароля.
            if (prop.testcnl)
            {
                long t2 = Ticks();
                if ((t2 > (prop.tik + 240000)) || !prop.opencnl)
                {
                    // Запрос на открытие канала
                    Request(requests.openCnlReq, 4);

                    if (lastCommSucc) prop.opencnl = true;
                    prop.tik = t2;
                }
            }

            // Определить начало нового дня, возможна синхронизация времени или нет
            if (prop.opencnl && timeSync && DateTime.Today > prop.LastSyncDt)
            {
                DateTime nowDt = DateTime.Now; // запрос времени с секундами, минутами, часами
                bool needSync = false;
                DateTime lastSyncDt = DateTime.MinValue;

                // Чтение времени последней синхронизации счетчика
                Request(requests.lastSyncReq, 16);

                if (lastCommSucc)
                {
                    lastSyncDt = new DateTime(2000 + (int)ConvFunc.BcdToDec(new byte[] { inBuf[6] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[5] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[4] }));
                    prop.LastSyncDt = lastSyncDt;

                    if (DateTime.Today > lastSyncDt)
                    {
                        needSync = true;
                    }
                }

                if (needSync)
                {
                    Request(requests.readTimeReq, 11);
                    if (lastCommSucc)
                    {
                        DateTime readDt = new DateTime(2000 + (int)ConvFunc.BcdToDec(new byte[] { inBuf[7] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[6] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[5] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[3] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[2] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[1] }));

                        // Если время счетчика перешло на новый день
                        if (readDt > DateTime.Today)
                        {
                            if (nowDt.Subtract(readDt).TotalSeconds > Math.Abs(20))
                            {
                                int second = 0;
                                int minutes = 0;
                                int hours = 0;

                                // Часы в счетчике отстают или спешат больше 4-х минут
                                if (nowDt.Subtract(readDt).TotalMinutes > Math.Abs(4))
                                {
                                    if (nowDt.Subtract(readDt).TotalMinutes > 0)
                                    {
                                        readDt = readDt.AddMinutes(3);
                                    }
                                    else
                                    {
                                        readDt = readDt.AddMinutes(-3);
                                    }
                                    second = readDt.Second;
                                    minutes = readDt.Minute;
                                    hours = readDt.Hour;
                                }
                                else
                                {
                                    second = nowDt.Second;
                                    minutes = nowDt.Minute;
                                    hours = nowDt.Hour;
                                }

                                byte sec = (byte)ConvFunc.DecToBCD(second);
                                byte min = (byte)ConvFunc.DecToBCD(minutes);
                                byte hour = (byte)ConvFunc.DecToBCD(hours);
                                Request(Protocol.WriteCompReq(Address, 0x03, 0x0D, new byte[] { sec, min, hour }), 4, string.Format(Localization.UseRussian ? "Команда синхронизация времени" : "Time synchronization command"));
                                if (lastCommSucc)
                                {
                                    WriteToLog(string.Format(Localization.UseRussian ?
                                        "ОК! Команда Выполнена успешно" :
                                        "ОК! Command completed successfully"));
                                }
                                else
                                {
                                    WriteToLog(string.Format(Localization.UseRussian ?
                                        "Ошибка: Команда не выполнена" :
                                        "Error: Command not executed"));
                                }
                            }
                            else
                            {
                                // Если расхождение меньше 20 сек
                                prop.LastSyncDt = nowDt;
                            }
                        }
                    }
                }
            }

            // Запрос информации счетчика - Серийный номер, дата выпуска, версия ПО, вариант исполнения
            if (devTemplate.info && prop.opencnl && !prop.readInfo)
            {
                Request(requests.infoReq, 19);

                if (lastCommSucc)
                {
                    int snNum = 0;
                    int multip = 1;

                    for (int d = 4; d > 0; d--)
                    {
                        int ch = Convert.ToInt32(inBuf[d]);
                        snNum = (ch * multip) + snNum;
                        multip = ch > 99 ? multip = multip * 1000 : multip = multip * 100;
                    }
                    prop.serial = snNum;                                           // Сохраняем серийный номер
                    prop.made = new DateTime(2000 + inBuf[7], inBuf[6], inBuf[5]); // Сохраняем дату изготовления
                    prop.Aconst = ConstA(inBuf[12]);                               // Сохраняем постоянну счетчика имп/квт*ч

                    prop.saveParam.serial = snNum.ToString();
                    prop.saveParam.madeDt = prop.made.ToString("dd.MM.yyyy");
                    prop.saveParam.constA = prop.Aconst.ToString();

                    SaveSettings();
                }
                else
                {
                    // Данные со счетчика не считаны, чтение профилей средней мощности невозможно (заделка на будущее)
                }
                prop.readInfo = true;
            }


            // Запрос коэффициентов трансформации напряжения и тока
            if (prop.opencnl && !prop.Kui)
            {
                Request(requests.kuiReq, 7);

                if (lastCommSucc)
                {
                    prop.Kui = true;
                    prop.ku = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(inBuf, 1), 8));
                    prop.ki = Convert.ToInt32(BitFunc.ROR(BitConverter.ToUInt16(inBuf, 3), 8));
                    prop.parkui = new int[] { prop.ki, prop.ki, prop.ki, 1, prop.ku, prop.ki, 1, 1, prop.ki, prop.ki, prop.ki, prop.ki, prop.ki };
                }
                else
                {
                    string er = string.Concat(CommPhrases.ResponseError, " Недопустимая команда");
                    WriteToLog(er);
                }
            }

            // ------------Получить мгновенные значения P,Q,S,U,I вариант 2
            if (readPQSUI != 0)
            {
                int znx = 1; // начальное положение первого массива байт в ответе
                double znac = 0;

                if (readparam == "14h") // При чтении параметром 16h не нужна фиксация данных
                {
                    if (devTemplate.multicast)
                    {
                        DateTime datetime = DateTime.Now;

                        if (prop.firstKp == Number) // Address
                        {
                            // запись для всех КП времени фиксации и посылка команды фиксации данных
                            for (int x = 0; x < CommonProps.Count; x++)
                            {
                                var Val = (MyDevice)CommonProps.ElementAt(x).Value;
                                Val.dt = datetime;
                            }
                            Write(requests.fixDataReq);
                            Thread.Sleep(ReqParams.Delay);
                        }
                        else
                        {
                            // Тут сравнение времени фиксации и при необходимости отправка команды фиксации данных
                            if (datetime.Subtract(prop.dt).TotalSeconds > fixTime)
                            {
                                Write(requests.fixDataReq);
                                Thread.Sleep(ReqParams.Delay);
                            }
                        }
                    }
                    else // используем команду фиксации по адресу счетчика
                    {
                        Request(requests.fixDataReq, 4);
                        if (lastCommSucc)
                        {
                            // Если работа без широковещательной команды, переводим команду фиксации в true
                            //prop.firstFix = true;
                        }
                    }
                }

                // --------- формирование запросов P,Q,S,U,I и энергия от сброса при параметре 14h или 16h
                for (int f = 0; f < nbwri.Length; f++)
                {
                    int bwrim = nbwri[f] & 0xf0;

                    requests.dataReq = Protocol.DataReq(Address, readparam, nbwri[f]);

                    Request(requests.dataReq, nb_length[f]);

                    if (lastCommSucc)
                    {
                        for (int zn = 0; zn < nparc[f]; zn++)
                        {
                            byte[] zn_temp = new byte[4];
                            uint znac_temp = 0;

                            if (nparb[f] == 4)
                            {
                                Array.Copy(inBuf, znx, zn_temp, 0, nparb[f]);                       // Копирование количества байт nparb[f] во временный буфер
                                znac_temp = BitConverter.ToUInt32(zn_temp, 0);

                                znac_temp = BitFunc.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x00 && (znac_temp & 0x80000000) >= 1) Napr = -1;   // определение направления Активной   мощности
                                if (nbwri[f] == 0x04 && (znac_temp & 0x40000000) >= 1) Napr = -1;   // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffffff;              // наложение маски для удаления направления для получения значения
                            }
                            else
                            {   
                                Array.Copy(inBuf, znx, zn_temp, 0, 1);
                                Array.Copy(inBuf, znx + 1, zn_temp, 2, 2);
                                znac_temp = BitConverter.ToUInt32(zn_temp, 0);

                                znac_temp = BitFunc.ROR(znac_temp, 16);
                                if (nbwri[f] == 0x00 && (znac_temp & 0x800000) >= 1) Napr = -1;   // определение направления Активной   мощности
                                if (nbwri[f] == 0x04 && (znac_temp & 0x400000) >= 1) Napr = -1;   // определение направления Реактивной мощности
                                if (bwrim != 0xf0) znac_temp = znac_temp & 0x3fffff;              // наложение маски для удаления направления для получения значения
                                if (nbwri[f] == 0x30) znac_temp = znac_temp & 0x3ff;              // наложение маски на 3-х байтовую переменную косинуса
                            }

                            if (znac_temp == 0xffffffff && nparb[f] == 4)
                            {
                                InvalidateCurData(tag - 1, 1);
                            }
                            else
                            {
                                //получение значения с учетом разрещшающей способности
                                znac = Convert.ToDouble(znac_temp) / nbwrc[f] * prop.parkui[f];
                                // Значение умножается на множитель, если его нет, то он равен 1
                                SetCurData(tag - 1, znac * Napr * ValPar[tag - 1].range, 1);
                            }

                            znx = znx + nparb[f];
                            tag++;
                            Napr = 1;
                        }
                        znx = 1;
                    }
                    else
                    {
                        prop.opencnl = false;
                        InvalidateCurData(tag - 1, nparc[f]);
                        tag = tag + nparc[f];
                        znx = 1;
                    }
                }
            }

            //------------Получить пофазные значения накопленной энергии прямого направления  код запросв 0x05, параметр 0x60
            if (energyL != 0)
            {
                for (int f = 0; f < nenergy.Length; f++)
                {
                    requests.energyPReq = Protocol.EnergyPReq(Address, nenergy[f]);
                    Request(requests.energyPReq, 15);

                    // Тут проверка ответа на корректность и разбор значений
                    if (lastCommSucc)
                    {
                        int znx = 1;
                        for (int zn = 0; zn < 3; zn++)
                        {
                            uint znac_temp = BitConverter.ToUInt32(inBuf, znx);
                            znac_temp = BitFunc.ROR(znac_temp, 16);
                            double znac = Convert.ToDouble(znac_temp) / 1000 * prop.ki;
                            SetCurData(tag - 1, znac, 1);
                            znx = znx + 4;
                            tag++;
                        }
                    }
                    else
                    {
                        InvalidateCurData(tag - 1, 3);
                        tag = tag + 3;
                    }
                }
            }

            // Чтение профилей мощностей - Вид энергии 0 (A+, A-, R+, R-)
            DateTime dt = DateTime.Now;
            if (dt.Subtract(prop.srezDt) > TimeSpan.FromMinutes(srezPeriod)) // TotalMinuts - 30 заменить на полученное из счетчика
            {
                if (profile.Count > 0)
                {
                    Request(Protocol.WriteCompReq(Address, 0x08, 0x13), 12); // Чтение последней записи среза мощностей

                    int ramstart = 0;
                    if (lastCommSucc)
                    {
                        // -------Определить дату последней записи профиля средних мощностей ---------
                        dt = new DateTime(2000 + (int)ConvFunc.BcdToDec(new byte[] { inBuf[8] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[7] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[6] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[4] }), (int)ConvFunc.BcdToDec(new byte[] { inBuf[5] }), 0);

                        srezPeriod = inBuf[9]; // Проверка периода среза и запись значения в переменную, по умолчанию 30 минут

                        // Последняя ячейка памяти записи средней мощности
                        byte[] NumCell = new byte[2];
                        Array.Copy(inBuf, 1, NumCell, 0, 2);
                        Array.Reverse(NumCell);
                        // Адрес последней ячейки памяти
                        ramstart = (BitConverter.ToUInt16(NumCell, 0) * 16);
                    }
                    else
                    {
                        if (inBuf[1] == 0x05) prop.opencnl = false;
                    }

                    // Пока только Вид энергии 0 (A+, A-, R+, R-)
                    requests.readRomReq = Protocol.ReadRomReq(Address, 0, 3, ramstart, 15); // прочитать последнюю запись, 15 байт
                    Request(requests.readRomReq, 18); // Чтение ROM

                    if (lastCommSucc)
                    {
                        prop.srezDt = dt;
                        DateTime nowDt = DateTime.Now;
                        // Обработка профилей мощности
                        int znx = 8;
                        // Определение статуса среза средниих мощностей false = Архивный (Полный срез), true = Неполный срез, номер задается
                        // параметром шаблона halfArchStat, необходимо предварительно создать Номер и цвет в Проект - Справочники - Типы каналов 
                        bool halfSrez = (inBuf[1] & 0x02) > 0;

                        if (nowDt.Subtract(prop.srezDt) > TimeSpan.FromMinutes(0))
                        {
                            double second = saveTime != 0 ? nowDt.Subtract(prop.srezDt).TotalSeconds : 0;
                            DateTime writeDt = dt;

                            double tme = 0;
                            do
                            {
                                // Формируем новый архивный срез по количеству параметров прибора
                                TagSrez srez = new TagSrez(profile.Count);
                                srez.DateTime = writeDt.AddSeconds(tme);

                                for (int pf = 0; pf < profile.Count; pf++)
                                {
                                    // считаем среднее мощности согласно формуле раздела 2.4
                                    double prof = ((double)BitConverter.ToUInt16(inBuf, znx + profile[pf].offset) * (60 / inBuf[7]) / (2 * prop.Aconst));
                                    int cnlStat = halfSrez ? halfArch : 2;

                                    srez.KPTags[pf] = KPTags[myTags[profile[pf].signal].Index];
                                    srez.TagData[pf] = new SrezTableLight.CnlData(prof * profile[pf].range, cnlStat);
                                    // Запись в текущие промежутки последней записи если время saveTime не равно 0, тогда запись в точку времени среза
                                    if (saveTime != 0) SetCurData(myTags[profile[pf].signal].Index, prof * profile[pf].range, cnlStat);
                                }

                                srez.Descr = "Запись средних мощностей " + nowDt.ToString();
                                AddArcSrez(srez); // Записываем архивный срез в БД RapidScada для текущего прибора

                                tme = tme + saveTime;
                            }
                            while (tme < second);

                            //Сохранить время последнего считанного архива в список сохраняемых параметров
                            prop.saveParam.arcDt = prop.srezDt.ToString(); // TEST
                            SaveSettings();

                            // TEST
                            //WriteToLog("От архивного  "  + prop.saveParam.serial + "  " + prop.saveParam.madeDt + "  " + prop.saveParam.constA + "  " + prop.saveParam.arcDt);

                        }
                    }
                    else
                    {
                        if (inBuf[1] == 0x05) prop.opencnl = false;
                    }
                }
            }


            // У статуса и коэффициентов фиксированные номера сигналов
            // Статус передаем всегда на случай потери связи
            // при потери связи переменная открытия канала сбрасывается
            // Чтение последней записи журнала кода состояния прибора
            if (readstatus) SetCurData(myTags[70].Index, code, 1);

            if (readstatus && prop.opencnl)
            {
                SetCurData(myTags[71].Index, prop.ki, 1);
                SetCurData(myTags[72].Index, prop.ku, 1);

                Request(requests.wordStatReq, 16);
                if (lastCommSucc)
                {
                    Array.Copy(inBuf, 7, wordStat, 4, 2);
                    Array.Copy(inBuf, 9, wordStat, 2, 2);
                    Array.Copy(inBuf, 11, wordStat, 0, 2);
                    SetCurData(myTags[73].Index, BitConverter.ToUInt64(wordStat, 0), 1);
                }
                else
                {
                    InvalidateCurData(myTags[73].Index, 1);
                }
            }

            bool change = chan_err(code);

            if (readstatus && change)
            {
                int even = 2;               // Зеленый цвет события
                if (code != 0) even = 15;   // Красный цвет события
                // генерация события
                KPEvent kpEvent = new KPEvent(DateTime.Now, Number, KPTags[myTags[70].Index]);
                kpEvent.NewData = new SrezTableLight.CnlData(curData[myTags[70].Index].Val, even);
                kpEvent.Descr = ToLogString(code);
                AddEvent(kpEvent);
                CommLineSvc.FlushArcData(this);
                change = false;
                code_err = code;
            }

            tag = 1;

            CommonProps[address] = prop; // записать данные в общие свойства
            CalcSessStats(); // расчёт статистики
        }

        //-------------------------
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);
            lastCommSucc = false;
            byte[] bindata = null;

            MyDevice prop = (MyDevice)CommonProps[address]; // получить данные из общих свойств

            double cmdVal = cmd.CmdVal;
            int cmdNum = cmd.CmdNum;

            if (ActiveCmd.ContainsKey(cmdNum))
            {
                // Определить уровень команды, пользовательский или администратора
                // Если основной уровень 1 - пользователь, а команда с уровнем 2 - Админ, 
                // закрываем канал, открываем с уровнем администратора
                if (uroven != "2" && ActiveCmd[cmdNum].mode == 2)
                {
                    Request(requests.closeCnlReq, 4); // закрытие канала
                    if (lastCommSucc)
                    {
                        // Изменить атрибут открытого канала на false
                        prop.opencnl = false;

                        Request(requests.openAdmReq, 4);
                        if (lastCommSucc)
                        {
                            // Выполнить команду - по сути изменить параметр что канал открыт
                            prop.opencnl = true;
                        }
                    }
                }

                if ((cmd.CmdTypeID == BaseValues.CmdTypes.Standard || cmd.CmdTypeID == BaseValues.CmdTypes.Binary) && prop.opencnl) // Если канал открыт, выполняем команду
                {
                    if (cmd.CmdTypeID == BaseValues.CmdTypes.Standard)
                    {
                        Request(ActiveCmd[cmdNum].setCommand, ActiveCmd[cmdNum].scCnt, string.Format(Localization.UseRussian ? "Отправка команды {0}" : "Sending a command {0}", ActiveCmd[cmdNum].Name));
                        if (lastCommSucc)
                        {
                            WriteToLog(string.Format(Localization.UseRussian ?
                                "ОК! Команда {0} Выполнена успешно" :
                                "ОК! Command {0} completed successfully", ActiveCmd[cmdNum].Name));
                        }
                        else
                        {
                            WriteToLog(string.Format(Localization.UseRussian ?
                                "Ошибка: Команда {0} не выполнена" :
                                "Error: Command {0} not executed", ActiveCmd[cmdNum].Name));
                        }
                    }
                    if (cmd.CmdTypeID == BaseValues.CmdTypes.Binary)
                    {
                        bindata = new byte[cmd.CmdData.Length];
                        bindata = cmd.CmdData;

                        if (bindata.Length == ActiveCmd[cmdNum].datalen)
                        {
                            // Индекс начала блока данных в запросе
                            int startbindat = ActiveCmd[cmdNum].setCommand.Length - bindata.Length - 2;
                            byte[] DataBin = new byte[ActiveCmd[cmdNum].setCommand.Length];
                            Array.Copy(ActiveCmd[cmdNum].setCommand, 0, DataBin, 0, startbindat);
                            Array.Copy(cmd.CmdData, 0, DataBin, startbindat, cmd.CmdData.Length);
                            ushort res = CrcFunc.CalcCRC16(DataBin, DataBin.Length - 2);
                            DataBin[DataBin.Length - 2] = (byte)(res % 256);                // добавить контрольную сумму к буферу посылки
                            DataBin[DataBin.Length - 1] = (byte)(res / 256);

                            Request(DataBin, ActiveCmd[cmdNum].scCnt, string.Format(Localization.UseRussian ? "Отправка команды {0}" : "Sending a command {0}", ActiveCmd[cmdNum].Name));

                            if (lastCommSucc)
                            {
                                WriteToLog(string.Format(Localization.UseRussian ?
                                    "ОК! Команда {0} Выполнена успешно" :
                                    "ОК! Command {0} completed successfully", ActiveCmd[cmdNum].Name));
                            }
                            else
                            {
                                WriteToLog(string.Format(Localization.UseRussian ?
                                    "Ошибка: Команда {0} не выполнена" :
                                    "Error: Command {0} not executed", ActiveCmd[cmdNum].Name));
                            }
                        }
                        else
                        {
                            WriteToLog(string.Format(Localization.UseRussian ?
                                "Ошибка длина блока данных должен быть равна {0}" :
                                "Error the length of the data block must be equal to = {0}", ActiveCmd[cmdNum].datalen));
                        }
                    }
                }
            }

            if (uroven != "2" && ActiveCmd[cmdNum].mode == 2)
            {
                // После выполнения команды Администратором закрываем канал
                // в следующей сессии канал откроется с заданным уровнем доступа
                Request(requests.closeCnlReq, 4); // закрытие канала
                if (lastCommSucc)
                {
                    prop.opencnl = false;
                    CommonProps[address] = prop;
                }
            }
            CalcCmdStats(); // расчёт статистики
        }

        private void Request(byte[] request, int Cnt, string descr = null)
        {
            int tryNum = 0;
            if (CommSucc) // обработчик ошибок, который не связан с ошибками CRC
            {
                lastCommSucc = false;
                while (RequestNeeded(ref tryNum))
                {
                    if (descr != null) WriteToLog(descr); // Если есть описание, выводим в лог
                    string logText;
                    WriteRequest(request, out logText);
                    WriteToLog(logText);

                    ReadAnswer(inBuf, Cnt, out logText);
                    WriteToLog(logText);

                    checkCRC(Cnt);

                    if (!lastCommSucc)
                    {
                        Thread.Sleep(ReqParams.Delay);
                    }
                    FinishRequest();
                    tryNum++;
                }
            }
        }

        private void checkCRC(int count)
        {
            if (read_cnt >= 4)
            {
                ushort crc = CrcFunc.CalcCRC16(inBuf, read_cnt);
                if (crc == 0)
                {
                    if (read_cnt > 4)
                    {
                        code = 0x00;
                        WriteToLog(CommPhrases.ResponseOK);
                    }
                    else if (read_cnt == 4)
                    {
                        if (read_cnt == count && inBuf[1] != 0x00)
                        {
                            code = inBuf[1];
                            WriteToLog(string.Format(Localization.UseRussian ? "Ошибка: {0}" : "Error: {0}", ToLogString(inBuf[1])));
                        }
                        else
                        {
                            code = inBuf[1];
                            WriteToLog(CommPhrases.ResponseOK);
                        }
                    }
                    lastCommSucc = true;
                    CommSucc = true; // TEST
                }
                else WriteToLog(CommPhrases.ResponseCrcError);
            }
            else
            {
                code = 0x09;
                WriteToLog(CommPhrases.ResponseError);
                CommSucc = false; // при отсутствии ответа дальше не запрашивать
                InvalidateCurData();
            }
        }

        private class Requests
        {
            public byte[] testCnlReq;   // запрос тестирования канала
            public byte[] openCnlReq;   // запрос на открытие канала
            public byte[] openAdmReq;   // Запрос открытия канала с Административным паролем
            public byte[] closeCnlReq;  // Запрос на закрытие канала
            public byte[] readTimeReq;  // Запрос чтения времени
            public byte[] lastSyncReq;  // Запрос времени последней синхронизации
            public byte[] fixDataReq;   // запрос на фиксацию данных
            public byte[] dataReq;      // запрос чтения данных
            public byte[] energyPReq;   // запрос на чтение пофазной энергии А+ (0x05 0x60 тариф)
            public byte[] kuiReq;       // запрос значений трансформации тока и напряжения
            public byte[] infoReq;      // Запрос информации счетчика в ускоренном режиме (0x08 0x01) - 16 байт
            public byte[] readRomReq;   // Запрос на чтение информации по физическим адресам памяти (0x06)
            public byte[] curTimeReq;   // Зпапрос текущего времени 2.1 Запросы на чтение массивов времен (код 0x04 параметр 0x00)
            public byte[] wordStatReq;  // Запрос словосостояния счетчика

            public Requests()
            {
            }
        }

        private void tagcreate(TagGroup taggroup, int idgr)
        {
            for (int i = 0; i < devTemplate.SndGroups[idgr].value.Count; i++)
            {
                Allname = string.Concat(devTemplate.SndGroups[idgr].Name, " ", devTemplate.SndGroups[idgr].value[i].name);
                taggroup.KPTags.Add(new KPTag(devTemplate.SndGroups[idgr].value[i].signal, Allname));

                // Добавляем множитель в список тегов, индекс списка множителя равен индексу списка тегов
                ValPar.Add(new ValParam
                {
                    range = string.IsNullOrEmpty(devTemplate.SndGroups[idgr].value[i].range) ? 1 : SToDouble(devTemplate.SndGroups[idgr].value[i].range)
                });
            }
        }

        private double SToDouble(string s)
        {
            double result = 1;
            if (!double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), out result))
            {
                if (!double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out result))
                {
                    return 1;
                }
            }
            return result;
        }

        /// <summary>
        /// Загрузить из файла даты последнего архива и время запроса архивов
        /// </summary>
        private void LoadSettings()
        {
            MyDevice prop = (MyDevice)CommonProps[address];
            // CommLineSvc.Number - Номер линии связи, Number - Номер КП, Address - Адрес прибора
            string fname = AppDirs.StorageDir + "Mercury23x_L" + CommUtils.AddZeros(CommLineSvc.Number, 3) + "_A" + CommUtils.AddZeros(Address, 3) + ".xml";
            try
            {
                prop.saveParam = FileFunc.LoadXml(typeof(SaveParam), fname) as SaveParam;
                //CheckLoad();
                if (prop.saveParam != null) settLoaded = true;
            }
            catch (Exception err)
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                "Не найден файл настроек: " + err.Message :
                "No settings file found: " + err.Message));
            }

            //settLoaded = true;
        }

        /// <summary>
        /// Выполнить действия при завершении работы линии связи
        /// </summary>
        public override void OnCommLineTerminate()
        {
            // если все срезы переданы и загрузка настроек выполнена, сохранить время запроса архивов в файле
            //if (arcSrezList.Count == 0 && settLoaded)
            if (profile.Count > 0)
                SaveSettings();
        }

        /// <summary>
        /// Сохранить в файле время запроса последнего архивов
        /// </summary>
        private void SaveSettings()
        {
            MyDevice prop = (MyDevice)CommonProps[address];
            // CommLineSvc.Number - Номер линии связи, Number - Номер КП, Address - Адрес прибора
            string fname = AppDirs.StorageDir + "Mercury23x_L" + CommUtils.AddZeros(CommLineSvc.Number, 3) + "_A" + CommUtils.AddZeros(Address, 3) + ".xml"; 
            try
            {
                FileFunc.SaveXml(prop.saveParam, fname);
            }
            catch (Exception err)
            {
                WriteToLog(string.Format(Localization.UseRussian ?
                "Ошибка при сохранении настроек в файле: " + err.Message :
                "Error when saving settings in a file: " + err.Message));
            }
        }


    }
}

