'use client';

import type React from 'react';

import { useEffect, useRef, useState, useContext } from 'react';
import { DecoratorNode, type SerializedLexicalNode, $getNodeByKey } from 'lexical';
import { FileText } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import { cn } from '@/lib/utils';
import { Button } from '@/components/editor/ui/button';
import { Textarea } from '@/components/editor/ui/textarea';
import { EditorLoadingContext } from '../lexical-editor';
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';

export interface MarkdownData {
  content: string;
}

export interface SerializedMarkdownNode extends SerializedLexicalNode {
  type: 'markdown';
  data: MarkdownData;
  version: 1;
}

export class MarkdownNode extends DecoratorNode<React.ElementType> {
  __data: MarkdownData;

  static getType(): string {
    return 'markdown';
  }

  static clone(node: MarkdownNode): MarkdownNode {
    return new MarkdownNode(node.__data, node.__key);
  }

  constructor(data: MarkdownData, key?: string) {
    super(key);
    this.__data = data;
  }

  createDOM(): HTMLElement {
    return document.createElement('div');
  }

  updateDOM(): false {
    return false;
  }

  setData(data: MarkdownData): void {
    const writable = this.getWritable();
    writable.__data = data;
  }

  exportJSON(): SerializedMarkdownNode {
    return {
      type: 'markdown',
      data: this.__data,
      version: 1,
    };
  }

  static importJSON(serializedNode: SerializedMarkdownNode): MarkdownNode {
    return new MarkdownNode(serializedNode.data);
  }

  decorate(): React.ElementType {
    return <MarkdownComponent data={this.__data} nodeKey={this.__key} />;
  }
}

interface MarkdownComponentProps {
  data: MarkdownData;
  nodeKey: string;
}

function MarkdownComponent({ data, nodeKey }: MarkdownComponentProps) {
  const [editor] = useLexicalComposerContext();
  const isLoading = useContext(EditorLoadingContext);
  const [isEditing, setIsEditing] = useState(!data.content && !isLoading);
  const [content, setContent] = useState(data.content);
  const markdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const isClickInsideMarkdown = markdownRef.current && markdownRef.current.contains(e.target as Node);

      if (!isClickInsideMarkdown) {
        setIsEditing(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false);
    }
  }, [isLoading]);

  const updateMarkdown = (newContent: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node instanceof MarkdownNode) {
        node.setData({ content: newContent });
      }
    });
  };

  const handleContentChange = (newContent: string) => {
    setContent(newContent);
    updateMarkdown(newContent);
  };

  const placeholder = `# Markdown Editor

You can write markdown here. Some examples:

## Headers
# H1
## H2
### H3

## Emphasis
*italic* or _italic_
**bold** or __bold__
***bold italic*** or ___bold italic___

## Lists
- Item 1
- Item 2
  - Subitem 2.1
  - Subitem 2.2

1. First item
2. Second item
3. Third item

## Links and Images
[Link text](URL)
![Alt text](image URL)

## Code
\`inline code\`

\`\`\`
// code block
function example() {
  return "Hello, World!";
}
\`\`\`

## Blockquotes
> This is a blockquote
> Multiple lines
>> Nested blockquotes

## Tables
| Header 1 | Header 2 |
|----------|----------|
| Cell 1    | Cell 2    |
| Cell 3    | Cell 4    |
`;

  return (
    <div ref={markdownRef} className="my-4 relative group" onClick={() => !isEditing && setIsEditing(true)}>
      <div className="rounded-lg border bg-muted/20 p-4">
        {isEditing ? (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <FileText className="h-4 w-4" />
                Markdown Editor
              </div>
              <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)} disabled={!content}>
                Done
              </Button>
            </div>
            <Textarea
              value={content}
              onChange={(e) => handleContentChange(e.target.value)}
              placeholder={placeholder}
              className="min-h-[200px] resize-y font-mono text-sm"
            />
          </div>
        ) : (
          <div className={cn('prose prose-stone dark:prose-invert max-w-none', !content && 'min-h-[2.5rem] text-sm text-muted-foreground')}>
            {content ? <ReactMarkdown>{content}</ReactMarkdown> : 'Click to edit markdown...'}
          </div>
        )}
      </div>
    </div>
  );
}

export function $createMarkdownNode(): MarkdownNode {
  return new MarkdownNode({
    content: '',
  });
}
