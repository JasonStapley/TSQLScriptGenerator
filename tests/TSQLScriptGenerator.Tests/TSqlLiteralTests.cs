using System.Globalization;

namespace TSQLScriptGenerator.Tests;

public class TSqlLiteralTests
{
    [Fact]
    public void Null_and_DBNull_are_NULL()
    {
        var column = Col("A", "int");
        Assert.Equal("NULL", TSqlLiteral.FormatValue(null, column));
        Assert.Equal("NULL", TSqlLiteral.FormatValue(DBNull.Value, column));
    }

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void Bit_is_one_or_zero(bool value, string expected)
    {
        Assert.Equal(expected, TSqlLiteral.FormatValue(value, Col("Flag", "bit")));
    }

    [Fact]
    public void Integers_use_invariant_digits()
    {
        Assert.Equal("-42", TSqlLiteral.FormatValue(-42, Col("A", "int")));
        Assert.Equal("255", TSqlLiteral.FormatValue((byte)255, Col("A", "tinyint")));
        Assert.Equal(long.MaxValue.ToString(CultureInfo.InvariantCulture), TSqlLiteral.FormatValue(long.MaxValue, Col("A", "bigint")));
    }

    [Fact]
    public void Decimal_money_and_float_ignore_the_current_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var mutated = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            mutated.NumberFormat.NumberDecimalSeparator = ",";
            mutated.NumberFormat.NumberGroupSeparator = ".";
            mutated.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            CultureInfo.CurrentCulture = mutated;
            CultureInfo.CurrentUICulture = mutated;

            Assert.Equal("1.50", TSqlLiteral.FormatValue(1.5m, Col("A", "decimal", scale: 2)));
            Assert.Equal("1.2000", TSqlLiteral.FormatValue(1.2m, Col("A", "money")));
            Assert.Equal("1.25", TSqlLiteral.FormatValue(1.25d, Col("A", "float")));
            Assert.Equal("1.25", TSqlLiteral.FormatValue(1.25f, Col("A", "real")));
            Assert.Equal(
                "'2024-03-09T07:08:09.123'",
                TSqlLiteral.FormatValue(new DateTime(2024, 3, 9, 7, 8, 9, 123), Col("A", "datetime")));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void Dates_use_an_unambiguous_calendar_form()
    {
        var instant = new DateTime(2024, 3, 9, 7, 8, 9, 123);
        Assert.Equal("'2024-03-09'", TSqlLiteral.FormatValue(instant, Col("A", "date")));
        Assert.Equal("'2024-03-09T07:08:09'", TSqlLiteral.FormatValue(instant, Col("A", "smalldatetime")));
        Assert.Equal("'2024-03-09T07:08:09.123'", TSqlLiteral.FormatValue(instant, Col("A", "datetime")));

        var precise = new DateTime(2024, 3, 9, 7, 8, 9, 123).AddTicks(4567);
        Assert.Equal(
            "'2024-03-09T07:08:09.1234567'",
            TSqlLiteral.FormatValue(precise, Col("A", "datetime2", scale: 7)));

        var time = new TimeSpan(0, 1, 2, 3, 4);
        Assert.Equal("'01:02:03.004'", TSqlLiteral.FormatValue(time, Col("A", "time", scale: 3)));

        var offset = new DateTimeOffset(2024, 3, 9, 7, 8, 9, 123, TimeSpan.FromMinutes(90));
        Assert.Equal(
            "'2024-03-09T07:08:09.1230000 +01:30'",
            TSqlLiteral.FormatValue(offset, Col("A", "datetimeoffset", scale: 7)));
    }

    [Fact]
    public void Strings_escape_quotes_and_unicode_types_use_N()
    {
        Assert.Equal("N'O''Brien'", TSqlLiteral.FormatValue("O'Brien", Col("A", "nvarchar")));
        Assert.Equal("N'O''Brien'", TSqlLiteral.FormatValue("O'Brien", Col("A", "nchar")));
        Assert.Equal("N'<x a=''b''/>'", TSqlLiteral.FormatValue("<x a='b'/>", Col("A", "xml")));
        Assert.Equal("'it''s'", TSqlLiteral.FormatValue("it's", Col("A", "varchar")));
        Assert.Equal("N''''", TSqlLiteral.FormatValue("'", Col("A", "nvarchar")));
        Assert.Equal("N'café'", TSqlLiteral.FormatValue("café", Col("A", "varchar")));
    }

