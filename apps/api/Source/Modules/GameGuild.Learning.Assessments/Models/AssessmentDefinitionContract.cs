using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GameGuild.Learning.Assessments;

public static class AssessmentDefinitionContract
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string NormalizeDefinition(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return EmptyDefinitionPayload();
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            ValidateBlockStorage(document.RootElement);
            return document.RootElement.GetRawText();
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Assessment definition must be valid JSON.", nameof(payload), exception);
        }
    }

    public static JsonElement AuthorDefinition(string? payload)
    {
        return ParseElement(NormalizeDefinition(payload));
    }

    public static JsonElement LearnerDefinition(string? payload, Guid attemptSeed)
    {
        var root = JsonNode.Parse(NormalizeDefinition(payload)) as JsonObject ?? EmptyDefinitionObject();
        var blocks = root["blocks"] as JsonObject;
        if (blocks == null)
        {
            return ParseElement(root.ToJsonString(JsonOptions));
        }

        foreach (var (blockId, type, block) in EnumerateBlocks(root, blocks))
        {
            if (!string.Equals(type, "quiz", StringComparison.OrdinalIgnoreCase) || block is not JsonObject quiz)
            {
                continue;
            }

            RedactQuizEntry(quiz, attemptSeed, blockId);
        }

        return ParseElement(root.ToJsonString(JsonOptions));
    }

    public static bool TryGradeDeterministicQuiz(
        string? definitionPayload,
        string? structuredAnswerPayload,
        int maxScore,
        out int score,
        out string? feedback)
    {
        score = 0;
        feedback = null;

        if (string.IsNullOrWhiteSpace(structuredAnswerPayload))
        {
            return false;
        }

        JsonElement answerRoot;
        JsonElement definitionRoot;
        try
        {
            answerRoot = ParseElement(structuredAnswerPayload);
            definitionRoot = ParseElement(NormalizeDefinition(definitionPayload));
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!TryGetBlocksObject(definitionRoot, out var blocks))
        {
            return false;
        }

        var quizBlocks = EnumerateQuizBlocks(definitionRoot, blocks).ToArray();
        if (quizBlocks.Length == 0)
        {
            return false;
        }

        var earned = 0m;
        var possible = 0m;

        foreach (var (blockId, entry) in quizBlocks)
        {
            if (!TryGetAnswerForBlock(answerRoot, blockId, quizBlocks.Length == 1, out var answer))
            {
                possible += GetPoints(entry);
                continue;
            }

            var points = GetPoints(entry);
            var graded = TryGradeEntry(entry, answer, out var correct);
            if (!graded)
            {
                score = 0;
                feedback = null;
                return false;
            }

            possible += points;
            if (correct) earned += points;
        }

        if (possible <= 0)
        {
            return false;
        }

        score = (int)Math.Round((earned / possible) * maxScore, MidpointRounding.AwayFromZero);
        score = Math.Clamp(score, 0, maxScore);
        feedback = $"{earned:0.##}/{possible:0.##} deterministic quiz points";
        return true;
    }

    private static void ValidateBlockStorage(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Assessment definition must be a JSON object.");
        }

        if (!root.TryGetProperty("order", out var order) || order.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Assessment definition must include an order array.");
        }

        if (!root.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Assessment definition must include a blocks object.");
        }
    }

    private static string EmptyDefinitionPayload()
    {
        return EmptyDefinitionObject().ToJsonString(JsonOptions);
    }

    private static JsonObject EmptyDefinitionObject()
    {
        return new JsonObject
        {
            ["order"] = new JsonArray(),
            ["blocks"] = new JsonObject()
        };
    }

    private static JsonElement ParseElement(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    private static bool TryGetBlocksObject(JsonElement root, out JsonElement blocks)
    {
        blocks = default;
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty("blocks", out blocks) &&
               blocks.ValueKind == JsonValueKind.Object;
    }

    private static IEnumerable<(string BlockId, string Type, JsonNode? Block)> EnumerateBlocks(JsonObject root, JsonObject blocks)
    {
        if (root["order"] is not JsonArray order)
        {
            yield break;
        }

        foreach (var entry in order)
        {
            if (entry is not JsonArray pair || pair.Count < 2)
            {
                continue;
            }

            var blockId = pair[0]?.GetValue<string>();
            var type = pair[1]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(blockId) || string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            yield return (blockId, type, blocks[blockId]);
        }
    }

    private static IEnumerable<(string BlockId, JsonElement Entry)> EnumerateQuizBlocks(JsonElement root, JsonElement blocks)
    {
        if (!root.TryGetProperty("order", out var order) || order.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in order.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
            {
                continue;
            }

            var blockId = item[0].GetString();
            var type = item[1].GetString();
            if (string.IsNullOrWhiteSpace(blockId) ||
                !string.Equals(type, "quiz", StringComparison.OrdinalIgnoreCase) ||
                !blocks.TryGetProperty(blockId, out var entry) ||
                entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            yield return (blockId, entry);
        }
    }

    private static void RedactQuizEntry(JsonObject entry, Guid attemptSeed, string blockId)
    {
        var type = entry["type"]?.GetValue<string>();
        switch (type)
        {
            case "SINGLE_CHOICE":
                entry.Remove("correctOptionId");
                ShuffleArrayProperty(entry, "options", attemptSeed, $"{blockId}:options");
                break;
            case "MULTIPLE_CHOICE":
                entry.Remove("correctOptionIds");
                ShuffleArrayProperty(entry, "options", attemptSeed, $"{blockId}:options");
                break;
            case "TRUE_FALSE":
                entry.Remove("correctAnswer");
                break;
            case "FILL_IN_THE_BLANK":
                RedactFillBlank(entry, attemptSeed, blockId);
                break;
            case "SHORT_ANSWER":
                entry.Remove("acceptedAnswers");
                entry.Remove("caseSensitive");
                break;
            case "ESSAY":
                entry.Remove("correctAnswer");
                entry.Remove("correctAnswerPlain");
                entry.Remove("requireFormatting");
                break;
            case "MATCHING":
                RedactMatching(entry, attemptSeed, blockId);
                break;
            case "ORDERING":
                RedactOrdering(entry, attemptSeed, blockId);
                break;
            case "CATEGORIZATION":
                RedactCategorization(entry, attemptSeed, blockId);
                break;
            case "RATING":
                entry.Remove("correctRating");
                break;
            case "NUMERIC":
            case "FORMULA":
                entry.Remove("formula");
                break;
            case "HOTSPOT":
                entry.Remove("hotspots");
                break;
            case "HIGHLIGHT":
                entry.Remove("sourceText");
                entry.Remove("highlights");
                break;
        }
    }

    private static void RedactFillBlank(JsonObject entry, Guid attemptSeed, string blockId)
    {
        if (entry["blanks"] is not JsonArray blanks)
        {
            return;
        }

        foreach (var blank in blanks.OfType<JsonObject>())
        {
            if (blank["input"] is not JsonObject input)
            {
                continue;
            }

            switch (input["type"]?.GetValue<string>())
            {
                case "TEXT":
                    input.Remove("acceptedAnswers");
                    input.Remove("caseSensitive");
                    break;
                case "NUMBER":
                    input.Remove("correctValue");
                    input.Remove("tolerance");
                    input.Remove("requiredPrecision");
                    break;
                case "DROPDOWN":
                    ShuffleArrayProperty(input, "options", attemptSeed, $"{blockId}:{blank["id"]}:options");
                    break;
                case "WORDBANK":
                    ShuffleArrayProperty(input, "words", attemptSeed, $"{blockId}:{blank["id"]}:words");
                    break;
            }
        }
    }

    private static void RedactMatching(JsonObject entry, Guid attemptSeed, string blockId)
    {
        if (entry["pairs"] is not JsonArray pairs)
        {
            return;
        }

        var rightOptions = new JsonArray();
        foreach (var pair in pairs.OfType<JsonObject>())
        {
            if (pair["right"]?.DeepClone() is { } right)
            {
                rightOptions.Add(right);
            }
            pair.Remove("right");
        }

        if (entry["distractors"] is JsonArray distractors)
        {
            foreach (var distractor in distractors)
            {
                if (distractor?.DeepClone() is { } clone)
                {
                    rightOptions.Add(clone);
                }
            }
            entry.Remove("distractors");
        }

        entry["rightOptions"] = ShuffleArray(rightOptions, attemptSeed, $"{blockId}:rightOptions");
    }

    private static void RedactOrdering(JsonObject entry, Guid attemptSeed, string blockId)
    {
        if (entry["items"] is not JsonArray items)
        {
            return;
        }

        foreach (var item in items.OfType<JsonObject>())
        {
            item.Remove("correctPosition");
        }

        entry["items"] = ShuffleArray(items, attemptSeed, $"{blockId}:items");
    }

    private static void RedactCategorization(JsonObject entry, Guid attemptSeed, string blockId)
    {
        if (entry["items"] is not JsonArray items)
        {
            return;
        }

        foreach (var item in items.OfType<JsonObject>())
        {
            item.Remove("correctCategoryIds");
        }

        entry["items"] = ShuffleArray(items, attemptSeed, $"{blockId}:items");
    }

    private static void ShuffleArrayProperty(JsonObject parent, string propertyName, Guid attemptSeed, string salt)
    {
        if (parent[propertyName] is JsonArray array)
        {
            parent[propertyName] = ShuffleArray(array, attemptSeed, salt);
        }
    }

    private static JsonArray ShuffleArray(JsonArray source, Guid attemptSeed, string salt)
    {
        var items = source.Select(item => item?.DeepClone()).ToList();
        var random = CreateDeterministicRandom(attemptSeed, salt);
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        var shuffled = new JsonArray();
        foreach (var item in items)
        {
            shuffled.Add(item);
        }

        return shuffled;
    }

    private static Random CreateDeterministicRandom(Guid attemptSeed, string salt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{attemptSeed:N}:{salt}"));
        return new Random(BitConverter.ToInt32(hash, 0));
    }

    private static decimal GetPoints(JsonElement entry)
    {
        if (entry.TryGetProperty("points", out var points) &&
            points.ValueKind == JsonValueKind.Number &&
            points.TryGetDecimal(out var value) &&
            value > 0)
        {
            return value;
        }

        return 1m;
    }

    private static bool TryGetAnswerForBlock(JsonElement answerRoot, string blockId, bool isSingleBlock, out JsonElement answer)
    {
        answer = default;
        if (answerRoot.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var answersProperty in new[] { "answers", "answersByBlockId" })
        {
            if (answerRoot.TryGetProperty(answersProperty, out var answers) &&
                answers.ValueKind == JsonValueKind.Object &&
                answers.TryGetProperty(blockId, out answer) &&
                answer.ValueKind == JsonValueKind.Object)
            {
                return true;
            }
        }

        if (!isSingleBlock)
        {
            return false;
        }

        answer = answerRoot;
        return HasAnswerShape(answerRoot);
    }

    private static bool HasAnswerShape(JsonElement answer)
    {
        return answer.TryGetProperty("selectedOptionIds", out _) ||
               answer.TryGetProperty("textAnswers", out _) ||
               answer.TryGetProperty("categorizations", out _) ||
               answer.TryGetProperty("ordering", out _) ||
               answer.TryGetProperty("rating", out _);
    }

    private static bool TryGradeEntry(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        var type = entry.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        switch (type)
        {
            case "SINGLE_CHOICE":
                return TryGradeSingleChoice(entry, answer, out correct);
            case "MULTIPLE_CHOICE":
                return TryGradeMultipleChoice(entry, answer, out correct);
            case "TRUE_FALSE":
                return TryGradeTrueFalse(entry, answer, out correct);
            case "FILL_IN_THE_BLANK":
                return TryGradeFillBlank(entry, answer, out correct);
            case "SHORT_ANSWER":
                return TryGradeShortAnswer(entry, answer, out correct);
            case "MATCHING":
                return TryGradeMatching(entry, answer, out correct);
            case "ORDERING":
                return TryGradeOrdering(entry, answer, out correct);
            case "CATEGORIZATION":
                return TryGradeCategorization(entry, answer, out correct);
            case "RATING":
                return TryGradeRating(entry, answer, out correct);
            case "HOTSPOT":
                return TryGradeHotspot(entry, answer, out correct);
            case "HIGHLIGHT":
                return TryGradeHighlight(entry, answer, out correct);
            default:
                return false;
        }
    }

    private static bool TryGradeSingleChoice(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = TryGetStringArray(answer, "selectedOptionIds", out var selected) &&
                  entry.TryGetProperty("correctOptionId", out var correctOption) &&
                  selected.FirstOrDefault() == correctOption.GetString();
        return true;
    }

    private static bool TryGradeMultipleChoice(JsonElement entry, JsonElement answer, out bool correct)
    {
        var selected = TryGetStringArray(answer, "selectedOptionIds", out var selectedIds) ? selectedIds : Array.Empty<string>();
        var expected = TryGetStringArray(entry, "correctOptionIds", out var correctIds) ? correctIds : Array.Empty<string>();
        correct = SetEquals(selected, expected);
        return true;
    }

    private static bool TryGradeTrueFalse(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        if (!TryGetStringArray(answer, "selectedOptionIds", out var selected) ||
            !entry.TryGetProperty("correctAnswer", out var correctAnswer) ||
            correctAnswer.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return true;
        }

        correct = selected.FirstOrDefault() == (correctAnswer.GetBoolean() ? "true" : "false");
        return true;
    }

    private static bool TryGradeFillBlank(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        if (!entry.TryGetProperty("blanks", out var blanks) || blanks.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        if (!TryGetObject(answer, "textAnswers", out var textAnswers))
        {
            return true;
        }

        correct = blanks.EnumerateArray().All(blank => IsFillBlankCorrect(blank, textAnswers));
        return true;
    }

    private static bool IsFillBlankCorrect(JsonElement blank, JsonElement textAnswers)
    {
        if (!blank.TryGetProperty("id", out var idElement) || idElement.GetString() is not { Length: > 0 } blankId)
        {
            return false;
        }

        var rawAnswer = TryGetString(textAnswers, blankId, out var answer) ? answer.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(rawAnswer) ||
            !blank.TryGetProperty("input", out var input) ||
            input.ValueKind != JsonValueKind.Object ||
            !input.TryGetProperty("type", out var typeElement))
        {
            return false;
        }

        return typeElement.GetString() switch
        {
            "TEXT" => MatchesAcceptedAnswers(rawAnswer, input),
            "NUMBER" => MatchesNumberBlank(rawAnswer, input),
            "DROPDOWN" => TryGetStringArray(input, "options", out var options) && rawAnswer == options.FirstOrDefault(),
            "WORDBANK" => TryGetStringArray(input, "words", out var words) && StripWordBankToken(rawAnswer) == words.FirstOrDefault(),
            _ => false
        };
    }

    private static bool TryGradeShortAnswer(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = TryGetObject(answer, "textAnswers", out var textAnswers) &&
                  TryGetString(textAnswers, "main", out var rawAnswer) &&
                  MatchesAcceptedAnswers(rawAnswer.Trim(), entry);
        return true;
    }

    private static bool TryGradeMatching(JsonElement entry, JsonElement answer, out bool correct)
    {
        var assignments = new Dictionary<string, string>(StringComparer.Ordinal);
        if (TryGetStringArray(answer, "selectedOptionIds", out var selected))
        {
            foreach (var pair in selected.Select(ParseMatchingAssignment).Where(pair => pair.LeftId.Length > 0))
            {
                assignments[pair.LeftId] = pair.RightValue;
            }
        }

        if (!entry.TryGetProperty("pairs", out var pairs) || pairs.ValueKind != JsonValueKind.Array)
        {
            correct = false;
            return true;
        }

        correct = assignments.Count == pairs.GetArrayLength() &&
                  pairs.EnumerateArray().All(pair =>
                      TryGetString(pair, "id", out var id) &&
                      TryGetString(pair, "right", out var right) &&
                      assignments.TryGetValue(id, out var selectedRight) &&
                      selectedRight == right);
        return true;
    }

    private static bool TryGradeOrdering(JsonElement entry, JsonElement answer, out bool correct)
    {
        var submittedOrder = TryGetStringArray(answer, "ordering", out var ordering) ? ordering : Array.Empty<string>();
        var correctOrder = entry.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
            ? items.EnumerateArray()
                .Select(item => new
                {
                    Id = TryGetString(item, "id", out var id) ? id : string.Empty,
                    Position = TryGetInt(item, "correctPosition", out var position) ? position : int.MaxValue
                })
                .Where(item => item.Id.Length > 0)
                .OrderBy(item => item.Position)
                .Select(item => item.Id)
                .ToArray()
            : Array.Empty<string>();

        correct = submittedOrder.SequenceEqual(correctOrder);
        return true;
    }

    private static bool TryGradeCategorization(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        if (!entry.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        if (!TryGetObject(answer, "categorizations", out var categorizations))
        {
            return true;
        }

        correct = items.EnumerateArray().All(item =>
            TryGetString(item, "id", out var id) &&
            TryGetStringArray(item, "correctCategoryIds", out var expected) &&
            TryGetStringArray(categorizations, id, out var submitted) &&
            SetEquals(submitted, expected));
        return true;
    }

    private static bool TryGradeRating(JsonElement entry, JsonElement answer, out bool correct)
    {
        if (!entry.TryGetProperty("correctRating", out var expected))
        {
            correct = answer.TryGetProperty("rating", out var anyRating) && anyRating.ValueKind == JsonValueKind.Number;
            return true;
        }

        correct = answer.TryGetProperty("rating", out var rating) &&
                  rating.ValueKind == JsonValueKind.Number &&
                  expected.ValueKind == JsonValueKind.Number &&
                  rating.GetDecimal() == expected.GetDecimal();
        return true;
    }

    private static bool TryGradeHotspot(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        if (!TryGetObject(answer, "textAnswers", out var textAnswers) ||
            !TryGetString(textAnswers, "hotspot_x", out var xRaw) ||
            !TryGetString(textAnswers, "hotspot_y", out var yRaw) ||
            !decimal.TryParse(xRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var x) ||
            !decimal.TryParse(yRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var y) ||
            !TryGetDecimal(entry, "imageWidth", out var imageWidth) ||
            !TryGetDecimal(entry, "imageHeight", out var imageHeight) ||
            !entry.TryGetProperty("hotspots", out var hotspots) ||
            hotspots.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        correct = hotspots.EnumerateArray().Any(hotspot => IsInsideHotspot(x, y, imageWidth, imageHeight, hotspot));
        return true;
    }

    private static bool IsInsideHotspot(decimal x, decimal y, decimal imageWidth, decimal imageHeight, JsonElement hotspot)
    {
        if (!TryGetDecimal(hotspot, "x", out var hx) ||
            !TryGetDecimal(hotspot, "y", out var hy) ||
            !hotspot.TryGetProperty("zones", out var zones) ||
            zones.ValueKind != JsonValueKind.Array ||
            zones.GetArrayLength() == 0)
        {
            return false;
        }

        var outerRadius = zones.EnumerateArray()
            .Select(zone => TryGetDecimal(zone, "radius", out var radius) ? radius : 0m)
            .DefaultIfEmpty(0m)
            .Max();
        var dx = (double)((x - hx) / 100m * imageWidth);
        var dy = (double)((y - hy) / 100m * imageHeight);
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var threshold = (double)(outerRadius / 100m * imageWidth);
        return distance <= threshold;
    }

    private static bool TryGradeHighlight(JsonElement entry, JsonElement answer, out bool correct)
    {
        correct = false;
        if (!TryGetObject(answer, "textAnswers", out var textAnswers) ||
            !TryGetString(textAnswers, "highlight_spans", out var rawSpans) ||
            !entry.TryGetProperty("highlights", out var expectedSpans) ||
            expectedSpans.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(rawSpans);
            var submittedSpans = document.RootElement;
            if (submittedSpans.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            var expected = expectedSpans.EnumerateArray().Select(ReadSpan).Where(span => span.HasValue).Select(span => span!.Value).ToArray();
            var submitted = submittedSpans.EnumerateArray().Select(ReadSpan).Where(span => span.HasValue).Select(span => span!.Value).ToArray();
            correct = expected.Length == 0
                ? submitted.Length == 0
                : expected.All(expectedSpan => submitted.Any(span => Overlaps(span, expectedSpan))) &&
                  submitted.All(span => expected.Any(expectedSpan => Overlaps(span, expectedSpan)));
            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static (int Start, int End)? ReadSpan(JsonElement span)
    {
        return TryGetInt(span, "start", out var start) && TryGetInt(span, "end", out var end)
            ? (start, end)
            : null;
    }

    private static bool Overlaps((int Start, int End) a, (int Start, int End) b)
    {
        return a.Start < b.End && a.End > b.Start;
    }

    private static bool MatchesAcceptedAnswers(string rawAnswer, JsonElement source)
    {
        if (!TryGetStringArray(source, "acceptedAnswers", out var acceptedAnswers))
        {
            return false;
        }

        var caseSensitive = TryGetBool(source, "caseSensitive", out var value) && value;
        return acceptedAnswers.Any(accepted =>
            caseSensitive
                ? rawAnswer == accepted
                : string.Equals(rawAnswer, accepted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesNumberBlank(string rawAnswer, JsonElement input)
    {
        if (!TryGetDecimal(input, "correctValue", out var correctValue))
        {
            return false;
        }

        var numericText = rawAnswer;
        if (TryGetString(input, "unit", out var unit) && !string.IsNullOrWhiteSpace(unit))
        {
            var suffixPattern = $@"\s*{Regex.Escape(unit)}\s*$";
            numericText = Regex.Replace(numericText, suffixPattern, string.Empty).Trim();
            if (TryGetBool(input, "requireUnit", out var requireUnit) && requireUnit && numericText == rawAnswer)
            {
                return false;
            }
        }

        if (!decimal.TryParse(numericText, NumberStyles.Number, CultureInfo.InvariantCulture, out var submitted))
        {
            return false;
        }

        if (TryGetBool(input, "allowNegative", out var allowNegative) && !allowNegative && submitted < 0)
        {
            return false;
        }

        if (TryGetInt(input, "requiredPrecision", out var precision))
        {
            var decimalPart = numericText.Contains('.') ? numericText.Split('.')[1] : string.Empty;
            if (decimalPart.Length != precision)
            {
                return false;
            }
        }

        var tolerance = TryGetDecimal(input, "tolerance", out var configuredTolerance) ? configuredTolerance : 0m;
        return Math.Abs(submitted - correctValue) <= tolerance;
    }

    private static (string LeftId, string RightValue) ParseMatchingAssignment(string selection)
    {
        var separator = selection.IndexOf(':', StringComparison.Ordinal);
        return separator <= 0
            ? (string.Empty, string.Empty)
            : (selection[..separator], selection[(separator + 1)..]);
    }

    private static string StripWordBankToken(string value)
    {
        var separator = value.IndexOf('|', StringComparison.Ordinal);
        return separator >= 0 ? value[..separator] : value;
    }

    private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value)
    {
        value = default;
        return parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out value) &&
               value.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetString(JsonElement parent, string propertyName, out string value)
    {
        value = string.Empty;
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetInt(JsonElement parent, string propertyName, out int value)
    {
        value = 0;
        return parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }

    private static bool TryGetDecimal(JsonElement parent, string propertyName, out decimal value)
    {
        value = 0m;
        return parent.ValueKind == JsonValueKind.Object &&
               parent.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetDecimal(out value);
    }

    private static bool TryGetBool(JsonElement parent, string propertyName, out bool value)
    {
        value = false;
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }

    private static bool TryGetStringArray(JsonElement parent, string propertyName, out string[] values)
    {
        values = [];
        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        values = array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        return true;
    }

    private static bool SetEquals(string[] left, string[] right)
    {
        return left.Length == right.Length && left.ToHashSet(StringComparer.Ordinal).SetEquals(right);
    }
}
