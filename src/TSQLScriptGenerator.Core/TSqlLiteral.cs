using System.Globalization;
using System.Text;

namespace TSQLScriptGenerator;

/// <summary>
/// Formats CLR values and identifiers as T-SQL literals. Formatting is culture-invariant:
/// decimal points stay '.', and date text does not follow the machine locale.
/// </summary>
public static class TSqlLiteral
{
    public static string QuoteIdentifier(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var parts = SplitIdentifier(name.Trim());
        return string.Join(".", parts.Select(QuotePart));
    }

    public static string FormatValue(object? value, ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (value is null or DBNull)
        {
            return "NULL";
        }

        var type = NormalizeType(column.DataTypeName);
        return type switch
        {
            "bit" => ToBool(value) ? "1" : "0",
            "tinyint" or "smallint" or "int" or "bigint" => Convert.ToInt64(value, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
            "decimal" or "numeric" => FormatDecimal(Convert.ToDecimal(value, CultureInfo.InvariantCulture), column.NumericScale),
            "money" or "smallmoney" => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
                .ToString("0.0000", CultureInfo.InvariantCulture),
            "float" => FormatFloat(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
            "real" => FormatReal(Convert.ToSingle(value, CultureInfo.InvariantCulture)),
            "date" => "'" + ToDateTime(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'",
            "smalldatetime" => "'" + ToDateTime(value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "'",
            "datetime" => "'" + ToDateTime(value).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "'",
            "datetime2" => "'" + ToDateTime(value).ToString(DateTimePattern(column.NumericScale ?? 7), CultureInfo.InvariantCulture) + "'",
            "time" => "'" + FormatTime(ToTimeSpan(value), column.NumericScale ?? 7) + "'",
            "datetimeoffset" => "'" + ToDateTimeOffset(value).ToString(DateTimeOffsetPattern(column.NumericScale ?? 7), CultureInfo.InvariantCulture) + "'",
            "uniqueidentifier" => "'" + ToGuid(value).ToString("D") + "'",
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => FormatBinary(ToBytes(value)),
            "nvarchar" or "nchar" or "ntext" or "sysname" or "xml" => FormatString(AsString(value), unicode: true),
            "varchar" or "char" or "text" => FormatString(AsString(value), unicode: false),
            _ => FormatUnknown(value),
        };
    }

    public static string FormatDataType(ColumnSchema column)
    {
        ArgumentNullException.ThrowIfNull(column);
        var type = NormalizeType(column.DataTypeName);
        return type switch
        {
            "varchar" or "nvarchar" or "char" or "nchar" or "binary" or "varbinary"
                => type + "(" + FormatLength(type, column.ColumnSize) + ")",
            "decimal" or "numeric" => FormatDecimalType(type, column),
            "datetime2" or "datetimeoffset" or "time" => FormatScaledType(type, column.NumericScale),
            _ => type,
        };
    }

    /// <summary>
    /// SQL string literal. Unicode types use an <c>N</c> prefix. Embedded quotes are doubled.
    /// An embedded NUL is split out with <c>CHAR(0)</c> or <c>NCHAR(0)</c> so the script file is not truncated.
    /// A non-unicode string that contains a non-ASCII character is promoted to <c>N''</c> so it is not
    /// interpreted in the database code page.
    /// </summary>
    public static string FormatString(string? value, bool unicode)
    {
        value ??= "";
        if (!unicode && value.Any(static c => c > 127))
        {
            unicode = true;
        }

        var prefix = unicode ? "N" : "";
        var zero = unicode ? "NCHAR(0)" : "CHAR(0)";
        if (value.IndexOf('\0') < 0)
        {
            return prefix + "'" + value.Replace("'", "''") + "'";
        }

        var parts = value.Split('\0');
        return string.Join(
            " + " + zero + " + ",
            parts.Select(part => prefix + "'" + part.Replace("'", "''") + "'"));
    }

    private static string FormatLength(string type, int? size)
    {
        if (size is null)
        {
            return SupportsMax(type) ? "max" : "1";
        }

        if (SupportsMax(type) && (size < 0 || ExceedsFixedMaximum(type, size.Value)))
        {
            return "max";
        }

        return size.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool SupportsMax(string type) => type is "varchar" or "nvarchar" or "varbinary";

    private static bool ExceedsFixedMaximum(string type, int size) => type switch
    {
        "nvarchar" or "nchar" => size > 4000,
        _ => size > 8000,
    };

    private static string FormatDecimalType(string type, ColumnSchema column)
    {
        var precision = column.NumericPrecision is > 0 and <= 38 ? column.NumericPrecision.Value : 18;
        var scale = column.NumericScale ?? 0;
        if (scale < 0)
        {
            scale = 0;
        }

        if (scale > precision)
        {
            scale = precision;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{type}({precision},{scale})");
    }

    private static string FormatScaledType(string type, int? scale)
    {
        if (scale is null or < 0)
        {
            return type;
        }

        var bounded = Math.Min(scale.Value, 7);
        return string.Create(CultureInfo.InvariantCulture, $"{type}({bounded})");
    }

    private static string FormatDecimal(decimal number, int? scale)
    {
        if (scale is >= 0 and <= 28)
        {
            var format = scale == 0 ? "0" : "0." + new string('0', scale.Value);
            return number.ToString(format, CultureInfo.InvariantCulture);
        }

        return number.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatFloat(double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            throw new InvalidOperationException("SQL float values cannot be NaN or infinity.");
        }

        return number.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static string FormatReal(float number)
    {
        if (float.IsNaN(number) || float.IsInfinity(number))
        {
            throw new InvalidOperationException("SQL real values cannot be NaN or infinity.");
        }

        return number.ToString("G9", CultureInfo.InvariantCulture);
    }

    private static string FormatBinary(byte[] bytes) => "0x" + Convert.ToHexString(bytes);

    private static string FormatTime(TimeSpan value, int scale)
    {
        scale = Math.Clamp(scale, 0, 7);
        var clock = value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        if (value < TimeSpan.Zero && !clock.StartsWith('-'))
        {
            clock = "-" + clock;
        }

        if (scale == 0)
        {
            return clock;
        }

        var fraction = Math.Abs(value.Ticks % TimeSpan.TicksPerSecond);
        var digits = fraction.ToString("D7", CultureInfo.InvariantCulture);
        return clock + "." + digits[..scale];
    }

    private static string DateTimePattern(int scale)
    {
        scale = Math.Clamp(scale, 0, 7);
        return scale == 0 ? "yyyy-MM-ddTHH:mm:ss" : "yyyy-MM-ddTHH:mm:ss." + new string('f', scale);
    }

    private static string DateTimeOffsetPattern(int scale)
    {
        scale = Math.Clamp(scale, 0, 7);
        return scale == 0
            ? "yyyy-MM-ddTHH:mm:ss zzz"
            : "yyyy-MM-ddTHH:mm:ss." + new string('f', scale) + " zzz";
    }

    private static string FormatUnknown(object value)
    {
        if (value is byte[] bytes)
        {
            return FormatBinary(bytes);
        }

        if (value is bool bit)
        {
            return bit ? "1" : "0";
        }

        if (IsNumber(value))
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "NULL";
        }

        return FormatString(AsString(value), unicode: true);
    }

    private static bool IsNumber(object value) => value is
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static string NormalizeType(string dataTypeName)
    {
        var type = dataTypeName.Trim().ToLowerInvariant();
        var parenthesis = type.IndexOf('(');
        if (parenthesis > 0)
        {
            type = type[..parenthesis].TrimEnd();
        }

        return type;
    }

    private static string AsString(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    private static bool ToBool(object value) => value switch
    {
        bool bit => bit,
        byte or short or int or long => Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0,
        string text when text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase) => true,
        string text when text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
        _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
    };

    private static DateTime ToDateTime(object value) => value switch
    {
        DateTime dateTime => dateTime,
        DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
    };

    private static TimeSpan ToTimeSpan(object value) => value switch
    {
        TimeSpan time => time,
        DateTime dateTime => dateTime.TimeOfDay,
        TimeOnly time => time.ToTimeSpan(),
        _ => TimeSpan.Parse(AsString(value), CultureInfo.InvariantCulture),
    };

    private static DateTimeOffset ToDateTimeOffset(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified)),
        _ => DateTimeOffset.Parse(AsString(value), CultureInfo.InvariantCulture),
    };

    private static Guid ToGuid(object value) => value switch
    {
        Guid guid => guid,
        _ => Guid.Parse(AsString(value)),
    };

    private static byte[] ToBytes(object value) => value as byte[]
        ?? throw new InvalidOperationException($"Cannot format a {value.GetType().Name} value as binary.");

    private static List<string> SplitIdentifier(string name)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inBrackets = false;
        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (character == '[')
            {
                inBrackets = true;
                current.Append(character);
                continue;
            }

            if (character == ']' && inBrackets)
            {
                if (i + 1 < name.Length && name[i + 1] == ']')
                {
                    current.Append("]]");
                    i++;
                    continue;
                }

                current.Append(character);
                inBrackets = false;
                continue;
            }

            if (character == '.' && !inBrackets)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        if (inBrackets)
        {
            throw new ArgumentException($"Identifier '{name}' has an unclosed '['.", nameof(name));
        }

        parts.Add(current.ToString());
        if (parts.Any(static part => string.IsNullOrWhiteSpace(part)))
        {
            throw new ArgumentException($"Identifier '{name}' has an empty part.", nameof(name));
        }

        return parts;
    }

    private static string QuotePart(string part)
    {
        part = part.Trim();
        if (part.Length >= 2 && part[0] == '[' && part[^1] == ']')
        {
            var inner = part[1..^1].Replace("]]", "]", StringComparison.Ordinal);
            return "[" + inner.Replace("]", "]]", StringComparison.Ordinal) + "]";
        }

        return "[" + part.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }
}
