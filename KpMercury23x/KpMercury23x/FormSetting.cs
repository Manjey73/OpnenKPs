using Scada.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace Scada.Comm.Devices.KpMercury23x
{
    public partial class FormSetting : Form
    {
        private int kpNum;                   // номер КП
        private KPView.KPProperties kpProps; // свойства КП, сохраняемые SCADA-Коммуникатором
        private AppDirs appDirs;             // директории приложения
        private string kpNum_pass; 

        // Конструктор, ограничивающий создание формы без параметров
        private FormSetting()
        {
            InitializeComponent();
            appDirs = null;
        }
        
        /// Отобразить форму модально
        public static void ShowDialog(int kpNum, KPView.KPProperties kpProps, AppDirs appDirs)
        {
            if (kpProps == null)
                throw new ArgumentNullException("kpProps");
            if (appDirs == null)
                throw new ArgumentNullException("appDirs");

            FormSetting FormSetting = new FormSetting();
            FormSetting.kpNum = kpNum;
            FormSetting.kpProps = kpProps;
            FormSetting.appDirs = appDirs;
            FormSetting.ShowDialog();
        }

        private void FormSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            kpProps.Modified = true;
            if (kpProps.Modified)
            {
                kpProps.CmdLine = LineCmdText.Text;

                if (textPass.Text != "")
                {
                    kpProps.CustomParams[kpNum_pass] = textPass.Text;
                }
            }
            DialogResult = DialogResult.OK;
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            openFileDialog.Filter = "XML files(*.xml)|*.xml|All files(*.*)|*.*";

            // установка элементов управления в соответствии со свойствами КП
            LineCmdText.Text = kpProps.CmdLine;
            kpNum_pass = string.Concat(kpNum.ToString(), "_pass");

            if (kpProps.CustomParams.ContainsKey(kpNum_pass)) // Если есть параметр, читаем его значение
            {
                textPass.Text = kpProps.CustomParams.GetStringParam(kpNum_pass, false, "");
            }

            // вывод заголовка
            Text = string.Format(Text, "КП " + kpNum);

            kpProps.Modified = false;


            // настройка элементов управления
            openFileDialog.InitialDirectory = appDirs.ConfigDir;

        }

        private void btnBrowseDevTemplate_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                LineCmdText.Text = Path.GetFileName(openFileDialog.FileName);
            LineCmdText.Select();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            kpProps.Modified = false;
            DialogResult = DialogResult.Cancel;
        }
    }
    
 }
