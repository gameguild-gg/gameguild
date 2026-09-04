using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GameGuild.Learning.Assessments.Grading.Contracts;

[JsonConverter(typeof(ScoreValueJsonConverter))]
public readonly record struct ScoreValue : IComparable<ScoreValue>
{
    private const int ScaleDigits = 4;
    private static readonly BigInteger Scale = BigInteger.Pow(10, ScaleDigits);
    private static readonly BigInteger Maximum = BigInteger.Parse("999999999999");
    private static readonly Regex CanonicalPattern = new("^\\d{8}\\.\\d{4}$", RegexOptions.CultureInvariant);
    private readonly string _value;

    private ScoreValue(string value) => _value = value;

    public static ScoreValue Zero { get; } = new("00000000.0000");

    public static ScoreValue Parse(string value)
    {
        if (!CanonicalPattern.IsMatch(value))
        {
            throw new FormatException("ScoreValue must match ^\\d{8}\\.\\d{4}$.");
        }

        return new ScoreValue(value);
    }

    public static ScoreValue Canonicalize(string value) => FromScaled(ParseDraft(value, 8, Maximum));

    public static ScoreValue Sum(IEnumerable<ScoreValue> values) =>
        FromScaled(values.Aggregate(BigInteger.Zero, (sum, value) => sum + value.ToScaled()));

    public static ScoreValue ByRatio(ScoreValue maximum, BigInteger earnedUnits, BigInteger totalUnits)
    {
        if (earnedUnits < 0 || totalUnits <= 0 || earnedUnits > totalUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(earnedUnits), "The ratio must satisfy 0 <= earned <= total.");
        }

        var numerator = maximum.ToScaled() * earnedUnits;
        return FromScaled((numerator + totalUnits / 2) / totalUnits);
    }

    public int CompareTo(ScoreValue other) => string.CompareOrdinal(_value, other._value);

    public override string ToString() => _value ?? string.Empty;

    internal BigInteger ToScaled() => ParseCanonicalScaled(_value);

    private static ScoreValue FromScaled(BigInteger value)
    {
        EnsureRange(value, Maximum, nameof(ScoreValue));
        return new ScoreValue(FormatScaled(value, 8));
    }

    private static BigInteger ParseDraft(string value, int integerWidth, BigInteger maximum)
    {
        var match = Regex.Match(value.Trim(), "^(0|[1-9]\\d*)(?:\\.(\\d{0,4}))?$", RegexOptions.CultureInvariant);
        if (!match.Success || match.Groups[1].Value.Length > integerWidth)
        {
            throw new FormatException($"Expected a non-negative decimal with at most {integerWidth} integer and 4 fractional digits.");
        }

        var fraction = match.Groups[2].Value.PadRight(ScaleDigits, '0');
        var scaled = BigInteger.Parse(match.Groups[1].Value) * Scale + BigInteger.Parse(fraction.Length == 0 ? "0" : fraction);
        EnsureRange(scaled, maximum, "Decimal value");
        return scaled;
    }

    private static BigInteger ParseCanonicalScaled(string value)
    {
        var parts = value.Split('.');
        return BigInteger.Parse(parts[0]) * Scale + BigInteger.Parse(parts[1]);
    }

    private static string FormatScaled(BigInteger value, int integerWidth)
    {
        var integer = value / Scale;
        var fraction = value % Scale;
        return $"{integer.ToString().PadLeft(integerWidth, '0')}.{fraction.ToString().PadLeft(ScaleDigits, '0')}";
    }

    private static void EnsureRange(BigInteger value, BigInteger maximum, string label)
    {
        if (value < 0 || value > maximum)
        {
            throw new ArgumentOutOfRangeException(label, $"{label} is outside the supported range.");
        }
    }

    internal static BigInteger ParseDraftScaled(string value, int integerWidth, BigInteger maximum) =>
        ParseDraft(value, integerWidth, maximum);

    internal static string FormatScaledValue(BigInteger value, int integerWidth) => FormatScaled(value, integerWidth);
}

[JsonConverter(typeof(PercentValueJsonConverter))]
public readonly record struct PercentValue : IComparable<PercentValue>
{
    private static readonly BigInteger Maximum = BigInteger.Parse("1000000");
    private static readonly Regex CanonicalPattern = new("^\\d{3}\\.\\d{4}$", RegexOptions.CultureInvariant);
    private readonly string _value;

    private PercentValue(string value) => _value = value;

    public static PercentValue Zero { get; } = new("000.0000");
    public static PercentValue Hundred { get; } = new("100.0000");

    public static PercentValue Parse(string value)
    {
        if (!CanonicalPattern.IsMatch(value))
        {
            throw new FormatException("PercentValue must match ^\\d{3}\\.\\d{4}$.");
        }

        var scaled = BigInteger.Parse(value.Replace(".", string.Empty, StringComparison.Ordinal));
        if (scaled > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "PercentValue must be between 000.0000 and 100.0000.");
        }

        return new PercentValue(value);
    }

    public static PercentValue Canonicalize(string value)
    {
        var scaled = ScoreValue.ParseDraftScaled(value, 3, Maximum);
        return new PercentValue(ScoreValue.FormatScaledValue(scaled, 3));
    }

    public int CompareTo(PercentValue other) => string.CompareOrdinal(_value, other._value);

    public override string ToString() => _value ?? string.Empty;
}

public sealed class ScoreValueJsonConverter : JsonConverter<ScoreValue>
{
    public override ScoreValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? ScoreValue.Parse(reader.GetString()!)
            : throw new JsonException("ScoreValue must be a JSON string.");

    public override void Write(Utf8JsonWriter writer, ScoreValue value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public sealed class PercentValueJsonConverter : JsonConverter<PercentValue>
{
    public override PercentValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? PercentValue.Parse(reader.GetString()!)
            : throw new JsonException("PercentValue must be a JSON string.");

    public override void Write(Utf8JsonWriter writer, PercentValue value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
