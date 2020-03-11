namespace Scada.Comm.Devices.KpMercury23x
{
    partial class FormSetting
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LineCmdText = new System.Windows.Forms.TextBox();
            this.textPass = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.btnBrowseDevTemplate = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelPass = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // LineCmdText
            // 
            this.LineCmdText.Location = new System.Drawing.Point(12, 141);
            this.LineCmdText.Name = "LineCmdText";
            this.LineCmdText.Size = new System.Drawing.Size(245, 20);
            this.LineCmdText.TabIndex = 31;
            // 
            // textPass
            // 
            this.textPass.Location = new System.Drawing.Point(12, 104);
            this.textPass.Name = "textPass";
            this.textPass.Size = new System.Drawing.Size(245, 20);
            this.textPass.TabIndex = 29;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(182, 167);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 30;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // btnBrowseDevTemplate
            // 
            this.btnBrowseDevTemplate.Image = global::Scada.Comm.Devices.Properties.Resources.open;
            this.btnBrowseDevTemplate.Location = new System.Drawing.Point(263, 139);
            this.btnBrowseDevTemplate.Name = "btnBrowseDevTemplate";
            this.btnBrowseDevTemplate.Size = new System.Drawing.Size(23, 23);
            this.btnBrowseDevTemplate.TabIndex = 32;
            this.btnBrowseDevTemplate.UseVisualStyleBackColor = true;
            this.btnBrowseDevTemplate.Click += new System.EventHandler(this.btnBrowseDevTemplate_Click);
            // 
            // button1
            // 
            this.button1.Image = global::Scada.Comm.Devices.Properties.Resources.blank;
            this.button1.Location = new System.Drawing.Point(292, 139);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 33;
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Image = global::Scada.Comm.Devices.Properties.Resources.edit;
            this.button2.Location = new System.Drawing.Point(321, 138);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(23, 23);
            this.button2.TabIndex = 34;
            this.button2.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(269, 168);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 35;
            this.btnCancel.Text = "Canсel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelPass
            // 
            this.labelPass.AutoSize = true;
            this.labelPass.Location = new System.Drawing.Point(12, 79);
            this.labelPass.Name = "labelPass";
            this.labelPass.Size = new System.Drawing.Size(89, 13);
            this.labelPass.TabIndex = 36;
            this.labelPass.Text = "Device password";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            // 
            // FormSetting
            // 
            this.ClientSize = new System.Drawing.Size(356, 216);
            this.Controls.Add(this.labelPass);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnBrowseDevTemplate);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textPass);
            this.Controls.Add(this.LineCmdText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Device {0} Properties";
            this.Load += new System.EventHandler(this.FormSetting_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox LineCmdText;
        private System.Windows.Forms.TextBox textPass;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button btnBrowseDevTemplate;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelPass;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
    }
}
