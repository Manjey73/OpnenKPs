using Scada.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scada.Comm.Devices.KpRpi3
{
    public partial class FormRPi : Form
    {
        private int kpNum;                   // номер КП
        private KPView.KPProperties kpProps; // свойства КП, сохраняемые SCADA-Коммуникатором
        private AppDirs appDirs;             // директории приложения
        int mask_gpio = 0;
        int inout_gpio = 0;
        int pull_gpio = 0;
        int level_gpio = 0;
        int comand;
        string sr = ";";


        // Конструктор, ограничивающий создание формы без параметров
        public FormRPi()
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

            FormRPi formRpi = new FormRPi();
            formRpi.kpNum = kpNum;
            formRpi.kpProps = kpProps;
            formRpi.appDirs = appDirs;
            formRpi.ShowDialog();
        }

        private void FormKpRPi_Load(object sender, EventArgs e)
        {
            string[] s_out = new string[5]; // массив введенных значений или значений по умолчанию

            s_out = Parametrs.Parametr(kpProps.CmdLine);
            LineCmd.Text = kpProps.CmdLine;
            kpProps.Modified = false;

            int chanel = Convert.ToInt32(s_out[0], 10);
            int in_out = Convert.ToInt32(s_out[1], 10);
            int pullC  = Convert.ToInt32(s_out[2], 10);
            int level  = Convert.ToInt32(s_out[3], 10); 
            int comand = Convert.ToInt32(s_out[4], 10);

            ActivateOut.Checked = Parametrs.GetBit(comand, 0) > 0;
            Retain.Checked = Parametrs.GetBit(comand, 1) > 0;
            this.GpioFormat.SelectedIndex = Parametrs.GetBit(comand, 2);

            for (int i = 4; i < 28; i++)
            {
                (Controls["checkGpio" + i.ToString()] as CheckBox).Checked = Parametrs.GetBit(chanel, i-4) > 0;
            }
            for (int i = 4; i < 28; i++)
            {
                (Controls["inout" + i.ToString()] as CheckBox).Checked = Parametrs.GetBit(in_out, i-4) > 0;
                if ((Controls["inout" + i.ToString()] as CheckBox).Checked)
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["inout" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["inout" + i.ToString()] as CheckBox).Text = "OUT";
                }
                else
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["inout" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["inout" + i.ToString()] as CheckBox).Text = "IN";
                }

            }
            for (int i = 4; i < 28; i++)
            {
                (Controls["pull" + i.ToString()] as CheckBox).Checked = Parametrs.GetBit(pullC, i-4) > 0;
                if ((Controls["pull" + i.ToString()] as CheckBox).Checked)
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["pull" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["pull" + i.ToString()] as CheckBox).Text = "UP";
                }
                else
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["pull" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["pull" + i.ToString()] as CheckBox).Text = "Down";
                }
            }

            for (int i = 4; i < 28; i++)
            {
                (Controls["level" + i.ToString()] as CheckBox).Checked = Parametrs.GetBit(level, i-4) > 0;
                if ((Controls["level" + i.ToString()] as CheckBox).Checked)
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["level" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["level" + i.ToString()] as CheckBox).Text = "High";
                }
                else
                {
                    if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked) (Controls["level" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["level" + i.ToString()] as CheckBox).Text = "Low";
                }
            }
        }

        private void Gpio_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 4; i < 28; i++)
            {
                mask_gpio = Parametrs.SetBit(mask_gpio, i-4, (Controls["checkGpio" + i.ToString()] as CheckBox).Checked);
                if (!(Controls["checkGpio" + i.ToString()] as CheckBox).Checked)
                {
                    (Controls["inout" + i.ToString()] as CheckBox).Enabled = false;
                    (Controls["pull" + i.ToString()]  as CheckBox).Enabled = false;
                    (Controls["level" + i.ToString()] as CheckBox).Enabled = false;
                }
                else
                {
                    (Controls["inout" + i.ToString()] as CheckBox).Enabled = true;
                    (Controls["pull" + i.ToString()]  as CheckBox).Enabled = true;
                    (Controls["level" + i.ToString()] as CheckBox).Enabled = true;
                }
            }
            LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            kpProps.Modified = true;
            if (kpProps.Modified)
            {
                kpProps.CmdLine = LineCmd.Text;
            }
            DialogResult = DialogResult.OK;
        }

        private void INOUT_checkedChanged(object sender, EventArgs e)
        {

            for (int i = 4; i < 28; i++)
            {
                inout_gpio = Parametrs.SetBit(inout_gpio, i-4, (Controls["inout" + i.ToString()] as CheckBox).Checked);
                if ((Controls["inout" + i.ToString()] as CheckBox).Checked)
                {
                    (Controls["inout" + i.ToString()] as CheckBox).Text = "OUT";
                }
                else
                {
                    (Controls["inout" + i.ToString()] as CheckBox).Text = "IN";
                }
            }
            LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;
        }

        private void pullControl_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 4; i < 28; i++)
            {
                pull_gpio = Parametrs.SetBit(pull_gpio, i-4, (Controls["pull" + i.ToString()] as CheckBox).Checked);
                if ((Controls["pull" + i.ToString()] as CheckBox).Checked)
                {
                    (Controls["pull" + i.ToString()] as CheckBox).Text = "UP";
                }
                else
                {
                    (Controls["pull" + i.ToString()] as CheckBox).Text = "Down";
                }
            }
            LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;

        }

        private void level_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 4; i < 28; i++)
            {

                level_gpio = Parametrs.SetBit(level_gpio, i-4, (Controls["level" + i.ToString()] as CheckBox).Checked);
                if ((Controls["level" + i.ToString()] as CheckBox).Checked)
                {
                (Controls["level" + i.ToString()] as CheckBox).Text = "High";
                }
                else
                {
                (Controls["level" + i.ToString()] as CheckBox).Text = "Low";
                }
            }
        LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;
        }


        private void Comand(object sender, EventArgs e)
        {
            comand = Parametrs.SetBit(comand, 0, ActivateOut.Checked);
            comand = Parametrs.SetBit(comand, 1, Retain.Checked);
            LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;

        }

        private void FormRPi_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void GpioFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            comand = Parametrs.SetBit(comand, 2, Convert.ToBoolean(GpioFormat.SelectedIndex));
            LineCmd.Text = Convert.ToString(mask_gpio) + sr + Convert.ToString(inout_gpio) + sr + Convert.ToString(pull_gpio) + sr + Convert.ToString(level_gpio) + sr + Convert.ToString(comand) + sr;
        }
    }
}
