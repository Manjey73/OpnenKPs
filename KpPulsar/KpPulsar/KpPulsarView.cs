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

using Scada.Comm.Devices.KpPulsar;
using System.Collections.Generic;
using Scada.Data.Configuration;
using ScadaCommFunc;
using System;
using System.IO;
using System.Linq;

namespace Scada.Comm.Devices
{
    public sealed class KpPulsarView : KPView
    {

        private DevTemplate devTemplate = new DevTemplate();
        private DevTemplate.SndGroup sndGroup = new DevTemplate.SndGroup();
        private DevTemplate.CmdGroup cmdGroup = new DevTemplate.CmdGroup();
        private DevTemplate.Value Value = new DevTemplate.Value();
        private DevTemplate.Value.Val val = new DevTemplate.Value.Val();

        private int sigN = 1;
        private bool activeuse = false;

        private Dictionary<string, int> ActiveTagMenu = new Dictionary<string, int>();  // Ключ = Имя меню ValMenu - Значение - количество переменных в данном меню
        private Dictionary<int, string> ActiveTagName = new Dictionary<int, string>();  // Номер Сигнала - Имя переменной
        private Dictionary<int, int> ActiveSnd = new Dictionary<int, int>();            // Ключ = Номер запроса SndCnt - Значение = Индекс Активного запроса SndCnt
        private Dictionary<int, int> ActiveVal = new Dictionary<int, int>();            // Ключ = Номер ответа ValCnt - Значение = Индекс Активного ответа ValCnt

        private class ActiveCnlList
        {
            public int Cnl { get; set; }                // Cnl       - Номер активного сигнала
            public string Name { get; set; }            // Name      - Имя активного сигнала
            public bool cmdActive { get; set; }         // cmdActive - true, у канала есть активная команда  
        }                                              

        private List<ActiveCnlList> ActiveCnl = new List<ActiveCnlList>();              // Создание списка Активных сигналов, где ActiveCnl.Cnl - номер сигнала, ActiveCnl.Name - Имя сигнала, 
                                                                                        // ActiveCnl.Fotmat - Тип активной переменной, ActiveCnl.IdxCtrl - Индекс для ctrlCnl создания прототипов
        public override string KPDescr
        {
            get
            {
                return
                "Автор Бурахин Андрей, email: aburakhin@bk.ru\n\n" +
                "Драйвер протокола Пульсар.\n\n" +
                "Команды ТУ:\n" +
                "Номер канала управления = номеру сигнала\n" +
                "Тип команды - Стандартная, Значение команды - пусто ";
            }
        }
        public KpPulsarView() : this(0)
        {
        }

        public KpPulsarView(int number) : base(number)
        {
            CanShowProps = true;
        }

        // Получить параметры опроса КП по умолчанию
        public override KPReqParams DefaultReqParams
        {
            get
            {
                return new KPReqParams() { Timeout = 1500, Delay = 100 };
            }
        }

        // Отобразить свойства КП
        public override void ShowProps()
        {
            KpPulsarForm.ShowDialog(Number, KPProps, AppDirs); // отображение фармы параметров
        }

