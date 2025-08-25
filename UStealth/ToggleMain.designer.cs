namespace UStealth
{
    partial class ToggleMain
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToggleMain));
            dg1 = new System.Windows.Forms.DataGridView();
            button1 = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            textBox1 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)dg1).BeginInit();
            SuspendLayout();
            // 
            // dg1
            // 
            dg1.AllowUserToAddRows = false;
            dg1.AllowUserToDeleteRows = false;
            dg1.AllowUserToOrderColumns = true;
            dg1.AllowUserToResizeRows = false;
            dg1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dg1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            dg1.Location = new System.Drawing.Point(16, 82);
            dg1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dg1.MultiSelect = false;
            dg1.Name = "dg1";
            dg1.RowHeadersWidth = 51;
            dg1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dg1.Size = new System.Drawing.Size(824, 231);
            dg1.TabIndex = 4;
            dg1.CellDoubleClick += dg1_CellDoubleClick;
            // 
            // button1
            // 
            button1.Image = Properties.Resources.refresh;
            button1.Location = new System.Drawing.Point(797, 322);
            button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(43, 45);
            button1.TabIndex = 5;
            toolTip1.SetToolTip(button1, "Refresh");
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            textBox1.Font = new System.Drawing.Font("Calibri", 11F);
            textBox1.Location = new System.Drawing.Point(16, 17);
            textBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new System.Drawing.Size(824, 55);
            textBox1.TabIndex = 6;
            textBox1.TabStop = false;
            textBox1.Text = "Double click the drive to hide/show.  Note that the system and unrecognized drives won't be touched for obvious reasons.  Use this utility at your own risk!  Always backup!";
            // 
            // ToggleMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(856, 372);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Controls.Add(dg1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "ToggleMain";
            Text = "U-Stealth";
            ((System.ComponentModel.ISupportInitialize)dg1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dg1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox textBox1;
    }
}