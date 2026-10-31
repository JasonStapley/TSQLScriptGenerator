using System.Data;
using System.Globalization;

namespace TSQLScriptGenerator;

/// <summary>
/// One column from a SELECT result, with the SQL type metadata needed to script it.
/// </summary>
public sealed class ColumnSchema
{
    public required string ColumnName { get; init; }

    /// <summary>SQL type name without length, precision, or scale (for example <c>nvarchar</c>).</summary>
    public required string DataTypeName { get; init; }

    public int? ColumnSize { get; init; }

    public int? NumericPrecision { get; init; }

    public int? NumericScale { get; init; }

    public bool AllowDBNull { get; init; } = true;

    public bool IsKey { get; init; }

    /// <summary>Ordinal in the original reader. Hidden key columns are omitted from this list but keep their ordinals.</summary>
    public int Ordinal { get; init; }

    public Type? ClrType { get; init; }

    public static IReadOnlyList<ColumnSchema> FromSchemaTable(DataTable schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (!schema.Columns.Contains("ColumnName") || !schema.Columns.Contains("DataTypeName"))
        {
            throw new ArgumentException("Schema table must include ColumnName and DataTypeName.", nameof(schema));
        }

        var columns = new List<ColumnSchema>();
        for (var i = 0; i < schema.Rows.Count; i++)
        {
            var row = schema.Rows[i];
            if (ReadBool(row, "IsHidden"))
            {
                continue;
            }

            var name = Convert.ToString(row["ColumnName"], CultureInfo.InvariantCulture)?.Trim() ?? "";
            var dataType = Convert.ToString(row["DataTypeName"], CultureInfo.InvariantCulture)?.Trim() ?? "";
            if (string.IsNullOrEmpty(dataType))
            {
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(name)
                        ? $"Result column {i} does not have a data type."
                        : $"Result column '{name}' does not have a data type.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(
                    $"Result column {i} has no name. Alias every expression in the SELECT (for example: SELECT 1 AS Flag).");
            }

            columns.Add(new ColumnSchema
            {
                ColumnName = name,
                DataTypeName = dataType,
                ColumnSize = ReadInt(row, "ColumnSize"),
                NumericPrecision = ReadInt(row, "NumericPrecision"),
                NumericScale = ReadInt(row, "NumericScale"),
                AllowDBNull = !schema.Columns.Contains("AllowDBNull") || row.IsNull("AllowDBNull") || ReadBool(row, "AllowDBNull"),
                IsKey = ReadBool(row, "IsKey"),
                Ordinal = ReadInt(row, "ColumnOrdinal") ?? i,
                ClrType = schema.Columns.Contains("DataType") && !row.IsNull("DataType") ? row["DataType"] as Type : null,
            });
        }

        return columns;
    }

    private static int? ReadInt(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column) || row.IsNull(column))
        {
            return null;
        }

        return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
    }

    private static bool ReadBool(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column) || row.IsNull(column))
        {
            return false;
        }

        var value = row[column];
        return value switch
        {
            bool bit => bit,
            string text => text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1",
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
        };
    }
}
