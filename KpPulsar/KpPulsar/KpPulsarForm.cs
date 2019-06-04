using System;
using System.Windows.Forms;
using System.IO;

namespace Scada.Comm.Devices.KpPulsar
{
    public partial class KpPulsarForm : Form
    {

        private int kpNum;                   // номер КП
        private KPView.KPProperties kpProps; // свойства КП, сохраняемые SCADA-Коммуникатором
        private AppDirs appDirs;             // директории приложения

        // Сообщение об ошибке
        public string errMsg;

        // Конструктор, ограничивающий создание формы без параметров
        private KpPulsarForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Отобразить форму модально
        /// </summary>
        public static void ShowDialog(int kpNum, KPView.KPProperties kpProps, AppDirs appDirs)
        {
            if (kpProps == null)
                throw new ArgumentNullException("kpProps");
            if (appDirs == null)
                throw new ArgumentNullException("appDirs");

            KpPulsarForm PulsarForm = new KpPulsarForm();
            PulsarForm.kpNum = kpNum;
            PulsarForm.kpProps = kpProps;
            PulsarForm.appDirs = appDirs;
            PulsarForm.ShowDialog();
        }


        private void KpPulsarForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void KpPulsarForm_Load(object sender, EventArgs e)
        {

            openFileDialog.Filter = "XML files(*.xml)|*.xml|All files(*.*)|*.*";

            // установка элементов управления в соответствии со свойствами КП
            LineCmdText.Text = kpProps.CmdLine;

            // вывод заголовка
            Text = string.Format(Text, "КП " + kpNum);

            kpProps.Modified = false;

            // настройка элементов управления
            openFileDialog.InitialDirectory = appDirs.ConfigDir;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            kpProps.Modified = true;
            if (kpProps.Modified)
            {
                kpProps.CmdLine = LineCmdText.Text;
            }
            DialogResult = DialogResult.OK;
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