    [Fact]
    public void Embedded_NUL_is_split_out_of_the_literal()
    {
        Assert.Equal("'a' + CHAR(0) + 'b'", TSqlLiteral.FormatValue("a\0b", Col("A", "varchar")));
        Assert.Equal("N'a' + NCHAR(0) + N'b'", TSqlLiteral.FormatValue("a\0b", Col("A", "nvarchar")));
    }

    [Fact]
    public void Binary_and_rowversion_are_hex_not_quoted_strings()
    {
        Assert.Equal("0x000AFF", TSqlLiteral.FormatValue(new byte[] { 0x00, 0x0A, 0xFF }, Col("A", "varbinary")));
        Assert.Equal("0x000AFF", TSqlLiteral.FormatValue(new byte[] { 0x00, 0x0A, 0xFF }, Col("A", "binary")));
        Assert.Equal("0x", TSqlLiteral.FormatValue(Array.Empty<byte>(), Col("A", "varbinary")));
        Assert.Equal("0x0A", TSqlLiteral.FormatValue(new byte[] { 0x0A }, Col("A", "timestamp")));
        Assert.Equal("NULL", TSqlLiteral.FormatValue(null, Col("A", "varbinary")));
    }

    [Fact]
    public void Uniqueidentifier_is_a_canonical_string()
    {
        var guid = Guid.Parse("00112233-4455-6677-8899-AABBCCDDEEFF");
        Assert.Equal("'00112233-4455-6677-8899-aabbccddeeff'", TSqlLiteral.FormatValue(guid, Col("A", "uniqueidentifier")));
    }

    [Fact]
    public void Non_finite_floats_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => TSqlLiteral.FormatValue(double.NaN, Col("A", "float")));
        Assert.Throws<InvalidOperationException>(() => TSqlLiteral.FormatValue(float.PositiveInfinity, Col("A", "real")));
    }

    [Theory]
    [InlineData("Orders", "[Orders]")]
    [InlineData("dbo.Orders", "[dbo].[Orders]")]
    [InlineData("dbo.[Order Details]", "[dbo].[Order Details]")]
    [InlineData("[dbo].[Orders]", "[dbo].[Orders]")]
    [InlineData("Weird]", "[Weird]]]")]
    [InlineData("[O]]Brien]", "[O]]Brien]")]
    [InlineData("a.b.c", "[a].[b].[c]")]
    public void Identifiers_are_bracketed_per_part(string name, string expected)
    {
        Assert.Equal(expected, TSqlLiteral.QuoteIdentifier(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("dbo..Orders")]
    [InlineData("[unclosed")]
    public void Bad_identifiers_are_rejected(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => TSqlLiteral.QuoteIdentifier(name));
    }

    [Fact]
    public void Data_type_text_includes_length_precision_and_max()
    {
        Assert.Equal("nvarchar(100)", TSqlLiteral.FormatDataType(Col("A", "nvarchar", size: 100)));
        Assert.Equal("varchar(max)", TSqlLiteral.FormatDataType(Col("A", "varchar", size: int.MaxValue)));
        Assert.Equal("nvarchar(max)", TSqlLiteral.FormatDataType(Col("A", "nvarchar", size: 1073741823)));
        Assert.Equal("varbinary(max)", TSqlLiteral.FormatDataType(Col("A", "varbinary", size: -1)));
        Assert.Equal("char(10)", TSqlLiteral.FormatDataType(Col("A", "char", size: 10)));
        Assert.Equal("decimal(10,2)", TSqlLiteral.FormatDataType(Col("A", "decimal", precision: 10, scale: 2)));
        Assert.Equal("decimal(18,0)", TSqlLiteral.FormatDataType(Col("A", "decimal")));
        Assert.Equal("datetime2(3)", TSqlLiteral.FormatDataType(Col("A", "datetime2", scale: 3)));
        Assert.Equal("int", TSqlLiteral.FormatDataType(Col("A", "int")));
    }

    private static ColumnSchema Col(string name, string type, int? size = null, int? precision = null, int? scale = null)
    {
        return new ColumnSchema
        {
            ColumnName = name,
            DataTypeName = type,
            ColumnSize = size,
            NumericPrecision = precision,
            NumericScale = scale,
        };
    }
}
