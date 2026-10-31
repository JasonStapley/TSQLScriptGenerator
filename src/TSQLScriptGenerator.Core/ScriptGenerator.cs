using System.Globalization;
using System.Text;

namespace TSQLScriptGenerator;

/// <summary>
/// Builds T-SQL and C# text from an in-memory result. Nothing in this type opens a connection,
/// so the same output can be checked without a SQL Server instance.
/// Scripts use CRLF so a file produced on Linux matches one produced on Windows.
/// </summary>
public static class ScriptGenerator
{
    private const string NewLine = "\r\n";

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        "record", "nint", "nuint", "required", "file", "scoped", "init",
    };

    public static string BuildCreateTable(string tableName, IReadOnlyList<ColumnSchema> columns)
    {
        ValidateTable(tableName);
        ValidateColumns(columns);
        var quotedTable = TSqlLiteral.QuoteIdentifier(tableName);
        var builder = new StringBuilder();
        builder.Append("IF OBJECT_ID(")
            .Append(TSqlLiteral.FormatString(quotedTable, unicode: true))
            .Append(") IS NULL")
            .Append(NewLine);
        builder.Append("BEGIN").Append(NewLine);
        builder.Append("CREATE TABLE ").Append(quotedTable).Append('(');
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(TSqlLiteral.QuoteIdentifier(columns[i].ColumnName));
            builder.Append(' ');
            builder.Append(TSqlLiteral.FormatDataType(columns[i]));
            builder.Append(columns[i].AllowDBNull ? " NULL" : " NOT NULL");
        }

        builder.Append(NewLine);
        builder.Append(");").Append(NewLine);
        builder.Append(BuildPrimaryKey(quotedTable, columns));
        builder.Append(NewLine);
        builder.Append("END");
        return builder.ToString();
    }

    public static void WriteInsertScript(TextWriter writer, string tableName, IReadOnlyList<ColumnSchema> columns, IEnumerable<IReadOnlyList<object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(rows);
        writer.NewLine = NewLine;
        writer.WriteLine(BuildCreateTable(tableName, columns));
        var header = BuildInsertHeader(tableName, columns);
        foreach (var row in rows)
        {
            writer.WriteLine(header);
            writer.Write("VALUES(");
            writer.Write(FormatRow(columns, row));
            writer.WriteLine(")");
            writer.WriteLine("GO");
        }
    }

    public static string BuildInsertScript(string tableName, IReadOnlyList<ColumnSchema> columns, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var writer = new StringWriter { NewLine = NewLine };
        WriteInsertScript(writer, tableName, columns, rows);
        return writer.ToString();
    }

    public static void WriteIfNotExistsScript(
        TextWriter writer,
        string tableName,
        IReadOnlyList<ColumnSchema> columns,
        IReadOnlyList<string> primaryKeyColumns,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(primaryKeyColumns);
        ArgumentNullException.ThrowIfNull(rows);
        if (primaryKeyColumns.Count == 0)
        {
            throw new InvalidOperationException("No Primary keys set in the source table so I can't generate a if exists");
        }

        writer.NewLine = NewLine;
        writer.WriteLine(BuildCreateTable(tableName, columns));
        var quotedTable = TSqlLiteral.QuoteIdentifier(tableName);
        var header = BuildInsertHeader(tableName, columns);
        var keyIndexes = primaryKeyColumns.Select(name => IndexOfColumn(columns, name)).ToArray();
        foreach (var row in rows)
        {
            ValidateRowWidth(columns, row);
            writer.WriteLine();
            writer.Write("IF NOT EXISTS(SELECT 1 FROM ");
            writer.Write(quotedTable);
            writer.Write(" WHERE ");
            for (var i = 0; i < keyIndexes.Length; i++)
            {
                if (i > 0)
                {
                    writer.Write(" AND ");
                }

                var index = keyIndexes[i];
                writer.Write(TSqlLiteral.QuoteIdentifier(columns[index].ColumnName));
                writer.Write('=');
                writer.Write(TSqlLiteral.FormatValue(row[index], columns[index]));
            }

            writer.WriteLine(")");
            writer.WriteLine("BEGIN");
            writer.Write('\t');
            writer.WriteLine(header);
            writer.Write("\tVALUES(");
            writer.Write(FormatRow(columns, row));
            writer.WriteLine(")");
            writer.WriteLine("END");
            writer.WriteLine();
            writer.WriteLine("GO");
        }
    }

    public static string BuildIfNotExistsScript(
        string tableName,
        IReadOnlyList<ColumnSchema> columns,
        IReadOnlyList<string> primaryKeyColumns,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var writer = new StringWriter { NewLine = NewLine };
        WriteIfNotExistsScript(writer, tableName, columns, primaryKeyColumns, rows);
        return writer.ToString();
    }

    public static void WriteProcedureScript(
        TextWriter writer,
        string procedureName,
        IReadOnlyList<ColumnSchema> columns,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(rows);
        ValidateTable(procedureName);
        ValidateColumns(columns);
        writer.NewLine = NewLine;
        var quotedProcedure = TSqlLiteral.QuoteIdentifier(procedureName);
        foreach (var row in rows)
        {
            ValidateRowWidth(columns, row);
            writer.WriteLine();
            writer.Write("EXEC ");
            writer.Write(quotedProcedure);
            writer.Write(' ');
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(',');
                }

                writer.Write('@');
                writer.Write(TSqlLiteral.QuoteIdentifier(columns[i].ColumnName));
                writer.Write('=');
                writer.Write(TSqlLiteral.FormatValue(row[i], columns[i]));
            }

            writer.WriteLine();
            writer.WriteLine("GO");
        }
    }

    public static string BuildProcedureScript(string procedureName, IReadOnlyList<ColumnSchema> columns, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var writer = new StringWriter { NewLine = NewLine };
        WriteProcedureScript(writer, procedureName, columns, rows);
        return writer.ToString();
    }

    public static void WriteCursorScript(TextWriter writer, IReadOnlyList<ColumnSchema> columns, string query)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ValidateColumns(columns);
        writer.NewLine = NewLine;
        foreach (var column in columns)
        {
            writer.Write("DECLARE ");
            writer.Write(Variable(column));
            writer.Write(' ');
            writer.Write(TSqlLiteral.FormatDataType(column));
            writer.WriteLine(" = NULL");
        }

        writer.WriteLine();
        writer.WriteLine("DECLARE @TableCursor as CURSOR");
        writer.Write("SET @TableCursor = CURSOR READ_ONLY FOR ");
        writer.WriteLine(query.Trim());
        writer.WriteLine("OPEN @TableCursor");
        writer.Write("FETCH NEXT FROM @TableCursor INTO ");
        WriteVariableList(writer, columns);
        writer.WriteLine();
        writer.WriteLine("WHILE @@FETCH_STATUS = 0");
        writer.WriteLine("BEGIN");
        writer.WriteLine("-- Code here");
        writer.Write("FETCH NEXT FROM @TableCursor INTO ");
        WriteVariableList(writer, columns);
        writer.WriteLine();
        writer.WriteLine("END");
        writer.WriteLine("CLOSE @TableCursor");
        writer.WriteLine("DEALLOCATE @TableCursor");
    }

    public static string BuildCursorScript(IReadOnlyList<ColumnSchema> columns, string query)
    {
        using var writer = new StringWriter { NewLine = NewLine };
        WriteCursorScript(writer, columns, query);
        return writer.ToString();
    }

    public static string BuildCSharpFragment(IReadOnlyList<ColumnSchema> columns, SqlConnectionOptions options, string query)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ValidateColumns(columns);

        var safeOptions = string.IsNullOrEmpty(options.Password)
            ? options
            : options with { Password = "YOUR_PASSWORD" };
        var connectionString = SqlConnectionStrings.Build(safeOptions);
        var builder = new StringBuilder();
        builder.Append("// Code fragment generated by TSQLScriptGenerator.").Append(NewLine);
        builder.Append("// Review nullability and the connection string before running it.").Append(NewLine);
        if (!string.IsNullOrEmpty(options.Password))
        {
            builder.Append("// The password was replaced with YOUR_PASSWORD.").Append(NewLine);
        }

        builder.Append("using Microsoft.Data.SqlClient;").Append(NewLine);
        builder.Append(NewLine);
        builder.Append("using (var connection = new SqlConnection(").Append(ToVerbatim(connectionString)).Append("))").Append(NewLine);
        builder.Append('{').Append(NewLine);
        builder.Append("    connection.Open();").Append(NewLine);
        builder.Append("    using (var command = new SqlCommand(").Append(ToVerbatim(query)).Append(", connection))").Append(NewLine);
        builder.Append("    {").Append(NewLine);
        builder.Append("        command.CommandType = System.Data.CommandType.Text;").Append(NewLine);
        builder.Append("        command.CommandTimeout = ").Append(options.TimeoutSeconds.ToString(CultureInfo.InvariantCulture)).Append(';').Append(NewLine);
        builder.Append("        using (var reader = command.ExecuteReader())").Append(NewLine);
        builder.Append("        {").Append(NewLine);
        builder.Append("            while (reader.Read())").Append(NewLine);
        builder.Append("            {").Append(NewLine);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in columns)
        {
            builder.Append("                ");
            builder.Append(CSharpDeclaration(column, usedNames));
            builder.Append(NewLine);
        }

        builder.Append("            }").Append(NewLine);
        builder.Append("        }").Append(NewLine);
        builder.Append("    }").Append(NewLine);
        builder.Append('}').Append(NewLine);
        return builder.ToString();
    }

    private static string BuildPrimaryKey(string quotedTable, IReadOnlyList<ColumnSchema> columns)
    {
        var keys = columns.Where(static column => column.IsKey).ToList();
        if (keys.Count == 0)
        {
            return "";
        }

        var keyList = string.Join(",", keys.Select(static key => TSqlLiteral.QuoteIdentifier(key.ColumnName)));
        return "ALTER TABLE " + quotedTable + NewLine
            + "ADD PRIMARY KEY" + NewLine
            + "(" + keyList + NewLine
            + ");";
    }

    private static string BuildInsertHeader(string tableName, IReadOnlyList<ColumnSchema> columns)
    {
        var columnList = string.Join(",", columns.Select(static column => TSqlLiteral.QuoteIdentifier(column.ColumnName)));
        return "INSERT INTO " + TSqlLiteral.QuoteIdentifier(tableName) + "(" + columnList + ")";
    }

    private static string FormatRow(IReadOnlyList<ColumnSchema> columns, IReadOnlyList<object?> row)
    {
        ValidateRowWidth(columns, row);
        var values = new string[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            values[i] = TSqlLiteral.FormatValue(row[i], columns[i]);
        }

        return string.Join(",", values);
    }

    private static void WriteVariableList(TextWriter writer, IReadOnlyList<ColumnSchema> columns)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(", ");
            }

            writer.Write(Variable(columns[i]));
        }
    }

    private static string Variable(ColumnSchema column) => "@" + TSqlLiteral.QuoteIdentifier(column.ColumnName);

    private static int IndexOfColumn(IReadOnlyList<ColumnSchema> columns, string name)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (string.Equals(columns[i].ColumnName, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Primary key column '{name}' is not in the result set.");
    }

    private static void ValidateTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
    }

    private static void ValidateColumns(IReadOnlyList<ColumnSchema> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        if (columns.Count == 0)
        {
            throw new ArgumentException("The result has no columns.", nameof(columns));
        }
    }

    private static void ValidateRowWidth(IReadOnlyList<ColumnSchema> columns, IReadOnlyList<object?> row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Count != columns.Count)
        {
            throw new ArgumentException($"Expected {columns.Count} values but the row has {row.Count}.", nameof(row));
        }
    }

    private static string ToVerbatim(string value) => "@\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string CSharpDeclaration(ColumnSchema column, ISet<string> usedNames)
    {
        var (clrType, read) = ReaderAccess(column);
        var name = CSharpName(column.ColumnName, usedNames);
        if (!column.AllowDBNull)
        {
            return clrType + " " + name + " = " + read + ";";
        }

        var nullable = clrType is "string" or "byte[]" ? clrType + "?" : clrType + "?";
        return nullable + " " + name + " = reader.IsDBNull(" + column.Ordinal.ToString(CultureInfo.InvariantCulture) + ") ? null : " + read + ";";
    }

    private static (string Type, string Read) ReaderAccess(ColumnSchema column)
    {
        var ordinal = column.Ordinal.ToString(CultureInfo.InvariantCulture);
        var type = column.DataTypeName.Trim().ToLowerInvariant();
        var parenthesis = type.IndexOf('(');
        if (parenthesis > 0)
        {
            type = type[..parenthesis].TrimEnd();
        }

        return type switch
        {
            "bigint" => ("long", "reader.GetInt64(" + ordinal + ")"),
            "int" => ("int", "reader.GetInt32(" + ordinal + ")"),
            "smallint" => ("short", "reader.GetInt16(" + ordinal + ")"),
            "tinyint" => ("byte", "reader.GetByte(" + ordinal + ")"),
            "bit" => ("bool", "reader.GetBoolean(" + ordinal + ")"),
            "decimal" or "numeric" or "money" or "smallmoney" => ("decimal", "reader.GetDecimal(" + ordinal + ")"),
            "float" => ("double", "reader.GetDouble(" + ordinal + ")"),
            "real" => ("float", "reader.GetFloat(" + ordinal + ")"),
            "date" or "datetime" or "smalldatetime" or "datetime2" => ("DateTime", "reader.GetDateTime(" + ordinal + ")"),
            "datetimeoffset" => ("DateTimeOffset", "reader.GetDateTimeOffset(" + ordinal + ")"),
            "time" => ("TimeSpan", "reader.GetTimeSpan(" + ordinal + ")"),
            "uniqueidentifier" => ("Guid", "reader.GetGuid(" + ordinal + ")"),
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => ("byte[]", "reader.GetFieldValue<byte[]>(" + ordinal + ")"),
            _ => ("string", "reader.GetString(" + ordinal + ")"),
        };
    }

    private static string CSharpName(string columnName, ISet<string> usedNames)
    {
        var builder = new StringBuilder();
        foreach (var character in columnName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        if (builder.Length == 0 || !(char.IsLetter(builder[0]) || builder[0] == '_'))
        {
            builder.Insert(0, '_');
        }

        var name = builder.ToString();
        if (CSharpKeywords.Contains(name))
        {
            name = "@" + name;
        }

        var candidate = name;
        var suffix = 2;
        while (!usedNames.Add(candidate))
        {
            candidate = name + suffix.ToString(CultureInfo.InvariantCulture);
            suffix++;
        }

        return candidate;
    }
}
