import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';

const testMarkdown = `# Markdown Renderer Test

This is a test page to demonstrate the markdown renderer functionality.

## Basic Markdown Features

### Headers
This is a **bold text** and this is *italic text*.

### Lists
- Item 1
- Item 2
- Item 3

### Code Blocks
\`\`\`javascript
function hello() {
  console.log("Hello, World!");
}
\`\`\`

### Inline Code
This is an inline \`code\` example.

### Links
[Visit our website](https://gameguild.gg)

### Blockquotes
> This is a blockquote
> It can span multiple lines

## Advanced Features

### Admonitions

::: note "Important Note"
This is an important note with a custom title.
:::

::: tip
This is a helpful tip.
:::

::: warning
This is a warning message.
:::

::: danger
This is a danger message.
:::

### Mermaid Diagrams

#### Small Diagram (should not scale up)
\`\`\`mermaid
graph TD
    A[Start] --> B{Is it working?}
    B -->|Yes| C[Great!]
    B -->|No| D[Debug]
    D --> B
\`\`\`

#### Large Diagram (should scale down)
\`\`\`mermaid
graph TD
    A[User Input] --> B{Validate Input}
    B -->|Valid| C[Process Data]
    B -->|Invalid| D[Show Error]
    C --> E[Transform Data]
    E --> F[Apply Business Logic]
    F --> G{Check Conditions}
    G -->|Condition 1| H[Execute Action 1]
    G -->|Condition 2| I[Execute Action 2]
    G -->|Condition 3| J[Execute Action 3]
    H --> K[Update Database]
    I --> K
    J --> K
    K --> L[Generate Response]
    L --> M[Send Response]
    D --> N[Log Error]
    N --> O[Return to User]
\`\`\`

### Quiz Activity

!!! quiz
{
  "title": "Sample Quiz",
  "question": "What is 2 + 2?",
  "options": ["3", "4", "5", "6"],
  "answers": ["4"]
}
!!!

### Code Activity

!!! code
{
  "code": "print('Hello, World!')",
  "description": "Write a Python program that prints 'Hello, World!'",
  "language": "python",
  "expectedOutput": "Hello, World!",
  "stdin": ""
}
!!!

### Math Equations

Inline math: $E = mc^2$

Block math:
$$
\\int_{-\\infty}^{\\infty} e^{-x^2} dx = \\sqrt{\\pi}
$$

### Images

Here are some test images to verify responsive behavior:

![Small Image](https://via.placeholder.com/300x200/0066cc/ffffff?text=Small+Image)

![Medium Image](https://via.placeholder.com/600x400/cc6600/ffffff?text=Medium+Image)

![Large Image](https://via.placeholder.com/1200x800/cc0066/ffffff?text=Large+Image)

![Extra Large Image](https://via.placeholder.com/2000x1000/66cc00/ffffff?text=Extra+Large+Image)

### Tables

| Feature | Status | Notes |
|---------|--------|-------|
| Basic Markdown | ✅ | Working |
| Admonitions | ✅ | Working |
| Mermaid | ✅ | Working |
| Quiz | ✅ | Working |
| Code Activities | ✅ | Working |
| Math | ✅ | Working |
| Images | ✅ | Working |

## Conclusion

The markdown renderer is working correctly! 🎉
`;

export default function MarkdownTestPage() {
  return (
    <div
      className="min-h-screen bg-background"
      style={{
        width: '100vw',
        maxWidth: 'none',
        margin: 0,
        padding: 0
      }}
    >
      <div
        className="px-4 py-8"
        style={{
          width: '100%',
          maxWidth: 'none',
          margin: 0
        }}
      >
        <div
          className="mb-8"
          style={{
            width: '100%',
            maxWidth: 'none'
          }}
        >
          <h1 className="text-3xl font-bold mb-4 text-foreground">Markdown Renderer Test</h1>
          <p className="text-muted-foreground">
            This page demonstrates the markdown renderer functionality migrated from the old implementation.
          </p>
        </div>

        <div
          className="bg-card rounded-lg shadow-lg p-4 md:p-8"
          style={{
            width: '100%',
            maxWidth: 'none',
            margin: 0
          }}
        >
          <MarkdownRenderer content={testMarkdown} />
        </div>
      </div>
    </div>
  );
}