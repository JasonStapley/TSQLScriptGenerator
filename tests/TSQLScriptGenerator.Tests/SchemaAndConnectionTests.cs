using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace TSQLScriptGenerator.Tests;

public class SchemaAndConnectionTests
{
    [Fact]
    public void Schema_mapping_skips_hidden_columns_and_honours_boolean_keys()
    {
        var table = SchemaTable();
        Add(table, "Id", "int", isKey: true, allowNull: false, ordinal: 0);
        Add(table, "HiddenKey", "int", isKey: true, allowNull: false, ordinal: 1, hidden: true);
        Add(table, "Note", "nvarchar", isKey: false, allowNull: true, ordinal: 2, size: 50);

        var columns = ColumnSchema.FromSchemaTable(table);

        Assert.Equal(2, columns.Count);
        Assert.Equal("Id", columns[0].ColumnName);
        Assert.True(columns[0].IsKey);
        Assert.False(columns[0].AllowDBNull);
        Assert.Equal("Note", columns[1].ColumnName);
        Assert.Equal(50, columns[1].ColumnSize);
        Assert.Equal(2, columns[1].Ordinal);
    }

    [Fact]
    public void Schema_mapping_accepts_the_string_form_of_IsKey()
    {
        var table = SchemaTable();
        table.Columns["IsKey"]!.DataType = typeof(string);
        var row = table.NewRow();
        row["ColumnName"] = "Id";
        row["DataTypeName"] = "int";
        row["IsKey"] = "True";
        row["AllowDBNull"] = false;
        row["ColumnOrdinal"] = 0;
        row["IsHidden"] = false;
        table.Rows.Add(row);

        var columns = ColumnSchema.FromSchemaTable(table);

        Assert.True(columns[0].IsKey);
    }

    [Fact]
    public void An_unnamed_expression_asks_for_an_alias()
    {
        var table = SchemaTable();
        Add(table, "", "int", isKey: false, allowNull: true, ordinal: 0);

        var error = Assert.Throws<InvalidOperationException>(() => ColumnSchema.FromSchemaTable(table));
        Assert.Contains("Alias every expression", error.Message);
    }

    [Fact]
    public void Connection_string_keeps_the_old_optional_encryption_default()
    {
        var text = SqlConnectionStrings.Build(new SqlConnectionOptions
        {
            Server = "localhost",
            Database = "AdventureWorks",
            TimeoutSeconds = 500,
        });

        var parsed = new SqlConnectionStringBuilder(text);
        Assert.Equal(SqlConnectionEncryptOption.Optional, parsed.Encrypt);
        Assert.True(parsed.TrustServerCertificate);
        Assert.True(parsed.IntegratedSecurity);
        Assert.Equal("localhost", parsed.DataSource);
        Assert.Equal("AdventureWorks", parsed.InitialCatalog);
        Assert.Equal(500, parsed.ConnectTimeout);
        Assert.Equal(SqlConnectionStrings.ApplicationName, parsed.ApplicationName);
    }

    [Fact]
    public void Sql_authentication_turns_integrated_security_off()
    {
        var text = SqlConnectionStrings.Build(new SqlConnectionOptions
        {
            Server = "db.internal",
            Database = "App",
            Encrypt = true,
            TrustServerCertificate = false,
            UserId = "loader",
            Password = "secret",
        });

        var parsed = new SqlConnectionStringBuilder(text);
        Assert.False(parsed.IntegratedSecurity);
        Assert.Equal("loader", parsed.UserID);
        Assert.Equal("secret", parsed.Password);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, parsed.Encrypt);
        Assert.False(parsed.TrustServerCertificate);
    }

    [Fact]
    public void Script_files_are_utf8_with_bom_and_do_not_overwrite_unless_asked()
    {
        var path = Path.Combine(Path.GetTempPath(), "tsqlscript-" + Guid.NewGuid().ToString("N") + ".sql");
        try
        {
            var script = ScriptGenerator.BuildInsertScript(
                "dbo.Person",
                new[] { new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false, IsKey = true } },
                new[] { new object?[] { 1 } });

            using (var writer = ScriptFiles.CreateText(path, overwrite: false))
            {
                writer.Write(script);
            }

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
            Assert.Contains("\r\n", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
            var exists = Assert.Throws<IOException>(() => ScriptFiles.CreateText(path, overwrite: false));
            Assert.Contains("already exists", exists.Message, StringComparison.Ordinal);

            using (var writer = ScriptFiles.CreateText(path, overwrite: true))
            {
                writer.Write("overwritten");
            }

            Assert.Equal("overwritten", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static DataTable SchemaTable()
    {
        var table = new DataTable();
        table.Columns.Add("ColumnName", typeof(string));
        table.Columns.Add("DataTypeName", typeof(string));
        table.Columns.Add("ColumnSize", typeof(int));
        table.Columns.Add("NumericPrecision", typeof(short));
        table.Columns.Add("NumericScale", typeof(short));
        table.Columns.Add("AllowDBNull", typeof(bool));
        table.Columns.Add("IsKey", typeof(bool));
        table.Columns.Add("IsHidden", typeof(bool));
        table.Columns.Add("ColumnOrdinal", typeof(int));
        table.Columns.Add("DataType", typeof(Type));
        return table;
    }

    private static void Add(
        DataTable table,
        string name,
        string type,
        bool isKey,
        bool allowNull,
        int ordinal,
        int? size = null,
        bool hidden = false)
    {
        var row = table.NewRow();
        row["ColumnName"] = name;
        row["DataTypeName"] = type;
        row["IsKey"] = isKey;
        row["AllowDBNull"] = allowNull;
        row["ColumnOrdinal"] = ordinal;
        row["IsHidden"] = hidden;
        if (size is int length)
        {
            row["ColumnSize"] = length;
        }

        table.Rows.Add(row);
    }
}
