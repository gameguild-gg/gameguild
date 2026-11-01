using System.Net;
using System.Text;

namespace GameGuild.Social.Blog.Services;

/// <summary>
/// Converts Ghost post HTML to Markdown for <see cref="BlogContentFormat.Markdown"/> blog posts
/// (issue #96 content migration). Backed by a small parser for Ghost's machine-generated,
/// well-formed HTML — not a general-purpose converter. The web Markdown pipeline renders GFM
/// (fences, tables) and passes through residual raw HTML, so imperfect conversions degrade
/// gracefully instead of losing content.
/// </summary>
public static class GhostHtmlToMarkdown
{
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img", "meta", "input", "source", "embed", "track", "wbr",
    };

    /// <summary>Converts a Ghost HTML body to Markdown. Empty input yields an empty string.</summary>
    public static string Convert(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = StripGhostCardComments(html);
        var nodes = HtmlNode.ParseFragment(text);
        var builder = new StringBuilder();
        foreach (var node in nodes)
        {
            RenderBlock(node, builder, 0);
        }

        return Normalize(builder);
    }

    private static string StripGhostCardComments(string html)
        => html
            .Replace("<!--kg-card-begin: markdown-->", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<!--kg-card-end: markdown-->", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<!--kg-card-begin: html-->", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<!--kg-card-end: html-->", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static void RenderBlock(HtmlNode node, StringBuilder builder, int listDepth)
    {
        switch (node.Tag)
        {
            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                var level = node.Tag[1] - '0';
                var heading = RenderInline(node.Children);
                if (heading.Length > 0)
                {
                    builder.Append(new string('#', level)).Append(' ').Append(heading).Append("\n\n");
                }

                break;
            case "p":
                var paragraph = RenderInline(node.Children);
                if (paragraph.Length > 0)
                {
                    builder.Append(paragraph).Append("\n\n");
                }

                break;
            case "ul":
            case "ol":
                RenderList(node, builder, listDepth);
                builder.Append('\n');
                break;
            case "li":
                // Stray li outside a list: render as a bullet at the current depth.
                builder.Append(' ', listDepth * 2).Append("- ").Append(RenderInline(node.Children)).Append('\n');
                break;
            case "blockquote":
                var quoted = new StringBuilder();
                foreach (var child in node.Children)
                {
                    RenderBlock(child, quoted, 0);
                }

                foreach (var line in quoted.ToString().Split('\n'))
                {
                    var trimmed = line.TrimEnd();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    builder.Append("> ").Append(trimmed).Append('\n');
                }

                builder.Append('\n');
                break;
            case "pre":
                var (language, code) = ExtractCode(node);
                builder.Append("```").Append(language).Append('\n')
                    .Append(code.TrimEnd('\n', '\r'))
                    .Append("\n```\n\n");
                break;
            case "table":
                RenderTable(node, builder);
                break;
            case "hr":
                builder.Append("---\n\n");
                break;
            case "figure":
                foreach (var child in node.Children)
                {
                    RenderBlock(child, builder, listDepth);
                }

                break;
            case "figcaption":
                var caption = RenderInline(node.Children);
                if (caption.Length > 0)
                {
                    builder.Append('*').Append(caption).Append("*\n\n");
                }

                break;
            case "img":
                var alt = node.Attribute("alt");
                builder.Append("![").Append(EscapeBracket(alt)).Append("](").Append(node.Attribute("src")).Append(")\n\n");
                break;
            case "":
                var text = WebUtility.HtmlDecode(node.Text).Trim();
                if (text.Length > 0)
                {
                    builder.Append(text).Append("\n\n");
                }

                break;
            default:
                // Unknown container (section, div, article, …): render children as blocks.
                foreach (var child in node.Children)
                {
                    RenderBlock(child, builder, listDepth);
                }

                break;
        }
    }

    private static void RenderList(HtmlNode list, StringBuilder builder, int depth)
    {
        var ordered = list.Tag == "ol";
        var index = 1;
        foreach (var item in list.Children.Where(child => child.Tag == "li"))
        {
            var marker = ordered ? $"{index++}. " : "- ";
            builder.Append(' ', depth * 2).Append(marker);

            var inlineParts = new StringBuilder();
            var nestedLists = new List<HtmlNode>();
            foreach (var child in item.Children)
            {
                if (child.Tag is "ul" or "ol")
                {
                    nestedLists.Add(child);
                }
                else
                {
                    RenderBlock(child, inlineParts, depth + 1);
                }
            }

            var inline = inlineParts.ToString().TrimEnd('\n');
            builder.Append(inline).Append('\n');
            foreach (var nested in nestedLists)
            {
                RenderList(nested, builder, depth + 1);
            }
        }
    }

    private static (string Language, string Code) ExtractCode(HtmlNode pre)
    {
        var codeNode = pre.Children.FirstOrDefault(child => child.Tag == "code") ?? pre;
        var cssClass = codeNode.Attribute("class");
        var language = cssClass?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(c => c.StartsWith("language-", StringComparison.Ordinal))
            ?[("language-".Length)..] ?? string.Empty;

        var code = new StringBuilder();
        AppendRawText(codeNode, code);
        return (language, WebUtility.HtmlDecode(code.ToString()));
    }

    private static void AppendRawText(HtmlNode node, StringBuilder builder)
    {
        if (node.Tag.Length == 0)
        {
            builder.Append(node.Text);
            return;
        }

        foreach (var child in node.Children)
        {
            AppendRawText(child, builder);
        }
    }

    private static void RenderTable(HtmlNode table, StringBuilder builder)
    {
        var rows = new List<List<string>>();
        var headerRow = -1;
        void Walk(HtmlNode node, bool inHead)
        {
            switch (node.Tag)
            {
                case "thead":
                    foreach (var child in node.Children)
                    {
                        Walk(child, inHead: true);
                    }

                    break;
                case "tr":
                    var cells = new List<string>();
                    foreach (var cell in node.Children.Where(child => child.Tag is "th" or "td"))
                    {
                        cells.Add(RenderInline(cell.Children).Trim());
                    }

                    if (inHead && headerRow == -1)
                    {
                        headerRow = rows.Count;
                    }

                    rows.Add(cells);
                    break;
                default:
                    foreach (var child in node.Children)
                    {
                        Walk(child, inHead);
                    }

                    break;
            }
        }

        Walk(table, inHead: false);
        if (rows.Count == 0)
        {
            return;
        }

        var columns = rows.Max(row => row.Count);
        if (headerRow == -1)
        {
            // No thead: promote the first row to the GFM header.
            headerRow = 0;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            builder.Append("| ");
            for (var column = 0; column < columns; column++)
            {
                var cell = column < rows[i].Count ? rows[i][column] : string.Empty;
                if (column > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(cell.Replace("|", "\\|", StringComparison.Ordinal)).Append(" |");
            }

            builder.Append('\n');
            if (i == headerRow)
            {
                builder.Append('|');
                for (var column = 0; column < columns; column++)
                {
                    builder.Append(" --- |");
                }

                builder.Append('\n');
            }
        }

        builder.Append('\n');
    }

    private static string RenderInline(IReadOnlyList<HtmlNode> nodes)
    {
        var builder = new StringBuilder();
        RenderInlineInto(nodes, builder);
        return builder.ToString().Trim();
    }

    private static void RenderInlineInto(IReadOnlyList<HtmlNode> nodes, StringBuilder builder)
    {
        foreach (var node in nodes)
        {
            switch (node.Tag)
            {
                case "":
                    builder.Append(WebUtility.HtmlDecode(node.Text));
                    break;
                case "br":
                    builder.Append('\n');
                    break;
                case "strong":
                case "b":
                    Wrap(node, builder, "**");
                    break;
                case "em":
                case "i":
                    Wrap(node, builder, "*");
                    break;
                case "del":
                case "s":
                case "strike":
                    Wrap(node, builder, "~~");
                    break;
                case "code":
                    var code = WebUtility.HtmlDecode(RawText(node)).Trim();
                    if (code.Length > 0)
                    {
                        builder.Append('`').Append(code.Replace("`", "'", StringComparison.Ordinal)).Append('`');
                    }

                    break;
                case "a":
                    var label = RenderInline(node.Children);
                    var href = node.Attribute("href");
                    if (label.Length == 0)
                    {
                        label = href;
                    }

                    if (href.Length > 0)
                    {
                        builder.Append('[').Append(label).Append("](").Append(href).Append(')');
                    }
                    else
                    {
                        builder.Append(label);
                    }

                    break;
                case "img":
                    builder.Append("![").Append(EscapeBracket(node.Attribute("alt"))).Append("](").Append(node.Attribute("src")).Append(')');
                    break;
                default:
                    RenderInlineInto(node.Children, builder);
                    break;
            }
        }
    }

    private static void Wrap(HtmlNode node, StringBuilder builder, string marker)
    {
        var inner = new StringBuilder();
        RenderInlineInto(node.Children, inner);
        var text = inner.ToString().Trim();
        if (text.Length > 0)
        {
            builder.Append(marker).Append(text).Append(marker);
        }
    }

    private static string RawText(HtmlNode node)
    {
        var builder = new StringBuilder();
        AppendRawText(node, builder);
        return builder.ToString();
    }

    private static string EscapeBracket(string value)
        => value.Replace("[", "%5B", StringComparison.Ordinal).Replace("]", "%5D", StringComparison.Ordinal);

    private static string Normalize(StringBuilder builder)
    {
        var markdown = builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        while (markdown.Contains("\n\n\n", StringComparison.Ordinal))
        {
            markdown = markdown.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        return markdown;
    }

    /// <summary>Minimal DOM node for well-formed HTML fragments (tag "" = text node).</summary>
    internal sealed class HtmlNode
    {
        public string Tag { get; private init; } = string.Empty;

        public string Text { get; private init; } = string.Empty;

        public List<HtmlNode> Children { get; } = [];

        private Dictionary<string, string> Attributes { get; set; } = [];

        public string Attribute(string name)
            => Attributes.TryGetValue(name.ToLowerInvariant(), out var value) ? value : string.Empty;

        public static List<HtmlNode> ParseFragment(string html)
        {
            var nodes = new List<HtmlNode>();
            var index = 0;
            ParseSiblings(html, ref index, null, nodes);
            return nodes;
        }

        private static void ParseSiblings(string html, ref int index, string? untilTag, List<HtmlNode> siblings)
        {
            while (index < html.Length)
            {
                var open = html.IndexOf('<', index);
                if (open < 0)
                {
                    AppendText(siblings, html[index..]);
                    index = html.Length;
                    break;
                }

                if (open > index)
                {
                    AppendText(siblings, html[index..open]);
                }

                if (html.AsSpan(open).StartsWith("<!--", StringComparison.Ordinal))
                {
                    var endComment = html.IndexOf("-->", open, StringComparison.Ordinal);
                    index = endComment < 0 ? html.Length : endComment + 3;
                    continue;
                }

                var close = html.IndexOf('>', open);
                if (close < 0)
                {
                    AppendText(siblings, html[open..]);
                    index = html.Length;
                    break;
                }

                var tagContent = html[(open + 1)..close].Trim();
                index = close + 1;

                if (tagContent.StartsWith('/'))
                {
                    var closingName = tagContent[1..].Trim();
                    if (untilTag is not null && string.Equals(closingName, untilTag, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    continue;
                }

                var (name, attributes, selfClosing) = ParseTag(tagContent);
                if (name.Length == 0)
                {
                    continue;
                }

                var node = new HtmlNode { Tag = name.ToLowerInvariant(), Attributes = attributes };
                siblings.Add(node);
                if (selfClosing || VoidElements.Contains(name))
                {
                    continue;
                }

                ParseSiblings(html, ref index, node.Tag, node.Children);
            }
        }

        private static (string Name, Dictionary<string, string> Attributes, bool SelfClosing) ParseTag(string content)
        {
            var space = content.IndexOf(' ');
            var name = (space < 0 ? content : content[..space]).Trim();
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var selfClosing = content.EndsWith('/');

            var rest = space < 0 ? string.Empty : content[space..].TrimEnd('/');
            var attributeIndex = 0;
            while (attributeIndex < rest.Length)
            {
                while (attributeIndex < rest.Length && char.IsWhiteSpace(rest[attributeIndex]))
                {
                    attributeIndex++;
                }

                var keyStart = attributeIndex;
                while (attributeIndex < rest.Length && !char.IsWhiteSpace(rest[attributeIndex]) && rest[attributeIndex] != '=')
                {
                    attributeIndex++;
                }

                if (attributeIndex == keyStart)
                {
                    break;
                }

                var key = rest[keyStart..attributeIndex].ToLowerInvariant();
                var value = string.Empty;
                while (attributeIndex < rest.Length && char.IsWhiteSpace(rest[attributeIndex]))
                {
                    attributeIndex++;
                }

                if (attributeIndex < rest.Length && rest[attributeIndex] == '=')
                {
                    attributeIndex++;
                    while (attributeIndex < rest.Length && char.IsWhiteSpace(rest[attributeIndex]))
                    {
                        attributeIndex++;
                    }

                    if (attributeIndex < rest.Length && rest[attributeIndex] is '"' or '\'')
                    {
                        var quote = rest[attributeIndex];
                        var valueEnd = rest.IndexOf(quote, attributeIndex + 1);
                        if (valueEnd < 0)
                        {
                            valueEnd = rest.Length - 1;
                        }

                        value = rest[(attributeIndex + 1)..valueEnd];
                        attributeIndex = valueEnd + 1;
                    }
                    else
                    {
                        var valueStart = attributeIndex;
                        while (attributeIndex < rest.Length && !char.IsWhiteSpace(rest[attributeIndex]))
                        {
                            attributeIndex++;
                        }

                        value = rest[valueStart..attributeIndex];
                    }
                }

                attributes[key] = WebUtility.HtmlDecode(value);
            }

            return (name, attributes, selfClosing);
        }

        private static void AppendText(List<HtmlNode> siblings, string text)
        {
            if (text.Length > 0)
            {
                siblings.Add(new HtmlNode { Text = text });
            }
        }
    }
}
