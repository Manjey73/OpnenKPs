namespace Scada.Comm.Devices.KpMercury20x
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
            this.checkBoxCOSPp = new System.Windows.Forms.CheckBox();
            this.checkBoxPr = new System.Windows.Forms.CheckBox();
            this.checkBoxER = new System.Windows.Forms.CheckBox();
            this.checkBoxEA = new System.Windows.Forms.CheckBox();
            this.checkBoxCH = new System.Windows.Forms.CheckBox();
            this.checkBoxUIP = new System.Windows.Forms.CheckBox();
            this.Address = new System.Windows.Forms.Label();
            this.LineCmd = new System.Windows.Forms.TextBox();
            this.address_1 = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // checkBoxCOSPp
            // 
            this.checkBoxCOSPp.AccessibleName = "CheckBox_Group";
            this.checkBoxCOSPp.AutoSize = true;
            this.checkBoxCOSPp.Location = new System.Drawing.Point(12, 107);
            this.checkBoxCOSPp.Name = "checkBoxCOSPp";
            this.checkBoxCOSPp.Size = new System.Drawing.Size(145, 17);
            this.checkBoxCOSPp.TabIndex = 8;
            this.checkBoxCOSPp.Text = "COS, полная мощность";
            this.checkBoxCOSPp.UseVisualStyleBackColor = true;
            this.checkBoxCOSPp.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // checkBoxPr
            // 
            this.checkBoxPr.AccessibleName = "CheckBox_Group";
            this.checkBoxPr.AutoSize = true;
            this.checkBoxPr.Location = new System.Drawing.Point(12, 84);
            this.checkBoxPr.Name = "checkBoxPr";
            this.checkBoxPr.Size = new System.Drawing.Size(141, 17);
            this.checkBoxPr.TabIndex = 3;
            this.checkBoxPr.Text = "Реактивная мощность";
            this.checkBoxPr.UseVisualStyleBackColor = true;
            this.checkBoxPr.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // checkBoxER
            // 
            this.checkBoxER.AccessibleName = "CheckBox_Group";
            this.checkBoxER.AutoSize = true;
            this.checkBoxER.Location = new System.Drawing.Point(12, 176);
            this.checkBoxER.Name = "checkBoxER";
            this.checkBoxER.Size = new System.Drawing.Size(189, 17);
            this.checkBoxER.TabIndex = 7;
            this.checkBoxER.Text = "Энергия реактивная (от сброса)";
            this.checkBoxER.UseVisualStyleBackColor = true;
            this.checkBoxER.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // checkBoxEA
            // 
            this.checkBoxEA.AccessibleName = "CheckBox_Group";
            this.checkBoxEA.AutoSize = true;
            this.checkBoxEA.Location = new System.Drawing.Point(12, 153);
            this.checkBoxEA.Name = "checkBoxEA";
            this.checkBoxEA.Size = new System.Drawing.Size(177, 17);
            this.checkBoxEA.TabIndex = 0;
            this.checkBoxEA.Text = "Энергия активная (от сброса)";
            this.checkBoxEA.UseVisualStyleBackColor = true;
            this.checkBoxEA.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // checkBoxCH
            // 
            this.checkBoxCH.AccessibleName = "CheckBox_Group";
            this.checkBoxCH.AutoSize = true;
            this.checkBoxCH.Location = new System.Drawing.Point(12, 130);
            this.checkBoxCH.Name = "checkBoxCH";
            this.checkBoxCH.Size = new System.Drawing.Size(68, 17);
            this.checkBoxCH.TabIndex = 5;
            this.checkBoxCH.Text = "Частота";
            this.checkBoxCH.UseVisualStyleBackColor = true;
            this.checkBoxCH.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // checkBoxUIP
            // 
            this.checkBoxUIP.AccessibleName = "CheckBox_Group";
            this.checkBoxUIP.AutoSize = true;
            this.checkBoxUIP.Location = new System.Drawing.Point(12, 61);
            this.checkBoxUIP.Name = "checkBoxUIP";
            this.checkBoxUIP.Size = new System.Drawing.Size(221, 17);
            this.checkBoxUIP.TabIndex = 0;
            this.checkBoxUIP.Text = "Напряжение, ток, активная мощность";
            this.checkBoxUIP.UseVisualStyleBackColor = true;
            this.checkBoxUIP.CheckedChanged += new System.EventHandler(this.checkBoxUIP_CheckedChanged_1);
            // 
            // Address
            // 
            this.Address.AutoSize = true;
            this.Address.Location = new System.Drawing.Point(207, 27);
            this.Address.Name = "Address";
            this.Address.Size = new System.Drawing.Size(101, 13);
            this.Address.TabIndex = 10;
            this.Address.Text = "---  Адрес счетчика";
            // 
            // LineCmd
            // 
            this.LineCmd.Location = new System.Drawing.Point(229, 130);
            this.LineCmd.Name = "LineCmd";
            this.LineCmd.Size = new System.Drawing.Size(161, 20);
            this.LineCmd.TabIndex = 12;
            // 
            // address_1
            // 
            this.address_1.Location = new System.Drawing.Point(12, 24);
            this.address_1.Name = "address_1";
            this.address_1.Size = new System.Drawing.Size(189, 20);
            this.address_1.TabIndex = 13;
            this.address_1.TextChanged += new System.EventHandler(this.address_TextChanged);
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(315, 170);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 14;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(226, 111);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Командная строка";
            // 
            // FormSetting
            // 
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.CheckButton;
            this.ClientSize = new System.Drawing.Size(401, 211);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.address_1);
            this.Controls.Add(this.LineCmd);
            this.Controls.Add(this.Address);
            this.Controls.Add(this.checkBoxCOSPp);
            this.Controls.Add(this.checkBoxPr);
            this.Controls.Add(this.checkBoxER);
            this.Controls.Add(this.checkBoxEA);
            this.Controls.Add(this.checkBoxUIP);
            this.Controls.Add(this.checkBoxCH);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Меркурий-20x... Параметры.";
            this.Load += new System.EventHandler(this.FormSetting_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox checkBoxUIP;
        private System.Windows.Forms.CheckBox checkBoxPr;
        private System.Windows.Forms.CheckBox checkBoxER;
        private System.Windows.Forms.CheckBox checkBoxEA;
        private System.Windows.Forms.CheckBox checkBoxCH;
        private System.Windows.Forms.CheckBox checkBoxCOSPp;
        private System.Windows.Forms.Label Address;
        private System.Windows.Forms.TextBox LineCmd;
        private System.Windows.Forms.TextBox address_1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label1;
    }
}
