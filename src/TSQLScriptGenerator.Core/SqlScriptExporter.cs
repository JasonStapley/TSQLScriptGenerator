using System.Data;
using Microsoft.Data.SqlClient;

namespace TSQLScriptGenerator;

/// <summary>
/// Runs a query with Microsoft.Data.SqlClient and writes a script. Generation itself lives in <see cref="ScriptGenerator"/>.
/// </summary>
public sealed class SqlScriptExporter
{
    private readonly SqlConnectionOptions _options;

    public SqlScriptExporter(SqlConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public static IReadOnlyList<string> ListDatabases(SqlConnectionOptions options)
    {
        var master = options with { Database = "master" };
        using var connection = Open(master);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [name] FROM sys.databases";
        command.CommandTimeout = options.TimeoutSeconds;
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    public void ExportInserts(string outputPath, string query, string tableName, bool ifNotExists, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        using var connection = Open(_options);
        IReadOnlyList<string>? keys = null;
        if (ifNotExists)
        {
            keys = ReadKeyColumns(connection, query);
            if (keys.Count == 0)
            {
                throw new InvalidOperationException("No Primary keys set in the source table so I can't generate a if exists");
            }
        }

        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = _options.TimeoutSeconds;
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            throw new InvalidOperationException($"No rows for extract Query {tableName}");
        }

        var columns = ReadColumns(reader);
        using var writer = ScriptFiles.CreateText(outputPath, overwrite);
        if (ifNotExists)
        {
            ScriptGenerator.WriteIfNotExistsScript(writer, tableName, columns, keys!, ReadRows(reader, columns));
        }
        else
        {
            ScriptGenerator.WriteInsertScript(writer, tableName, columns, ReadRows(reader, columns));
        }
    }

    public void ExportProcedureCalls(string outputPath, string query, string procedureName, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureName);

        using var connection = Open(_options);
        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = _options.TimeoutSeconds;
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            throw new InvalidOperationException($"No rows for extract Query {procedureName}");
        }

        var columns = ReadColumns(reader);
        using var writer = ScriptFiles.CreateText(outputPath, overwrite);
        ScriptGenerator.WriteProcedureScript(writer, procedureName, columns, ReadRows(reader, columns));
    }

    public void ExportCursor(string outputPath, string query, string label, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var name = string.IsNullOrWhiteSpace(label) ? "query" : label;

        using var connection = Open(_options);
        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = _options.TimeoutSeconds;
        using var reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            throw new InvalidOperationException($"No rows for cursor {name}");
        }

        var columns = ReadColumns(reader);
        using var writer = ScriptFiles.CreateText(outputPath, overwrite);
        ScriptGenerator.WriteCursorScript(writer, columns, query);
    }

    public void ExportCSharp(string outputPath, string query, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var connection = Open(_options);
        var columns = ReadSchema(connection, query);
        var script = ScriptGenerator.BuildCSharpFragment(columns, _options, query);
        using var writer = ScriptFiles.CreateText(outputPath, overwrite);
        writer.Write(script);
    }

    private static SqlConnection Open(SqlConnectionOptions options)
    {
        var connection = new SqlConnection(SqlConnectionStrings.Build(options));
        connection.Open();
        return connection;
    }

    private IReadOnlyList<string> ReadKeyColumns(SqlConnection connection, string query)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = _options.TimeoutSeconds;
            command.CommandType = CommandType.Text;
            using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
            return ReadColumns(reader).Where(static column => column.IsKey).Select(static column => column.ColumnName).ToList();
        }
        catch (SqlException ex) when (ex.Number != -2)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = _options.TimeoutSeconds;
            command.CommandType = CommandType.Text;
            using var reader = command.ExecuteReader(CommandBehavior.KeyInfo);
            return ReadColumns(reader).Where(static column => column.IsKey).Select(static column => column.ColumnName).ToList();
        }
    }

    private IReadOnlyList<ColumnSchema> ReadSchema(SqlConnection connection, string query)
    {
        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.TimeoutSeconds;
        command.CommandType = CommandType.Text;
        try
        {
            using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
            return ReadColumns(reader);
        }
        catch (SqlException ex) when (ex.Number != -2)
        {
            using var fallback = connection.CreateCommand();
            fallback.CommandText = query;
            fallback.CommandTimeout = _options.TimeoutSeconds;
            fallback.CommandType = CommandType.Text;
            using var reader = fallback.ExecuteReader();
            return ReadColumns(reader);
        }
    }

    private static IReadOnlyList<ColumnSchema> ReadColumns(SqlDataReader reader)
    {
        var schema = reader.GetSchemaTable()
            ?? throw new InvalidOperationException("The query did not return a result schema.");
        var columns = ColumnSchema.FromSchemaTable(schema);
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("The query did not return any columns.");
        }

        return columns;
    }

    private static IEnumerable<IReadOnlyList<object?>> ReadRows(SqlDataReader reader, IReadOnlyList<ColumnSchema> columns)
    {
        while (reader.Read())
        {
            var values = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                values[i] = reader.IsDBNull(columns[i].Ordinal) ? null : reader.GetValue(columns[i].Ordinal);
            }

            yield return values;
        }
    }
}
