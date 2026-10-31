namespace TSQLScriptGenerator;

public partial class FormScriptGenerator : Form
{
    private bool _hasRetrievedSqlServers;

    public FormScriptGenerator()
    {
        InitializeComponent();
    }

    private void comboServer_DropDown(object sender, EventArgs e)
    {
        if (_hasRetrievedSqlServers)
        {
            return;
        }

        Cursor = Cursors.WaitCursor;
        try
        {
            var servers = SqlLocator.GetServers();
            if (servers != null)
            {
                comboServer.DataSource = servers;
            }

            _hasRetrievedSqlServers = true;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void comboDatabase_DropDown(object sender, EventArgs e)
    {
        if (comboServer.Text.Equals(""))
        {
            MessageBox.Show("Please specify a SQL Server");
            return;
        }

        Cursor = Cursors.WaitCursor;
        try
        {
            var databases = SqlScriptExporter.ListDatabases(ConnectionOptions());
            comboDatabase.DataSource = databases.ToArray();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
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

        if (!radioButtonIfExists.Checked
            && !radioRawTSQL.Checked
            && !radioCSharpCode.Checked
            && !radioButtonStoredProcedure.Checked
            && !radioCursorCode.Checked)
        {
            MessageBox.Show("I'm not a mind reader how do you want me to process this? Please select a output type");
            return;
        }

        if (textQuery.Text.Equals(""))
        {
            MessageBox.Show("Please enter a Select query or Stored procedure");
            return;
        }

        var needsName = radioButtonIfExists.Checked || radioRawTSQL.Checked || radioButtonStoredProcedure.Checked;
        if (needsName && textBoxOutputtypevalue.Text.Trim().Length == 0)
        {
            MessageBox.Show("You need to enter a name for the table or procedure");
            return;
        }

        var exporter = new SqlScriptExporter(ConnectionOptions());
        var targetName = textBoxOutputtypevalue.Text.Trim();
        var query = textQuery.Text;

        if (radioButtonIfExists.Checked)
        {
            Generate("SQL File|*.sql|Text File|*.txt", "Save a SQL File", path =>
                exporter.ExportInserts(path, query, targetName, ifNotExists: true));
        }
        else if (radioCSharpCode.Checked)
        {
            Generate("C# File|*.cs|Text File|*.txt", "Save a c# File", path =>
                exporter.ExportCSharp(path, query));
        }
        else if (radioButtonStoredProcedure.Checked)
        {
            Generate("SQL File|*.sql|Text File|*.txt", "Save a SQL File", path =>
                exporter.ExportProcedureCalls(path, query, targetName));
        }
        else if (radioRawTSQL.Checked)
        {
            Generate("SQL File|*.sql|Text File|*.txt", "Save a SQL File", path =>
                exporter.ExportInserts(path, query, targetName, ifNotExists: false));
        }
        else if (radioCursorCode.Checked)
        {
            Generate("SQL File|*.sql|Text File|*.txt", "Save a SQL File", path =>
                exporter.ExportCursor(path, query, targetName));
        }
    }

    private void Generate(string filter, string title, Action<string> export)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = filter,
            Title = title,
        };

        if (dialog.ShowDialog() != DialogResult.OK || dialog.FileName.Length == 0)
        {
            return;
        }

        try
        {
            export(dialog.FileName);
            MessageBox.Show("Done");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private SqlConnectionOptions ConnectionOptions()
    {
        return new SqlConnectionOptions
        {
            Server = comboServer.Text,
            Database = comboDatabase.Text,
            TimeoutSeconds = (int)numericTimeout.Value,
            Encrypt = false,
            TrustServerCertificate = true,
        };
    }

    private void radioButtonIfExists_CheckedChanged(object sender, EventArgs e)
    {
        if (radioButtonIfExists.Checked)
        {
            lableoutputname.Text = "Table Name";
        }
    }

    private void radioRawTSQL_CheckedChanged(object sender, EventArgs e)
    {
        if (radioRawTSQL.Checked)
        {
            lableoutputname.Text = "Table Name";
        }
    }

    private void radioCSharpCode_CheckedChanged(object sender, EventArgs e)
    {
        if (radioCSharpCode.Checked)
        {
            lableoutputname.Text = "C# Method Name";
        }
    }

    private void radioButtonStoredProcedure_CheckedChanged(object sender, EventArgs e)
    {
        if (radioButtonStoredProcedure.Checked)
        {
            lableoutputname.Text = "Stored Procedure Name";
        }
    }

    private void radioCursorCode_CheckedChanged(object sender, EventArgs e)
    {
        if (radioCursorCode.Checked)
        {
            lableoutputname.Text = "Table Name";
        }
    }

    private void buttonOpen_Click(object sender, EventArgs e)
    {
        using var openFileDialog1 = new OpenFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Filter = "SQL files (*.SQL)|*.SQL|All files (*.*)|*.*",
            FilterIndex = 2,
            RestoreDirectory = true,
        };

        if (openFileDialog1.ShowDialog() != DialogResult.OK)
        {
            return;
        }

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

    private void buttonSave_Click(object sender, EventArgs e)
    {
        using var saveFileDialog1 = new SaveFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Filter = "SQL files (*.SQL)|*.SQL|All files (*.*)|*.*",
            FilterIndex = 2,
            RestoreDirectory = true,
        };

        if (saveFileDialog1.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(saveFileDialog1.FileName, textQuery.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: Could not write file to disk. Original error: " + ex.Message);
        }
    }
}
