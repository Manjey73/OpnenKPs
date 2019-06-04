namespace Scada.Comm.Devices.KpPulsar
{
    partial class KpPulsarForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KpPulsarForm));
            this.LineCmdText = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbDevice = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnCreateDevTemplate = new System.Windows.Forms.Button();
            this.btnBrowseDevTemplate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.gbDevice.SuspendLayout();
            this.SuspendLayout();
            // 
            // LineCmdText
            // 
            this.LineCmdText.Location = new System.Drawing.Point(9, 45);
            this.LineCmdText.Name = "LineCmdText";
            this.LineCmdText.Size = new System.Drawing.Size(185, 20);
            this.LineCmdText.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(144, 207);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(225, 207);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cansel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // gbDevice
            // 
            this.gbDevice.Controls.Add(this.button1);
            this.gbDevice.Controls.Add(this.btnCreateDevTemplate);
            this.gbDevice.Controls.Add(this.btnBrowseDevTemplate);
            this.gbDevice.Controls.Add(this.label1);
            this.gbDevice.Controls.Add(this.LineCmdText);
            this.gbDevice.Location = new System.Drawing.Point(12, 126);
            this.gbDevice.Name = "gbDevice";
            this.gbDevice.Size = new System.Drawing.Size(288, 75);
            this.gbDevice.TabIndex = 3;
            this.gbDevice.TabStop = false;
            this.gbDevice.Text = "Device";
            // 
            // button1
            // 
            this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
            this.button1.Location = new System.Drawing.Point(258, 43);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 5;
            this.button1.UseVisualStyleBackColor = true;
            // 
            // btnCreateDevTemplate
            // 
            this.btnCreateDevTemplate.Image = ((System.Drawing.Image)(resources.GetObject("btnCreateDevTemplate.Image")));
            this.btnCreateDevTemplate.Location = new System.Drawing.Point(229, 43);
            this.btnCreateDevTemplate.Name = "btnCreateDevTemplate";
            this.btnCreateDevTemplate.Size = new System.Drawing.Size(23, 23);
            this.btnCreateDevTemplate.TabIndex = 4;
            this.btnCreateDevTemplate.UseVisualStyleBackColor = true;
            // 
            // btnBrowseDevTemplate
            // 
            this.btnBrowseDevTemplate.Image = ((System.Drawing.Image)(resources.GetObject("btnBrowseDevTemplate.Image")));
            this.btnBrowseDevTemplate.Location = new System.Drawing.Point(200, 43);
            this.btnBrowseDevTemplate.Name = "btnBrowseDevTemplate";
            this.btnBrowseDevTemplate.Size = new System.Drawing.Size(23, 23);
            this.btnBrowseDevTemplate.TabIndex = 3;
            this.btnBrowseDevTemplate.UseVisualStyleBackColor = true;
            this.btnBrowseDevTemplate.Click += new System.EventHandler(this.btnBrowseDevTemplate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Device Template";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            // 
            // KpPulsarTForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(313, 239);
            this.Controls.Add(this.gbDevice);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "KpPulsarTForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Device {0} Properties";
            this.Load += new System.EventHandler(this.KpPulsarForm_Load);
            this.gbDevice.ResumeLayout(false);
            this.gbDevice.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox LineCmdText;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox gbDevice;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnCreateDevTemplate;
        private System.Windows.Forms.Button btnBrowseDevTemplate;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
    }
}
