import {
  FileText,
  List,
  CheckSquare,
  Table as TableIcon,
  Code,
  Quote,
  Image as ImageIcon,
  Link as LinkIcon,
  Heading,
  AlertTriangle,
  Lightbulb,
  BookOpen,
} from "lucide-react"

export interface MarkdownTemplate {
  id: string
  title: string
  description: string
  category: string
  code: string
  icon: any
}

export const markdownTemplates: MarkdownTemplate[] = [
  // Structure (6)
  {
    id: "headers",
    title: "Headers",
    description: "All heading levels H1-H6",
    category: "structure",
    icon: Heading,
    code: `# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6`,
  },
  {
    id: "document",
    title: "Document",
    description: "Basic document structure",
    category: "structure",
    icon: FileText,
    code: `# Document Title

## Introduction
Brief introduction here.

## Main Content
Your main content.

## Conclusion
Final thoughts.`,
  },
  {
    id: "article",
    title: "Article",
    description: "Article with metadata",
    category: "structure",
    icon: FileText,
    code: `# Article Title

**Author:** Your Name  
**Date:** ${new Date().toLocaleDateString()}  
**Reading Time:** 5 min

---

Brief summary here.

## Content
Article content.`,
  },
  {
    id: "faq",
    title: "FAQ",
    description: "Question and answer format",
    category: "structure",
    icon: FileText,
    code: `# FAQ

### What is this?
Answer to question 1.

### How do I use it?
Answer to question 2.

### Where can I learn more?
Answer to question 3.`,
  },
  {
    id: "changelog",
    title: "Changelog",
    description: "Version history",
    category: "structure",
    icon: FileText,
    code: `# Changelog

## [1.0.0] - ${new Date().toISOString().split('T')[0]}

### Added
- New feature

### Changed
- Modified behavior

### Fixed
- Bug fix`,
  },
  {
    id: "tutorial",
    title: "Tutorial",
    description: "Step-by-step guide",
    category: "structure",
    icon: FileText,
    code: `# Tutorial Title

## Prerequisites
- Requirement 1
- Requirement 2

## Step 1
Instructions here.

## Step 2
More instructions.

## Summary
What you learned.`,
  },

  // Lists (5)
  {
    id: "unordered-list",
    title: "Bullet List",
    description: "Simple bullet points",
    category: "lists",
    icon: List,
    code: `- Item 1
- Item 2
  - Subitem 2.1
  - Subitem 2.2
- Item 3`,
  },
  {
    id: "ordered-list",
    title: "Numbered List",
    description: "Ordered items",
    category: "lists",
    icon: List,
    code: `1. First item
2. Second item
   1. Subitem 2.1
   2. Subitem 2.2
3. Third item`,
  },
  {
    id: "task-list",
    title: "Task List",
    description: "Checkable todos",
    category: "lists",
    icon: CheckSquare,
    code: `### Tasks

- [x] Completed task
- [ ] Pending task 1
- [ ] Pending task 2
- [ ] Pending task 3`,
  },
  {
    id: "priority-tasks",
    title: "Priority Tasks",
    description: "Tasks by priority",
    category: "lists",
    icon: CheckSquare,
    code: `### High Priority
- [ ] Critical task 1
- [ ] Critical task 2

### Medium Priority
- [ ] Regular task 1

### Low Priority
- [ ] Optional task 1`,
  },
  {
    id: "pros-cons",
    title: "Pros & Cons",
    description: "Advantages and disadvantages",
    category: "lists",
    icon: List,
    code: `### ✅ Pros
- Advantage 1
- Advantage 2
- Advantage 3

### ❌ Cons
- Disadvantage 1
- Disadvantage 2`,
  },

  // Tables (4)
  {
    id: "simple-table",
    title: "Simple Table",
    description: "Basic two-column table",
    category: "tables",
    icon: TableIcon,
    code: `| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |`,
  },
  {
    id: "aligned-table",
    title: "Aligned Table",
    description: "Left, center, right alignment",
    category: "tables",
    icon: TableIcon,
    code: `| Left | Center | Right |
|:-----|:------:|------:|
| A    | B      | C     |
| 1    | 2      | 3     |`,
  },
  {
    id: "comparison-table",
    title: "Comparison",
    description: "Feature comparison",
    category: "tables",
    icon: TableIcon,
    code: `| Feature | Plan A | Plan B | Plan C |
|---------|--------|--------|--------|
| Price   | $10    | $20    | $30    |
| Users   | 1      | 5      | 10     |
| Storage | 10GB   | 50GB   | 100GB  |`,
  },
  {
    id: "status-table",
    title: "Status Table",
    description: "Progress tracking",
    category: "tables",
    icon: TableIcon,
    code: `| Task | Status | Progress |
|------|:------:|---------:|
| A    | ✓ Done | 100%     |
| B    | ⏳ WIP | 60%      |
| C    | ❌ Todo | 0%      |`,
  },

  // Code (5)
  {
    id: "inline-code",
    title: "Inline Code",
    description: "Code within text",
    category: "code",
    icon: Code,
    code: `Use \`const\` for constants and \`let\` for variables.

Example: \`const name = "John";\``,
  },
  {
    id: "code-block",
    title: "Code Block",
    description: "Multi-line code",
    category: "code",
    icon: Code,
    code: `\`\`\`javascript
function greet(name) {
  const message = \`Hello, \${name}!\`;
  console.log(message);
  return message;
}

greet("World");
\`\`\``,
  },
  {
    id: "code-diff",
    title: "Code Before/After",
    description: "Show code changes",
    category: "code",
    icon: Code,
    code: `**Before:**
\`\`\`javascript
var x = 10;
\`\`\`

**After:**
\`\`\`javascript
const x = 10;
\`\`\``,
  },
  {
    id: "multi-lang",
    title: "Multi-Language",
    description: "Multiple languages",
    category: "code",
    icon: Code,
    code: `**Python:**
\`\`\`python
print("Hello")
\`\`\`

**JavaScript:**
\`\`\`javascript
console.log("Hello");
\`\`\``,
  },
  {
    id: "code-output",
    title: "Code + Output",
    description: "Code with result",
    category: "code",
    icon: Code,
    code: `\`\`\`javascript
2 + 2
\`\`\`

Output:
\`\`\`
4
\`\`\``,
  },

  // Formatting (5)
  {
    id: "text-emphasis",
    title: "Text Styles",
    description: "Bold, italic, strikethrough",
    category: "formatting",
    icon: FileText,
    code: `**Bold text**

*Italic text*

***Bold italic***

~~Strikethrough~~`,
  },
  {
    id: "blockquote",
    title: "Blockquote",
    description: "Quoted text",
    category: "formatting",
    icon: Quote,
    code: `> This is a quote.
> It can span lines.
> 
> And paragraphs.`,
  },
  {
    id: "callout-info",
    title: "Info Callout",
    description: "Information box",
    category: "formatting",
    icon: Quote,
    code: `> ℹ️ **Information**
> 
> Important info here.`,
  },
  {
    id: "callout-warning",
    title: "Warning",
    description: "Warning message",
    category: "formatting",
    icon: AlertTriangle,
    code: `> ⚠️ **Warning**
> 
> Caution message here.`,
  },
  {
    id: "callout-tip",
    title: "Tip",
    description: "Helpful tip",
    category: "formatting",
    icon: Lightbulb,
    code: `> 💡 **Tip**
> 
> Helpful tip here.`,
  },
  {
    id: "callout-note",
    title: "Note",
    description: "Important note",
    category: "formatting",
    icon: BookOpen,
    code: `> 📝 **Note**
> 
> Important note here.`,
  },
  {
    id: "horizontal-rule",
    title: "Divider",
    description: "Section separator",
    category: "formatting",
    icon: FileText,
    code: `Section 1

---

Section 2`,
  },

  // Media (3)
  {
    id: "link",
    title: "Link",
    description: "Hyperlink",
    category: "media",
    icon: LinkIcon,
    code: `[Link text](https://example.com)`,
  },
  {
    id: "image",
    title: "Image",
    description: "Embedded image",
    category: "media",
    icon: ImageIcon,
    code: `![Description](https://via.placeholder.com/400)

*Figure: Caption here*`,
  },
  {
    id: "image-link",
    title: "Linked Image",
    description: "Clickable image",
    category: "media",
    icon: ImageIcon,
    code: `[![Alt text](https://via.placeholder.com/400)](https://example.com)`,
  },
  {
    id: "link-list",
    title: "Link List",
    description: "Multiple links",
    category: "media",
    icon: LinkIcon,
    code: `## Resources

- [Link 1](https://example.com) - Description
- [Link 2](https://example.com) - Description
- [Link 3](https://example.com) - Description`,
  },
  {
    id: "youtube-embed",
    title: "Video Embed",
    description: "Embedded video guide",
    category: "media",
    icon: ImageIcon,
    code: `## Video Title

[![Watch Video](https://img.youtube.com/vi/VIDEO_ID/0.jpg)](https://www.youtube.com/watch?v=VIDEO_ID)

*Click to watch on YouTube*`,
  },

  // Advanced (2)
  {
    id: "definition-list",
    title: "Definitions",
    description: "Term definitions",
    category: "advanced",
    icon: FileText,
    code: `**Term 1**
: Definition here.

**Term 2**
: Another definition.`,
  },
  {
    id: "footnotes",
    title: "Footnotes",
    description: "Text with footnotes",
    category: "advanced",
    icon: FileText,
    code: `This is a statement[^1] with a footnote.

Another statement[^2].

[^1]: First footnote.
[^2]: Second footnote.`,
  },
  {
    id: "collapsible",
    title: "Collapsible",
    description: "Expandable section",
    category: "advanced",
    icon: FileText,
    code: `<details>
<summary>Click to expand</summary>

Hidden content here.

- Item 1
- Item 2
</details>`,
  },
  {
    id: "readme",
    title: "README",
    description: "Project README",
    category: "advanced",
    icon: FileText,
    code: `# Project Name

Brief description.

## Features
- Feature 1
- Feature 2

## Installation
\`\`\`bash
npm install
\`\`\`

## Usage
\`\`\`javascript
import { example } from 'package';
\`\`\`

## License
MIT`,
  },
]

export function getAllTemplates(): MarkdownTemplate[] {
  return markdownTemplates
}

export function getTemplateById(id: string): MarkdownTemplate | undefined {
  return markdownTemplates.find((t) => t.id === id)
}

export function getTemplatesByCategory(category: string): MarkdownTemplate[] {
  return markdownTemplates.filter((t) => t.category === category)
}

export function searchTemplates(query: string): MarkdownTemplate[] {
  const lowerQuery = query.toLowerCase()
  return markdownTemplates.filter(
    (t) =>
      t.title.toLowerCase().includes(lowerQuery) ||
      t.description.toLowerCase().includes(lowerQuery) ||
      t.category.toLowerCase().includes(lowerQuery)
  )
}