        /// <summary>
        /// Gets the channel prototypes.
        /// </summary>
        public override KPCnlPrototypes DefaultCnls
        {
            get
            {
                devTemplate = null;

                KPCnlPrototypes prototypes = new KPCnlPrototypes();
                List<InCnlPrototype> inCnls = prototypes.InCnls;
                List<CtrlCnlPrototype> ctrlCnls = prototypes.CtrlCnls;

                // загрузка шаблона устройства
                string fileName = KPProps == null ? "" : KPProps.CmdLine.Trim();

                if (fileName == "")
                    return null;

                string filePath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(AppDirs.ConfigDir, fileName);

                devTemplate = FileFunc.LoadXml(typeof(DevTemplate), filePath) as DevTemplate;

                try
                {
                    devTemplate = FileFunc.LoadXml(typeof(DevTemplate), filePath) as DevTemplate;
                }
                catch (Exception ex)
                {
                    throw new ScadaException(string.Format(Localization.UseRussian ?
                    "Ошибка при получении типа логики КП из библиотеки {0}" :
                    "Error getting device logic type from the library {0}", ex.Message), ex);
                }

                // Проверка на наличие конфигурации XML
                if (devTemplate != null)
                {
                    // Определить Номера активных запросов.
                    if (devTemplate.SndGroups.Count != 0) // Определить наличие списка запросов, найти активные запросы и записать в массив номера активных запросов для создания тегов по номерам 
                    {
                        for (int snd = 0; snd < devTemplate.SndGroups.Count; snd++) 
                        {
                            if (devTemplate.SndGroups[snd].SndActive) // Если запрос активен, заносим его номер SndCnt в массив
                            {
                                if (!ActiveSnd.ContainsKey(devTemplate.SndGroups[snd].SndCnt))                              // Ключ = SndCnt - Значение = Индекс Активного запроса SndCnt
                                {
                                    ActiveSnd.Add(devTemplate.SndGroups[snd].SndCnt, devTemplate.SndGroups.FindIndex(x => x.SndCnt == devTemplate.SndGroups[snd].SndCnt));

                                    if (!ActiveVal.ContainsKey(devTemplate.SndGroups[snd].SndCnt))                          // Ключ = SndCnt (№ ответа должен быть равен № запроса)
                                    {                                                                                       // Значение = Индекс Ответа Активного запроса SndCnt
                                        ActiveVal.Add(devTemplate.SndGroups[snd].SndCnt, devTemplate.Values.FindIndex(x => x.ValCnt == devTemplate.SndGroups[snd].SndCnt));
                                    }
                                }
                                activeuse = true; // Есть активные запросы
                            }
                        }
                    }

                    if (devTemplate.Values.Count != 0)
                    {
                        if (activeuse)
                        {
                            // ------------------- Сформировать Словарь параметров по меню ------------------ 
                            for (int ac = 0; ac < ActiveSnd.Count; ac++)
                            {
                                int valCnt_ = ActiveVal.Values.ElementAt(ac);                       // Индексы из параллельного словаря Ответов на Активные запросы

                                for (int val = 0; val < devTemplate.Values[valCnt_].Vals.Count; val++)
                                {
                                    if (devTemplate.Values[valCnt_].Vals[val].SigActive)            // Проверяем переменную на активность
                                    {
                                        sigN = devTemplate.Values[valCnt_].Vals[val].SigCnl;        // читаем номер сигнала переменной

                                        // Добавляем в Список переменную для создания прототипов каналов
                                        ActiveCnl.Add(new ActiveCnlList()
                                        {
                                            Cnl = sigN,                                             // Номер текущего активного сигнала
                                            Name = devTemplate.Values[valCnt_].Vals[val].SigName    // Имя текущего активного сигнала
                                        });
                                    }
                                }
                            }
                        }
                    }

                    if (devTemplate.CmdGroups.Count != 0) // Определяем наличие активных команд и заносим в словарь Индексы команд с нулевого значения
                    {
                        for (int cmd = 0; cmd < devTemplate.CmdGroups.Count; cmd++)
                        {
                            if (devTemplate.CmdGroups[cmd].CmdActive)
                            {
                                // Так каналы управления будут последовательны вместе с входными каналами, последовательность
                                // индексов ctrlCnl будет зависеть от расположения веток CmdGroup в шаблоне
                                ActiveCnl.Find(s => s.Cnl == devTemplate.CmdGroups[cmd].CmdCnl).cmdActive = true;
                            }
                        }
                    }
                }

                // ------------ Тут формирование списка каналов управления для Администратора ------------ 
                // output channels

                foreach (var listCmd in ActiveCnl)
                {
                    if (listCmd.cmdActive) // Если у канала есть активная команда, значение cmdActive = true, добавляем ее в список прототипов команд
                    {
                        ctrlCnls.Add(new CtrlCnlPrototype("Уст. " + listCmd.Name, BaseValues.CmdTypes.Standard)
                        {
                            CmdNum = listCmd.Cnl
                        });
                    }
                }

                // ------------ Тут формирование списка входных каналов для Администратора ------------
                // ------------      с учетом подготовленного списка каналов управления        ------------
                // input channels

                foreach (var listCnl in ActiveCnl)
                {                                           // Проверяем наличие команды для канала, если отсутствует, значение индекса = -1
                    int idx = ctrlCnls.FindIndex(x => x.CmdNum == listCnl.Cnl); 
                    inCnls.Add(new InCnlPrototype(listCnl.Name, BaseValues.CnlTypes.TI)
                    { 
                        Signal = listCnl.Cnl,               // номера каналов управления CmdCnl и сигнала SigCnl должны совпадать при формировании шаблона
                        CtrlCnlProps = idx != -1 ? ctrlCnls[idx] : null
                    });                                     // Если индекс найден, добавляем к сигналу связь на команду управления
                }

                // создание прототипов каналов КП
                return prototypes;
            }
        }
    }
}
