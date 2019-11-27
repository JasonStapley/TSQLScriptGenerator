namespace TSQLScriptGenerator
{
    partial class FormScriptGenerator
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormScriptGenerator));
            this.label1 = new System.Windows.Forms.Label();
            this.comboServer = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboDatabase = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numericTimeout = new System.Windows.Forms.NumericUpDown();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.textQuery = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.radioButtonStoredProcedure = new System.Windows.Forms.RadioButton();
            this.radioButtonIfExists = new System.Windows.Forms.RadioButton();
            this.radioRawTSQL = new System.Windows.Forms.RadioButton();
            this.buttonGenerateScript = new System.Windows.Forms.Button();
            this.textBoxOutputtypevalue = new System.Windows.Forms.TextBox();
            this.lableoutputname = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.radioCSharpCode = new System.Windows.Forms.RadioButton();
            this.radioCursorCode = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.numericTimeout)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server";
            // 
            // comboServer
            // 
            this.comboServer.FormattingEnabled = true;
            this.comboServer.Location = new System.Drawing.Point(57, 10);
            this.comboServer.Name = "comboServer";
            this.comboServer.Size = new System.Drawing.Size(196, 21);
            this.comboServer.TabIndex = 1;
            this.comboServer.Text = "localhost";
            this.comboServer.DropDown += new System.EventHandler(this.comboServer_DropDown);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(259, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Database";
            // 
            // comboDatabase
            // 
            this.comboDatabase.FormattingEnabled = true;
            this.comboDatabase.Location = new System.Drawing.Point(319, 9);
            this.comboDatabase.Name = "comboDatabase";
            this.comboDatabase.Size = new System.Drawing.Size(240, 21);
            this.comboDatabase.TabIndex = 3;
            this.comboDatabase.DropDown += new System.EventHandler(this.comboDatabase_DropDown);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(565, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Timeout";
            // 
            // numericTimeout
            // 
            this.numericTimeout.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericTimeout.Location = new System.Drawing.Point(617, 10);
            this.numericTimeout.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
            this.numericTimeout.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numericTimeout.Name = "numericTimeout";
            this.numericTimeout.Size = new System.Drawing.Size(83, 20);
            this.numericTimeout.TabIndex = 5;
            this.numericTimeout.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // buttonOpen
            // 
            this.buttonOpen.Location = new System.Drawing.Point(12, 37);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(311, 23);
            this.buttonOpen.TabIndex = 6;
            this.buttonOpen.Text = "&Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(329, 36);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(371, 23);
            this.buttonSave.TabIndex = 7;
            this.buttonSave.Text = "&Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // textQuery
            // 
            this.textQuery.Location = new System.Drawing.Point(16, 94);
            this.textQuery.Multiline = true;
            this.textQuery.Name = "textQuery";
            this.textQuery.Size = new System.Drawing.Size(684, 318);
            this.textQuery.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 424);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Output type";
            // 
            // radioButtonStoredProcedure
            // 
            this.radioButtonStoredProcedure.AutoSize = true;
            this.radioButtonStoredProcedure.Location = new System.Drawing.Point(16, 461);
            this.radioButtonStoredProcedure.Name = "radioButtonStoredProcedure";
            this.radioButtonStoredProcedure.Size = new System.Drawing.Size(232, 17);
            this.radioButtonStoredProcedure.TabIndex = 13;
            this.radioButtonStoredProcedure.TabStop = true;
            this.radioButtonStoredProcedure.Text = "Generate Stored Procedure Calls from query";
            this.radioButtonStoredProcedure.UseVisualStyleBackColor = true;
            this.radioButtonStoredProcedure.CheckedChanged += new System.EventHandler(this.radioButtonStoredProcedure_CheckedChanged);
            // 
            // radioButtonIfExists
            // 
            this.radioButtonIfExists.AutoSize = true;
            this.radioButtonIfExists.Location = new System.Drawing.Point(16, 497);
            this.radioButtonIfExists.Name = "radioButtonIfExists";
            this.radioButtonIfExists.Size = new System.Drawing.Size(270, 17);
            this.radioButtonIfExists.TabIndex = 14;
            this.radioButtonIfExists.TabStop = true;
            this.radioButtonIfExists.Text = "Wrap raw inserts with a if exists Primary key required";
            this.radioButtonIfExists.UseVisualStyleBackColor = true;
            this.radioButtonIfExists.CheckedChanged += new System.EventHandler(this.radioButtonIfExists_CheckedChanged);
            // 
            // radioRawTSQL
            // 
            this.radioRawTSQL.AutoSize = true;
            this.radioRawTSQL.Location = new System.Drawing.Point(16, 535);
            this.radioRawTSQL.Name = "radioRawTSQL";
            this.radioRawTSQL.Size = new System.Drawing.Size(112, 17);
            this.radioRawTSQL.TabIndex = 15;
            this.radioRawTSQL.TabStop = true;
            this.radioRawTSQL.Text = "Raw TSQL Inserts";
            this.radioRawTSQL.UseVisualStyleBackColor = true;
            this.radioRawTSQL.CheckedChanged += new System.EventHandler(this.radioRawTSQL_CheckedChanged);
            // 
            // buttonGenerateScript
            // 
            this.buttonGenerateScript.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.buttonGenerateScript.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGenerateScript.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.buttonGenerateScript.Location = new System.Drawing.Point(16, 663);
            this.buttonGenerateScript.Name = "buttonGenerateScript";
            this.buttonGenerateScript.Size = new System.Drawing.Size(686, 99);
            this.buttonGenerateScript.TabIndex = 16;
            this.buttonGenerateScript.Text = "&Generate Script";
            this.buttonGenerateScript.UseVisualStyleBackColor = false;
            this.buttonGenerateScript.Click += new System.EventHandler(this.buttonGenerateScript_Click);
            // 
            // textBoxOutputtypevalue
            // 
            this.textBoxOutputtypevalue.Location = new System.Drawing.Point(423, 495);
            this.textBoxOutputtypevalue.Name = "textBoxOutputtypevalue";
            this.textBoxOutputtypevalue.Size = new System.Drawing.Size(270, 20);
            this.textBoxOutputtypevalue.TabIndex = 17;
            // 
            // lableoutputname
            // 
            this.lableoutputname.AutoSize = true;
            this.lableoutputname.Location = new System.Drawing.Point(298, 497);
            this.lableoutputname.Name = "lableoutputname";
            this.lableoutputname.Size = new System.Drawing.Size(119, 13);
            this.lableoutputname.TabIndex = 18;
            this.lableoutputname.Text = "Stored Procedure name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 67);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 13);
            this.label5.TabIndex = 19;
            this.label5.Text = "TSQL Query";
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // radioCSharpCode
            // 
            this.radioCSharpCode.AutoSize = true;
            this.radioCSharpCode.Location = new System.Drawing.Point(16, 568);
            this.radioCSharpCode.Name = "radioCSharpCode";
            this.radioCSharpCode.Size = new System.Drawing.Size(106, 17);
            this.radioCSharpCode.TabIndex = 20;
            this.radioCSharpCode.TabStop = true;
            this.radioCSharpCode.Text = "Generate C# Call";
            this.radioCSharpCode.UseVisualStyleBackColor = true;
            this.radioCSharpCode.CheckedChanged += new System.EventHandler(this.radioCSharpCode_CheckedChanged);
            // 
            // radioCursorCode
            // 
            this.radioCursorCode.AutoSize = true;
            this.radioCursorCode.Location = new System.Drawing.Point(16, 603);
            this.radioCursorCode.Name = "radioCursorCode";
            this.radioCursorCode.Size = new System.Drawing.Size(130, 17);
            this.radioCursorCode.TabIndex = 21;
            this.radioCursorCode.TabStop = true;
            this.radioCursorCode.Text = "Generate Cursor Code";
            this.radioCursorCode.UseVisualStyleBackColor = true;
            // 
            // FormScriptGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(712, 774);
            this.Controls.Add(this.radioCursorCode);
            this.Controls.Add(this.radioCSharpCode);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lableoutputname);
            this.Controls.Add(this.textBoxOutputtypevalue);
            this.Controls.Add(this.buttonGenerateScript);
            this.Controls.Add(this.radioRawTSQL);
            this.Controls.Add(this.radioButtonIfExists);
            this.Controls.Add(this.radioButtonStoredProcedure);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textQuery);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonOpen);
            this.Controls.Add(this.numericTimeout);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboDatabase);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboServer);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormScriptGenerator";
            this.Text = "Script Generator";
            ((System.ComponentModel.ISupportInitialize)(this.numericTimeout)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboServer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboDatabase;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericTimeout;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TextBox textQuery;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton radioButtonStoredProcedure;
        private System.Windows.Forms.RadioButton radioButtonIfExists;
        private System.Windows.Forms.RadioButton radioRawTSQL;
        private System.Windows.Forms.Button buttonGenerateScript;
        private System.Windows.Forms.TextBox textBoxOutputtypevalue;
        private System.Windows.Forms.Label lableoutputname;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton radioCSharpCode;
        private System.Windows.Forms.RadioButton radioCursorCode;
    }
}

