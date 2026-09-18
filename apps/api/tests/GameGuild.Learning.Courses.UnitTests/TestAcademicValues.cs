global using GameGuild.Learning.Grading.Contracts;
global using static GameGuild.Learning.Courses.UnitTests.TestAcademicValues;

namespace GameGuild.Learning.Courses.UnitTests;

internal static class TestAcademicValues
{
    public static ScoreValue Score(string value) => ScoreValue.FromPoints(value);

    public static ScoreValue Score(int value) => Score(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static ScoreValue Score(decimal value) => Score(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static PercentValue Percent(string value) => PercentValue.FromPercentage(value);

    public static PercentValue Percent(int value) => Percent(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static PercentValue Percent(decimal value) => Percent(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
