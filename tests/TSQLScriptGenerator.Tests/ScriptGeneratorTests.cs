namespace TSQLScriptGenerator.Tests;

public class ScriptGeneratorTests
{
    [Fact]
    public void Create_table_quotes_names_and_emits_a_primary_key_when_IsKey_is_set()
    {
        var sql = ScriptGenerator.BuildCreateTable("dbo.Orders", OrderColumns());

        Assert.Equal(
            "IF OBJECT_ID(N'[dbo].[Orders]') IS NULL\r\n" +
            "BEGIN\r\n" +
            "CREATE TABLE [dbo].[Orders]([Id] int NOT NULL,[Note] nvarchar(100) NULL,[Payload] varbinary(max) NULL,[Amount] decimal(10,2) NOT NULL\r\n" +
            ");\r\n" +
            "ALTER TABLE [dbo].[Orders]\r\n" +
            "ADD PRIMARY KEY\r\n" +
            "([Id]\r\n" +
            ");\r\n" +
            "END",
            sql);
    }

    [Fact]
    public void Create_table_without_a_key_leaves_the_constraint_off()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Note", DataTypeName = "varchar", ColumnSize = 20, AllowDBNull = true },
        };

        var sql = ScriptGenerator.BuildCreateTable("Note", columns);

        Assert.Contains("CREATE TABLE [Note]([Note] varchar(20) NULL\r\n);\r\n\r\nEND", sql);
        Assert.DoesNotContain("PRIMARY KEY", sql);
    }

    [Fact]
    public void Insert_script_keeps_one_batch_per_row()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false, IsKey = true },
            new ColumnSchema { ColumnName = "Note", DataTypeName = "nvarchar", ColumnSize = 100, AllowDBNull = true },
        };
        var rows = new object?[][]
        {
            new object?[] { 1, "O'Brien" },
            new object?[] { 2, null },
        };

        var sql = ScriptGenerator.BuildInsertScript("dbo.Orders", columns, rows);

        Assert.Contains("INSERT INTO [dbo].[Orders]([Id],[Note])\r\nVALUES(1,N'O''Brien')\r\nGO\r\n", sql);
        Assert.Contains("INSERT INTO [dbo].[Orders]([Id],[Note])\r\nVALUES(2,NULL)\r\nGO\r\n", sql);
        Assert.StartsWith("IF OBJECT_ID(N'[dbo].[Orders]') IS NULL\r\n", sql);
    }

    [Fact]
    public void If_not_exists_spaces_the_AND_and_quotes_the_key()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false, IsKey = true },
            new ColumnSchema { ColumnName = "Code", DataTypeName = "nvarchar", ColumnSize = 10, AllowDBNull = false, IsKey = true },
        };

        var sql = ScriptGenerator.BuildIfNotExistsScript(
            "dbo.Orders",
            columns,
            new[] { "Id", "Code" },
            new[] { new object?[] { 1, "A" } });

        Assert.Contains(
            "IF NOT EXISTS(SELECT 1 FROM [dbo].[Orders] WHERE [Id]=1 AND [Code]=N'A')\r\n" +
            "BEGIN\r\n" +
            "\tINSERT INTO [dbo].[Orders]([Id],[Code])\r\n" +
            "\tVALUES(1,N'A')\r\n" +
            "END\r\n" +
            "\r\n" +
            "GO\r\n",
            sql);
        Assert.Contains("ADD PRIMARY KEY\r\n([Id],[Code]\r\n);", sql);
    }

    [Fact]
    public void If_not_exists_without_a_key_is_rejected()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Note", DataTypeName = "nvarchar", ColumnSize = 10 },
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            ScriptGenerator.BuildIfNotExistsScript("dbo.Note", columns, Array.Empty<string>(), new[] { new object?[] { "x" } }));

        Assert.Equal("No Primary keys set in the source table so I can't generate a if exists", error.Message);
    }

    [Fact]
    public void Procedure_calls_use_bracketed_parameter_names()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false },
            new ColumnSchema { ColumnName = "Name", DataTypeName = "nvarchar", ColumnSize = 50 },
        };

        var sql = ScriptGenerator.BuildProcedureScript(
            "dbo.usp_Load",
            columns,
            new[] { new object?[] { 1, "O'Brien" } });

        Assert.Equal("\r\nEXEC [dbo].[usp_Load] @[Id]=1,@[Name]=N'O''Brien'\r\nGO\r\n", sql);
    }

    [Fact]
    public void Cursor_declares_typed_variables_and_loops()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = true },
            new ColumnSchema { ColumnName = "Name", DataTypeName = "nvarchar", ColumnSize = 50, AllowDBNull = true },
        };

        var sql = ScriptGenerator.BuildCursorScript(columns, "SELECT Id, Name FROM dbo.Person");

        Assert.Equal(
            "DECLARE @[Id] int = NULL\r\n" +
            "DECLARE @[Name] nvarchar(50) = NULL\r\n" +
            "\r\n" +
            "DECLARE @TableCursor as CURSOR\r\n" +
            "SET @TableCursor = CURSOR READ_ONLY FOR SELECT Id, Name FROM dbo.Person\r\n" +
            "OPEN @TableCursor\r\n" +
            "FETCH NEXT FROM @TableCursor INTO @[Id], @[Name]\r\n" +
            "WHILE @@FETCH_STATUS = 0\r\n" +
            "BEGIN\r\n" +
            "-- Code here\r\n" +
            "FETCH NEXT FROM @TableCursor INTO @[Id], @[Name]\r\n" +
            "END\r\n" +
            "CLOSE @TableCursor\r\n" +
            "DEALLOCATE @TableCursor\r\n",
            sql);
    }

    [Fact]
    public void CSharp_fragment_uses_Microsoft_Data_SqlClient_and_hides_passwords()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false, Ordinal = 0 },
            new ColumnSchema { ColumnName = "class", DataTypeName = "nvarchar", ColumnSize = 20, AllowDBNull = true, Ordinal = 1 },
        };
        var options = new SqlConnectionOptions
        {
            Server = @"localhost\SQLEXPRESS",
            Database = "AdventureWorks",
            TimeoutSeconds = 30,
            UserId = "loader",
            Password = "s3cret-value",
        };

        var code = ScriptGenerator.BuildCSharpFragment(columns, options, "SELECT Id, \"class\" FROM dbo.Person");

        Assert.Contains("using Microsoft.Data.SqlClient;", code);
        Assert.DoesNotContain("System.Data.SqlClient", code);
        Assert.DoesNotContain("reader.reader", code);
        Assert.Contains("int Id = reader.GetInt32(0);", code);
        Assert.Contains("string? @class = reader.IsDBNull(1) ? null : reader.GetString(1);", code);
        Assert.Contains("YOUR_PASSWORD", code);
        Assert.DoesNotContain("s3cret-value", code);
        Assert.Contains("SELECT Id, \"\"class\"\" FROM dbo.Person", code);
        Assert.Contains("localhost\\SQLEXPRESS", code);
    }

    [Fact]
    public void A_row_with_the_wrong_width_is_rejected()
    {
        var columns = new[]
        {
            new ColumnSchema { ColumnName = "Id", DataTypeName = "int" },
            new ColumnSchema { ColumnName = "Note", DataTypeName = "nvarchar", ColumnSize = 10 },
        };

        Assert.Throws<ArgumentException>(() =>
            ScriptGenerator.BuildInsertScript("T", columns, new[] { new object?[] { 1 } }));
    }

    private static ColumnSchema[] OrderColumns() => new[]
    {
        new ColumnSchema { ColumnName = "Id", DataTypeName = "int", AllowDBNull = false, IsKey = true },
        new ColumnSchema { ColumnName = "Note", DataTypeName = "nvarchar", ColumnSize = 100, AllowDBNull = true },
        new ColumnSchema { ColumnName = "Payload", DataTypeName = "varbinary", ColumnSize = -1, AllowDBNull = true },
        new ColumnSchema { ColumnName = "Amount", DataTypeName = "decimal", NumericPrecision = 10, NumericScale = 2, AllowDBNull = false },
    };
}
