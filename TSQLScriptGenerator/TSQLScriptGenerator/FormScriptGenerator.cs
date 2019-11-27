using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSQLScriptGenerator
{
    public partial class FormScriptGenerator : Form
    {
        private bool HasRetrievedSQLServers = false;

        public FormScriptGenerator()
        {
            InitializeComponent();
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void comboServer_DropDown(object sender, EventArgs e)
        {
            if (!HasRetrievedSQLServers)
            {

                this.Cursor = Cursors.WaitCursor;

                string[] theAvailableSqlServers = SqlLocator.GetServers();
                if (theAvailableSqlServers != null)
                {
                    comboServer.DataSource = theAvailableSqlServers;
                }

                HasRetrievedSQLServers = true;
                this.Cursor = Cursors.Default;

            }
        }

        private void comboDatabase_DropDown(object sender, EventArgs e)
        {
            if (comboServer.Text.Equals(""))
            {
                MessageBox.Show("Please specify a SQL Server");
            }
            else
            {
                this.Cursor = Cursors.WaitCursor;

                string[] theAvailabledatabases = SqlLocator.GetDatabases(comboServer.Text);
                if (theAvailabledatabases != null)
                {
                    comboDatabase.DataSource = theAvailabledatabases;
                }                
                this.Cursor = Cursors.Default;
            }            
        }

        private void buttonGenerateScript_Click(object sender, EventArgs e)
        {
            if (comboServer.Text.Equals(""))
            {
                MessageBox.Show("You need to enter a SQL Server");
                return;
            }

            if (comboDatabase.Text.Equals(""))
            {
                MessageBox.Show("You need to enter a Database");
                return;
            }

            if (radioButtonIfExists.Checked == false &&
                radioRawTSQL.Checked == false &&
                radioCSharpCode.Checked == false &&
                radioButtonStoredProcedure.Checked == false &&
                radioCursorCode.Checked == false
                )
            {
                MessageBox.Show("I'm not a mind reader how do you want me to process this? Please select a output type");
                return;
            }

            if(textQuery.Text.Equals(""))
            {
                MessageBox.Show("Please enter a Select query or Stored procedure");
                return;
            }

            if (radioButtonIfExists.Checked == true)
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "SQL File|*.sql|Text File|*.txt";
                saveFileDialog1.Title = "Save a SQL File";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                    GenerateTSQL.ExportMsSQLToScriptWithIfExists(comboServer.Text, comboDatabase.Text, saveFileDialog1.FileName, textQuery.Text, textBoxOutputtypevalue.Text, numericTimeout.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    MessageBox.Show("Done");
                }   
               
            }

            if (radioCSharpCode.Checked == true)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "C# File|*.cs|Text File|*.txt";
                saveFileDialog1.Title = "Save a c# File";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                        GenerateTSQL.ExportCSharpCall(comboServer.Text, comboDatabase.Text, saveFileDialog1.FileName, textQuery.Text, textBoxOutputtypevalue.Text, numericTimeout.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    MessageBox.Show("Done");
                }
            }

            if (radioButtonStoredProcedure.Checked == true)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "SQL File|*.sql|Text File|*.txt";
                saveFileDialog1.Title = "Save a SQL File";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                        GenerateTSQL.ExportQueryToStoredProcedureCalls(comboServer.Text, comboDatabase.Text, saveFileDialog1.FileName, textQuery.Text, textBoxOutputtypevalue.Text, numericTimeout.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    MessageBox.Show("Done");
                }
            }

            if (radioRawTSQL.Checked == true)
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "SQL File|*.sql|Text File|*.txt";
                saveFileDialog1.Title = "Save a SQL File";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                        GenerateTSQL.ExportMsSQLToScript(comboServer.Text, comboDatabase.Text, saveFileDialog1.FileName, textQuery.Text, textBoxOutputtypevalue.Text, numericTimeout.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    MessageBox.Show("Done");
                }
            }

            if (radioCursorCode.Checked == true)
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "SQL File|*.sql|Text File|*.txt";
                saveFileDialog1.Title = "Save a SQL File";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                        GenerateTSQL.ExportCursor(comboServer.Text, comboDatabase.Text, saveFileDialog1.FileName, textQuery.Text, textBoxOutputtypevalue.Text, numericTimeout.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    MessageBox.Show("Done");
                }
            }
        }

        private void radioButtonIfExists_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonIfExists.Checked == true)
            {
                lableoutputname.Text = "Table Name";
            }
        }

        private void radioRawTSQL_CheckedChanged(object sender, EventArgs e)
        {
            if (radioRawTSQL.Checked == true)
            {
                lableoutputname.Text = "Table Name";
            }
        }

        private void radioCSharpCode_CheckedChanged(object sender, EventArgs e)
        {
            if (radioCSharpCode.Checked == true)
            {
                lableoutputname.Text = "C# Method Name";
            }
        }

        private void radioButtonStoredProcedure_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonStoredProcedure.Checked == true)
            {
                lableoutputname.Text = "Stored Procedure Name";
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {            
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Filter = "SQL files (*.SQL)|*.SQL|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (File.Exists(openFileDialog1.FileName))
                    {              
                        textQuery.Text = File.ReadAllText(openFileDialog1.FileName);
                    }                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            saveFileDialog1.Filter = "SQL files (*.SQL)|*.SQL|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {                    
                    File.WriteAllText(saveFileDialog1.FileName,textQuery.Text );                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not write file to disk. Original error: " + ex.Message);
                }
            }
        }
    }
}
