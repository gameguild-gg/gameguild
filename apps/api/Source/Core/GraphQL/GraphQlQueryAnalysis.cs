namespace GameGuild.Core.GraphQL;

public class GraphQlQueryAnalysis {
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int Depth { get; private set; }
    public int Complexity { get; private set; }
    public int FieldCount { get; private set; }

    private GraphQlQueryAnalysis(bool isValid, string? errorMessage = null, int depth = 0, int complexity = 0, int fieldCount = 0) {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Depth = depth;
        Complexity = complexity;
        FieldCount = fieldCount;
    }

    public static GraphQlQueryAnalysis Valid(int depth, int complexity, int fieldCount) =>
        new(true, null, depth, complexity, fieldCount);

    public static GraphQlQueryAnalysis Invalid(string errorMessage) =>
        new(false, errorMessage);
}