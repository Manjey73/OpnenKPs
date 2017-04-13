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

namespace Scada.Comm.Devices.KpMercury20x
{
    public partial class FormSetting : Form
    {
        private int kpNum;                   // номер КП
        private KPView.KPProperties kpProps; // свойства КП, сохраняемые SCADA-Коммуникатором
        private AppDirs appDirs;                // директории приложения
 
        // Сообщение об ошибке
        public string errMsg;

        int mask_par = 0;
        string address = "";
        string sr = ";";
       
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

        private void checkBoxUIP_CheckedChanged_1(object sender, EventArgs e)
        {
            mask_par = Parametrs.SetBit(mask_par, 0, checkBoxUIP.Checked);
            mask_par = Parametrs.SetBit(mask_par, 1, checkBoxPr.Checked);
            mask_par = Parametrs.SetBit(mask_par, 2, checkBoxCOSPp.Checked);
            mask_par = Parametrs.SetBit(mask_par, 3, checkBoxCH.Checked);
            mask_par = Parametrs.SetBit(mask_par, 4, checkBoxEA.Checked);
            mask_par = Parametrs.SetBit(mask_par, 5, checkBoxER.Checked);
            LineCmd.Text = address + sr + Convert.ToString(mask_par) + sr;
        }

        private void FormSetting_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void FormSetting_Load(object sender, EventArgs e)
        {

            string address; // объявление переменной для адреса счетчика
            string[] s_out = new string[3]; // массив введенных значений или значений по умолчанию

            s_out = Parametrs.Parametr(kpProps.CmdLine);
            LineCmd.Text = kpProps.CmdLine;
            kpProps.Modified = false;

            address = s_out[0];
            address_1.Text = address;

            int chanel = Convert.ToInt32(s_out[1], 10);

            checkBoxUIP.Checked   = Parametrs.GetBit(chanel, 0) > 0;
            checkBoxPr.Checked    = Parametrs.GetBit(chanel, 1) > 0;
            checkBoxCOSPp.Checked = Parametrs.GetBit(chanel, 2) > 0;
            checkBoxCH.Checked    = Parametrs.GetBit(chanel, 3) > 0;
            checkBoxEA.Checked    = Parametrs.GetBit(chanel, 4) > 0;
            checkBoxER.Checked    = Parametrs.GetBit(chanel, 5) > 0;

        }

        private void address_TextChanged(object sender, EventArgs e)
        {
            address = address_1.Text;
            LineCmd.Text = address + sr + Convert.ToString(mask_par) + sr;
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

    }
 }
