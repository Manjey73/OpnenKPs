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
        private AppDirs appDirs;                // директории приложения

        // Сообщение об ошибке
        public string errMsg;

        int mask_cnl = 0;
        string sr = ";";
        string password = ""; // объявление переменной для адреса счетчика

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

        private void checkBoxP_CheckedChanged_1(object sender, EventArgs e)
        {
            mask_cnl = Parametrs.SetBit(mask_cnl, 0, checkBoxP.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 1, checkBoxQ.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 2, checkBoxS.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 3, checkBoxCos.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 4, checkBoxU.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 5, checkBoxI.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 6, checkBoxFU.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 7, checkBoxF.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 8, checkBoxAR.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 9, checkBoxAR1.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 10, checkBoxAR2.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 11, checkBoxAR3.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 12, checkBoxAR4.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 13, checkBoxA.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 14, checkBoxA1.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 15, checkBoxA2.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 16, checkBoxA3.Checked);
            mask_cnl = Parametrs.SetBit(mask_cnl, 17, checkBoxA4.Checked);
            LineCmd.Text = password + sr + Convert.ToString(mask_cnl) + sr;
        }

        private void FormSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void password_1_TextChanged(object sender, EventArgs e)
        {
            password = password1.Text;
            LineCmd.Text = password + sr + Convert.ToString(mask_cnl) + sr;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            kpProps.Modified = true;
            if (kpProps.Modified)
            {
                kpProps.CmdLine = LineCmd.Text;
            }
            DialogResult = DialogResult.OK;
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            string password; // объявление переменной для пароля счетчика
            string[] s_out = new string[4]; // массив введенных значений или значений по умолчанию

            s_out = Parametrs.Parametr(kpProps.CmdLine);
            LineCmd.Text = kpProps.CmdLine;
            kpProps.Modified = false;

            password = s_out[0];
            password1.Text = password;

            int cnl = Convert.ToInt32(s_out[1], 10);
            checkBoxP.Checked = Parametrs.GetBit(cnl, 0) > 0;
            checkBoxQ.Checked = Parametrs.GetBit(cnl, 1) > 0;
            checkBoxS.Checked = Parametrs.GetBit(cnl, 2) > 0;
            checkBoxCos.Checked = Parametrs.GetBit(cnl, 3) > 0;
            checkBoxU.Checked = Parametrs.GetBit(cnl, 4) > 0;
            checkBoxI.Checked = Parametrs.GetBit(cnl, 5) > 0;
            checkBoxFU.Checked = Parametrs.GetBit(cnl, 6) > 0;
            checkBoxF.Checked = Parametrs.GetBit(cnl, 7) > 0;
            checkBoxAR.Checked = Parametrs.GetBit(cnl, 8) > 0;
            checkBoxAR1.Checked = Parametrs.GetBit(cnl, 9) > 0;
            checkBoxAR2.Checked = Parametrs.GetBit(cnl, 10) > 0;
            checkBoxAR3.Checked = Parametrs.GetBit(cnl, 11) > 0;
            checkBoxAR4.Checked = Parametrs.GetBit(cnl, 12) > 0;
            checkBoxA.Checked = Parametrs.GetBit(cnl, 13) > 0;
            checkBoxA1.Checked = Parametrs.GetBit(cnl, 14) > 0;
            checkBoxA2.Checked = Parametrs.GetBit(cnl, 15) > 0;
            checkBoxA3.Checked = Parametrs.GetBit(cnl, 16) > 0;
            checkBoxA4.Checked = Parametrs.GetBit(cnl, 17) > 0;

        }
    }

    
 }
