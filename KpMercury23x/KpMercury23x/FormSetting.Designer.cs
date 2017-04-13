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
            this.checkBoxS = new System.Windows.Forms.CheckBox();
            this.checkBoxQ = new System.Windows.Forms.CheckBox();
            this.checkBoxI = new System.Windows.Forms.CheckBox();
            this.checkBoxU = new System.Windows.Forms.CheckBox();
            this.checkBoxCos = new System.Windows.Forms.CheckBox();
            this.checkBoxP = new System.Windows.Forms.CheckBox();
            this.Mgn_znac = new System.Windows.Forms.Label();
            this.checkBoxFU = new System.Windows.Forms.CheckBox();
            this.checkBoxF = new System.Windows.Forms.CheckBox();
            this.checkBoxAR = new System.Windows.Forms.CheckBox();
            this.EnergyAR = new System.Windows.Forms.Label();
            this.checkBoxAR1 = new System.Windows.Forms.CheckBox();
            this.checkBoxAR2 = new System.Windows.Forms.CheckBox();
            this.checkBoxAR3 = new System.Windows.Forms.CheckBox();
            this.checkBoxAR4 = new System.Windows.Forms.CheckBox();
            this.EnergyAplus = new System.Windows.Forms.Label();
            this.checkBoxA4 = new System.Windows.Forms.CheckBox();
            this.checkBoxA3 = new System.Windows.Forms.CheckBox();
            this.checkBoxA2 = new System.Windows.Forms.CheckBox();
            this.checkBoxA1 = new System.Windows.Forms.CheckBox();
            this.checkBoxA = new System.Windows.Forms.CheckBox();
            this.CommandLine = new System.Windows.Forms.Label();
            this.LineCmd = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pass = new System.Windows.Forms.Label();
            this.password1 = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkBoxS
            // 
            this.checkBoxS.AutoSize = true;
            this.checkBoxS.Location = new System.Drawing.Point(12, 83);
            this.checkBoxS.Name = "checkBoxS";
            this.checkBoxS.Size = new System.Drawing.Size(154, 17);
            this.checkBoxS.TabIndex = 8;
            this.checkBoxS.Text = "Мощность S ∑, L1, L2, L3";
            this.checkBoxS.UseVisualStyleBackColor = true;
            this.checkBoxS.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxQ
            // 
            this.checkBoxQ.AutoSize = true;
            this.checkBoxQ.Location = new System.Drawing.Point(12, 60);
            this.checkBoxQ.Name = "checkBoxQ";
            this.checkBoxQ.Size = new System.Drawing.Size(155, 17);
            this.checkBoxQ.TabIndex = 3;
            this.checkBoxQ.Text = "Мощность Q ∑, L1, L2, L3";
            this.checkBoxQ.UseVisualStyleBackColor = true;
            this.checkBoxQ.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxI
            // 
            this.checkBoxI.AutoSize = true;
            this.checkBoxI.Location = new System.Drawing.Point(12, 152);
            this.checkBoxI.Name = "checkBoxI";
            this.checkBoxI.Size = new System.Drawing.Size(96, 17);
            this.checkBoxI.TabIndex = 7;
            this.checkBoxI.Text = "Ток L1, L2, L3";
            this.checkBoxI.UseVisualStyleBackColor = true;
            this.checkBoxI.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxU
            // 
            this.checkBoxU.AutoSize = true;
            this.checkBoxU.Location = new System.Drawing.Point(12, 129);
            this.checkBoxU.Name = "checkBoxU";
            this.checkBoxU.Size = new System.Drawing.Size(141, 17);
            this.checkBoxU.TabIndex = 0;
            this.checkBoxU.Text = "Напряжение L1, L2, L3";
            this.checkBoxU.UseVisualStyleBackColor = true;
            this.checkBoxU.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxCos
            // 
            this.checkBoxCos.AutoSize = true;
            this.checkBoxCos.Location = new System.Drawing.Point(12, 106);
            this.checkBoxCos.Name = "checkBoxCos";
            this.checkBoxCos.Size = new System.Drawing.Size(115, 17);
            this.checkBoxCos.TabIndex = 5;
            this.checkBoxCos.Text = "Cos f ∑, L1, L2, L3";
            this.checkBoxCos.UseVisualStyleBackColor = true;
            this.checkBoxCos.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxP
            // 
            this.checkBoxP.AutoSize = true;
            this.checkBoxP.Location = new System.Drawing.Point(12, 37);
            this.checkBoxP.Name = "checkBoxP";
            this.checkBoxP.Size = new System.Drawing.Size(157, 17);
            this.checkBoxP.TabIndex = 0;
            this.checkBoxP.Text = "Мощность P ∑, L1, L2, L3 ";
            this.checkBoxP.UseVisualStyleBackColor = true;
            this.checkBoxP.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // Mgn_znac
            // 
            this.Mgn_znac.AutoSize = true;
            this.Mgn_znac.Location = new System.Drawing.Point(32, 12);
            this.Mgn_znac.Name = "Mgn_znac";
            this.Mgn_znac.Size = new System.Drawing.Size(121, 13);
            this.Mgn_znac.TabIndex = 10;
            this.Mgn_znac.Text = "Мгновенные значения";
            // 
            // checkBoxFU
            // 
            this.checkBoxFU.AutoSize = true;
            this.checkBoxFU.Location = new System.Drawing.Point(12, 175);
            this.checkBoxFU.Name = "checkBoxFU";
            this.checkBoxFU.Size = new System.Drawing.Size(186, 17);
            this.checkBoxFU.TabIndex = 11;
            this.checkBoxFU.Text = "Угол м-ду ф. L1-L2, L1-L3, L2-L3";
            this.checkBoxFU.UseVisualStyleBackColor = true;
            this.checkBoxFU.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxF
            // 
            this.checkBoxF.AutoSize = true;
            this.checkBoxF.Location = new System.Drawing.Point(12, 198);
            this.checkBoxF.Name = "checkBoxF";
            this.checkBoxF.Size = new System.Drawing.Size(94, 17);
            this.checkBoxF.TabIndex = 12;
            this.checkBoxF.Text = "Частота сети";
            this.checkBoxF.UseVisualStyleBackColor = true;
            this.checkBoxF.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxAR
            // 
            this.checkBoxAR.AutoSize = true;
            this.checkBoxAR.Location = new System.Drawing.Point(226, 37);
            this.checkBoxAR.Name = "checkBoxAR";
            this.checkBoxAR.Size = new System.Drawing.Size(148, 17);
            this.checkBoxAR.TabIndex = 13;
            this.checkBoxAR.Text = "Энергия ∑ А+, А-, R+, R-";
            this.checkBoxAR.UseVisualStyleBackColor = true;
            this.checkBoxAR.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // EnergyAR
            // 
            this.EnergyAR.AutoSize = true;
            this.EnergyAR.Location = new System.Drawing.Point(247, 12);
            this.EnergyAR.Name = "EnergyAR";
            this.EnergyAR.Size = new System.Drawing.Size(102, 13);
            this.EnergyAR.TabIndex = 14;
            this.EnergyAR.Text = "Энергия от сброса";
            // 
            // checkBoxAR1
            // 
            this.checkBoxAR1.AutoSize = true;
            this.checkBoxAR1.Location = new System.Drawing.Point(226, 60);
            this.checkBoxAR1.Name = "checkBoxAR1";
            this.checkBoxAR1.Size = new System.Drawing.Size(143, 17);
            this.checkBoxAR1.TabIndex = 15;
            this.checkBoxAR1.Text = "Тариф 1   А+, А-, R+, R-";
            this.checkBoxAR1.UseVisualStyleBackColor = true;
            this.checkBoxAR1.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxAR2
            // 
            this.checkBoxAR2.AutoSize = true;
            this.checkBoxAR2.Location = new System.Drawing.Point(226, 83);
            this.checkBoxAR2.Name = "checkBoxAR2";
            this.checkBoxAR2.Size = new System.Drawing.Size(143, 17);
            this.checkBoxAR2.TabIndex = 16;
            this.checkBoxAR2.Text = "Тариф 2   А+, А-, R+, R-";
            this.checkBoxAR2.UseVisualStyleBackColor = true;
            this.checkBoxAR2.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxAR3
            // 
            this.checkBoxAR3.AutoSize = true;
            this.checkBoxAR3.Location = new System.Drawing.Point(226, 106);
            this.checkBoxAR3.Name = "checkBoxAR3";
            this.checkBoxAR3.Size = new System.Drawing.Size(143, 17);
            this.checkBoxAR3.TabIndex = 17;
            this.checkBoxAR3.Text = "Тариф 3   А+, А-, R+, R-";
            this.checkBoxAR3.UseVisualStyleBackColor = true;
            this.checkBoxAR3.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxAR4
            // 
            this.checkBoxAR4.AutoSize = true;
            this.checkBoxAR4.Location = new System.Drawing.Point(226, 129);
            this.checkBoxAR4.Name = "checkBoxAR4";
            this.checkBoxAR4.Size = new System.Drawing.Size(143, 17);
            this.checkBoxAR4.TabIndex = 18;
            this.checkBoxAR4.Text = "Тариф 4   А+, А-, R+, R-";
            this.checkBoxAR4.UseVisualStyleBackColor = true;
            this.checkBoxAR4.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // EnergyAplus
            // 
            this.EnergyAplus.AutoSize = true;
            this.EnergyAplus.Location = new System.Drawing.Point(425, 12);
            this.EnergyAplus.Name = "EnergyAplus";
            this.EnergyAplus.Size = new System.Drawing.Size(117, 13);
            this.EnergyAplus.TabIndex = 19;
            this.EnergyAplus.Text = "Энергия А+ по фазам";
            // 
            // checkBoxA4
            // 
            this.checkBoxA4.AutoSize = true;
            this.checkBoxA4.Location = new System.Drawing.Point(407, 129);
            this.checkBoxA4.Name = "checkBoxA4";
            this.checkBoxA4.Size = new System.Drawing.Size(141, 17);
            this.checkBoxA4.TabIndex = 24;
            this.checkBoxA4.Text = "Тариф 4   А+ L1, L2, L3";
            this.checkBoxA4.UseVisualStyleBackColor = true;
            this.checkBoxA4.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxA3
            // 
            this.checkBoxA3.AutoSize = true;
            this.checkBoxA3.Location = new System.Drawing.Point(407, 106);
            this.checkBoxA3.Name = "checkBoxA3";
            this.checkBoxA3.Size = new System.Drawing.Size(141, 17);
            this.checkBoxA3.TabIndex = 23;
            this.checkBoxA3.Text = "Тариф 3   А+ L1, L2, L3";
            this.checkBoxA3.UseVisualStyleBackColor = true;
            this.checkBoxA3.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxA2
            // 
            this.checkBoxA2.AutoSize = true;
            this.checkBoxA2.Location = new System.Drawing.Point(407, 83);
            this.checkBoxA2.Name = "checkBoxA2";
            this.checkBoxA2.Size = new System.Drawing.Size(141, 17);
            this.checkBoxA2.TabIndex = 22;
            this.checkBoxA2.Text = "Тариф 2   А+ L1, L2, L3";
            this.checkBoxA2.UseVisualStyleBackColor = true;
            this.checkBoxA2.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxA1
            // 
            this.checkBoxA1.AutoSize = true;
            this.checkBoxA1.Location = new System.Drawing.Point(407, 60);
            this.checkBoxA1.Name = "checkBoxA1";
            this.checkBoxA1.Size = new System.Drawing.Size(141, 17);
            this.checkBoxA1.TabIndex = 21;
            this.checkBoxA1.Text = "Тариф 1   А+ L1, L2, L3";
            this.checkBoxA1.UseVisualStyleBackColor = true;
            this.checkBoxA1.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // checkBoxA
            // 
            this.checkBoxA.AutoSize = true;
            this.checkBoxA.Location = new System.Drawing.Point(407, 37);
            this.checkBoxA.Name = "checkBoxA";
            this.checkBoxA.Size = new System.Drawing.Size(146, 17);
            this.checkBoxA.TabIndex = 20;
            this.checkBoxA.Text = "Энергия ∑ А+ L1, L2, L3";
            this.checkBoxA.UseVisualStyleBackColor = true;
            this.checkBoxA.CheckedChanged += new System.EventHandler(this.checkBoxP_CheckedChanged_1);
            // 
            // CommandLine
            // 
            this.CommandLine.AutoSize = true;
            this.CommandLine.Location = new System.Drawing.Point(9, 237);
            this.CommandLine.Name = "CommandLine";
            this.CommandLine.Size = new System.Drawing.Size(102, 13);
            this.CommandLine.TabIndex = 25;
            this.CommandLine.Text = "Командная строка";
            // 
            // LineCmd
            // 
            this.LineCmd.Location = new System.Drawing.Point(129, 234);
            this.LineCmd.Name = "LineCmd";
            this.LineCmd.Size = new System.Drawing.Size(195, 20);
            this.LineCmd.TabIndex = 31;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Location = new System.Drawing.Point(204, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1, 203);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            // 
            // pass
            // 
            this.pass.AutoSize = true;
            this.pass.Location = new System.Drawing.Point(223, 175);
            this.pass.Name = "pass";
            this.pass.Size = new System.Drawing.Size(93, 13);
            this.pass.TabIndex = 28;
            this.pass.Text = "Пароль счетчика";
            // 
            // password1
            // 
            this.password1.Location = new System.Drawing.Point(226, 195);
            this.password1.Name = "password1";
            this.password1.Size = new System.Drawing.Size(98, 20);
            this.password1.TabIndex = 29;
            this.password1.TextChanged += new System.EventHandler(this.password_1_TextChanged);
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(441, 231);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 30;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // FormSetting
            // 
            this.ClientSize = new System.Drawing.Size(554, 269);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.password1);
            this.Controls.Add(this.pass);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.LineCmd);
            this.Controls.Add(this.CommandLine);
            this.Controls.Add(this.checkBoxA4);
            this.Controls.Add(this.checkBoxA3);
            this.Controls.Add(this.checkBoxA2);
            this.Controls.Add(this.checkBoxA1);
            this.Controls.Add(this.checkBoxA);
            this.Controls.Add(this.EnergyAplus);
            this.Controls.Add(this.checkBoxAR4);
            this.Controls.Add(this.checkBoxAR3);
            this.Controls.Add(this.checkBoxAR2);
            this.Controls.Add(this.checkBoxAR1);
            this.Controls.Add(this.EnergyAR);
            this.Controls.Add(this.checkBoxAR);
            this.Controls.Add(this.checkBoxF);
            this.Controls.Add(this.checkBoxFU);
            this.Controls.Add(this.Mgn_znac);
            this.Controls.Add(this.checkBoxS);
            this.Controls.Add(this.checkBoxQ);
            this.Controls.Add(this.checkBoxI);
            this.Controls.Add(this.checkBoxU);
            this.Controls.Add(this.checkBoxP);
            this.Controls.Add(this.checkBoxCos);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Меркурий-23x  Параметры.";
            this.Load += new System.EventHandler(this.FormSetting_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox checkBoxP;
        private System.Windows.Forms.CheckBox checkBoxQ;
        private System.Windows.Forms.CheckBox checkBoxI;
        private System.Windows.Forms.CheckBox checkBoxU;
        private System.Windows.Forms.CheckBox checkBoxCos;
        private System.Windows.Forms.CheckBox checkBoxS;
        private System.Windows.Forms.Label Mgn_znac;
        private System.Windows.Forms.CheckBox checkBoxFU;
        private System.Windows.Forms.CheckBox checkBoxF;
        private System.Windows.Forms.CheckBox checkBoxAR;
        private System.Windows.Forms.Label EnergyAR;
        private System.Windows.Forms.CheckBox checkBoxAR1;
        private System.Windows.Forms.CheckBox checkBoxAR2;
        private System.Windows.Forms.CheckBox checkBoxAR3;
        private System.Windows.Forms.CheckBox checkBoxAR4;
        private System.Windows.Forms.Label EnergyAplus;
        private System.Windows.Forms.CheckBox checkBoxA4;
        private System.Windows.Forms.CheckBox checkBoxA3;
        private System.Windows.Forms.CheckBox checkBoxA2;
        private System.Windows.Forms.CheckBox checkBoxA1;
        private System.Windows.Forms.CheckBox checkBoxA;
        private System.Windows.Forms.Label CommandLine;
        private System.Windows.Forms.TextBox LineCmd;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label pass;
        private System.Windows.Forms.TextBox password1;
        private System.Windows.Forms.Button buttonOK;
    }
}
