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


using Scada.Comm.Devices.KpMercury23x;
using Scada.Data;
using System;
using System.IO;
using System.Collections.Generic;
using ScadaCommFunc;
using Scada.Data.Configuration;
using System.Text;

namespace Scada.Comm.Devices
{
    public sealed class KpMercury23xView : KPView
    {
        private DevTemplate devTemplate = new DevTemplate();

        private class ActiveCnlList
        {
            public int Cnl { get; set; }                // Cnl       - Номер активного сигнала
            public string Name { get; set; }            // Name      - Имя активного сигнала
            public bool cmdActive { get; set; }         // cmdActive - true, у канала есть активная команда
            public int idGroup { get; set; }         // Индекс группы, которой принадлежит Значение
        }

        private List<ActiveCnlList> ActiveCnl = new List<ActiveCnlList>(); // Создание списка Активных сигналов, где ActiveCnl.Cnl - номер сигнала, ActiveCnl.Name - Имя сигнала, 
        private string Allname;         // Полное имя тега
        private bool activeuse = false;


        public override string KPDescr
        {
            get
            {
                return
                "Автор Бурахин Андрей, email: aburakhin@bk.ru\n\n" +
                "Опрос электросчётчика Меркурий (230, 231, 232, 233, 236).\n\n" +
                "Адрес счетчика  = Администратор -> КП -> Адрес\n\n" +

                "Параметры DevTamplate:\npassword - пароль счетчика (asccii символы)\nreadparam - '14h' или '16h'\n" +
                "mode - '1' или '2'\nfixtime - время в минутах сравнения переданной команды фиксации данных\n" +
                "можно вводить в дробном виде для секунд 0,2 или 0.3\n" +
                "multicast = true - широковещательная команда фиксации данных\n" +
                "info = true - прочитать данные счетчика, SN, дату производства, постоянную счтечика\n\n" +
                "Параметры SndGroups - SndRequest:\n" +

                "Name - общее имя группы параметров\n" +
                "Active - активность чтения группы параметров\n" +
                "Bit - Номер бита для группы параметров (см. ниже)\n\n" +
                "Список параметров value:\n" +
                "name - имя обособленного параметра \n" +
                "signal - номер сигнала для параметра \n" +
                "active - (*) активнось параметра \n" +
                "range  - (*) множитель параметра \n" +
                "(*) - зарезервировано\n\n" +

                "    Мгновенные значения:\n\n" +
                "bit 0 - Мощность P ∑, L1, L2, L3\n" +
                "bit 1 - Мощность Q ∑, L1, L2, L3\n" +
                "bit 2 - Мощность S ∑, L1, L2, L3\n" +
                "bit 3 - Cos f ∑, L1, L2, L3\n" +
                "bit 4 - Напряжение L1, L2, L3\n" +
                "bit 5 - Ток L1, L2, L3\n" +
                "bit 6 - Угол м-ду ф. L1-L2, L1-L3, L2-L3\n" +
                "bit 7 - Частота сети\n\n" +
                "    Энергия от сброса:\n\n" +
                "bit 8  - Энергия ∑ А+, А-, R+, R-\n" +
                "bit 9  - Тариф 1   А+, А-, R+, R-\n" +
                "bit 10 - Тариф 2   А+, А-, R+, R-\n" +
                "bit 11 - Тариф 3   А+, А-, R+, R-\n" +
                "bit 12 - Тариф 4   А+, А-, R+, R-\n" +
                "bit 13 - Энергия ∑ А+ L1, L2, L3\n" +
                "bit 14 - Тариф 1   А+ L1, L2, L3\n" +
                "bit 15 - Тариф 2   А+ L1, L2, L3\n" +
                "bit 16 - Тариф 3   А+ L1, L2, L3\n" +
                "bit 17 - Тариф 4   А+ L1, L2, L3";
            }
        }

        public KpMercury23xView() : this(0)
        {
        }

        public KpMercury23xView(int number) : base(number)
        {
            CanShowProps = true;
        }

        // Получить параметры опроса КП по умолчанию
        public override KPReqParams DefaultReqParams
        {
            get
            {
                return new KPReqParams() { Timeout = 1000, Delay = 200 };
            }
        }

        // Отобразить свойства КП
        public override void ShowProps()
        {
            FormSetting.ShowDialog(Number, KPProps, AppDirs); // отображение фармы параметров
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
                    ActiveCnl.Clear(); // Очищаем список, так как код срабатывает при выборе КП при Создании каналов каждый раз...

                    if (devTemplate.SndGroups.Count != 0) // Определить активные запросы объектов и записать в список индексы запросов для создания тегов
                    {
                        for (int sg = 0; sg < devTemplate.SndGroups.Count; sg++)
                        {
                            if (devTemplate.SndGroups[sg].Active)
                            {
                                if (devTemplate.SndGroups[sg].value.Count > 0)
                                {
                                    for (int y = 0; y < devTemplate.SndGroups[sg].value.Count; y++)
                                    {
                                        if (devTemplate.SndGroups[sg].value[y].active)
                                        {
                                            Allname = string.Concat(devTemplate.SndGroups[sg].Name, " ", devTemplate.SndGroups[sg].value[y].name);
                                            ActiveCnl.Add(new ActiveCnlList()
                                            {
                                                Cnl = devTemplate.SndGroups[sg].value[y].signal,
                                                Name = Allname,
                                                idGroup = devTemplate.SndGroups[sg].Bit // ID Группы является значение Bit
                                            });
                                            activeuse = true; // Есть активные группы запросов значений
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (activeuse)
                    {
                        ActiveCnl.Add(new ActiveCnlList()
                        {
                            Cnl = 70,
                            Name = "Код ошибки:"
                        });

                        ActiveCnl.Add(new ActiveCnlList()
                        {
                            Cnl = 71,
                            Name = "Коэфф. тр-ра тока"
                        });

                        ActiveCnl.Add(new ActiveCnlList()
                        {
                            Cnl = 72,
                            Name = "Коэфф. тр-ра напряжения"
                        });

                        // ------------ Тут формирование списка каналов управления для Администратора ------------ ЗАРЕЗЕРВИРОВАНО
                        // output channels
                        //foreach (var listCmd in ActiveCnl)
                        //{
                        //    if (listCmd.cmdActive) // Если у канала есть активная команда, значение cmdActive = true, добавляем ее в список прототипов команд
                        //    {
                        //        ctrlCnls.Add(new CtrlCnlPrototype("Уст. " + listCmd.Name, BaseValues.CmdTypes.Standard)
                        //        {
                        //            CmdNum = listCmd.Cnl
                        //        });
                        //    }
                        //}

                        // ------------ Тут формирование списка входных каналов для Администратора ------------
                        // ------------      с учетом подготовленного списка каналов управления        ------------
                        // input channels

                        foreach (var listCnl in ActiveCnl)
                        {
                            int idx = ctrlCnls.FindIndex(x => x.CmdNum == listCnl.Cnl);
                            inCnls.Add(new InCnlPrototype(listCnl.Name, BaseValues.CnlTypes.TI)
                            {
                                Signal = listCnl.Cnl,               // номера каналов управления CmdCnl и сигнала SigCnl должны совпадать при формировании шаблона
                                CtrlCnlProps = idx != -1 ? ctrlCnls[idx] : null
                            });                                     // Если индекс найден, добавляем к сигналу связь на команду управления
                        }

                    }

                }
                // создание прототипов каналов КП
                return prototypes;
            }
        }
    }
}
