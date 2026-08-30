namespace GameGuild.TestingLab.UnitTests;

internal static class TestingEventTestExtensions
{
    private static readonly QuestionnaireSchema EmptySchema = new("Test fixture", []);

    public static void OpenConfiguredApplications(this TestingEvent testingEvent)
    {
        testingEvent.Configure(
            "Fixture rules",
            "Fixture candidate instructions",
            "Fixture tester instructions",
            EmptySchema,
            EmptySchema);
        testingEvent.OpenApplications();
    }
}
