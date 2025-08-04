# Markdown Renderer

This directory contains the markdown renderer components migrated from the old `gameguild-main/services/web` implementation.

## Components

### Main Components

- **`markdown-renderer.tsx`** - The main markdown renderer component that handles all markdown parsing and rendering
- **`Admonition.tsx`** - Component for rendering admonition blocks (note, warning, tip, etc.)
- **`Mermaid.tsx`** - Component for rendering Mermaid diagrams
- **`RevealJS.tsx`** - Component for rendering RevealJS presentations
- **`MarkdownQuizActivity.tsx`** - Component for interactive quiz activities
- **`MarkdownCodeActivity.tsx`** - Component for interactive code activities

## Features

### Basic Markdown Support
- Headers (h1-h6)
- Bold and italic text
- Lists (ordered and unordered)
- Links
- Blockquotes
- Inline code and code blocks with syntax highlighting
- Tables

### Advanced Features
- **Admonitions**: Custom blocks for notes, warnings, tips, etc.
  ```markdown
  ::: note "Important Note"
  This is an important note with a custom title.
  :::
  ```

- **Mermaid Diagrams**: Render flowcharts and diagrams
  ```markdown
  ```mermaid
  graph TD
      A[Start] --> B{Is it working?}
      B -->|Yes| C[Great!]
      B -->|No| D[Debug]
  ```
  ```

- **Math Equations**: Support for KaTeX math rendering
  ```markdown
  Inline math: $E = mc^2$
  
  Block math:
  $$
  \int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
  $$
  ```

- **Interactive Quiz Activities**:
  ```markdown
  !!! quiz
  {
    "title": "Sample Quiz",
    "question": "What is 2 + 2?",
    "options": ["3", "4", "5", "6"],
    "answers": ["4"]
  }
  !!!
  ```

- **Interactive Code Activities**:
  ```markdown
  !!! code
  {
    "code": "print('Hello, World!')",
    "description": "Write a Python program that prints 'Hello, World!'",
    "language": "python",
    "expectedOutput": "Hello, World!",
    "stdin": ""
  }
  !!!
  ```

- **RevealJS Presentations**: Support for slide presentations
  ```markdown
  <!-- Use renderer="reveal" prop -->
  <MarkdownRenderer content={content} renderer="reveal" />
  ```

## Usage

### Basic Usage
```tsx
import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';

const MyComponent = () => {
  const markdownContent = `
    # My Markdown Content
    
    This is some **bold** and *italic* text.
    
    ::: tip
    This is a helpful tip!
    :::
  `;

  return (
    <div>
      <MarkdownRenderer content={markdownContent} />
    </div>
  );
};
```

### With RevealJS
```tsx
<MarkdownRenderer content={presentationContent} renderer="reveal" />
```

## Dependencies

The following dependencies are required:

- `react-markdown` - Core markdown parsing
- `remark-gfm` - GitHub Flavored Markdown support
- `remark-math` - Math equation support
- `rehype-raw` - Raw HTML support
- `rehype-katex` - Math rendering
- `react-syntax-highlighter` - Code syntax highlighting
- `mermaid` - Diagram rendering
- `reveal.js` - Presentation rendering
- `canvas-confetti` - Confetti effects for code activities
- `@monaco-editor/react` - Code editor for activities

## Test Route

A test route is available at `/markdown-test` that demonstrates all the features of the markdown renderer.

## Migration Notes

This implementation was migrated from the old `gameguild-main/services/web` project with the following changes:

1. Removed dependency on `@game-guild/apiclient`
2. Simplified the renderer prop to use string literals instead of enum
3. Simplified code execution functionality for demo purposes
4. Updated import paths to match the new project structure
5. Maintained all core functionality while removing complex dependencies

## Future Enhancements

- Add real code execution functionality
- Support for more interactive elements
- Enhanced styling and theming options
- Better error handling and fallbacks 