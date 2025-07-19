"use client"

import { useEffect, useRef, useState, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode, type JSX } from "lexical"
import { Code2, CornerDownLeft } from "lucide-react"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import DOMPurify from "dompurify"
import { cn } from "@/lib/utils"

import { Button } from "@/components/editor/ui/button"
import { Textarea } from "@/components/editor/ui/textarea"
import { Switch } from "@/components/editor/ui/switch"
import type React from "react" // Import React
import { EditorLoadingContext } from "../lexical-editor"

export interface HTMLData {
  content: string
}

export interface SerializedHTMLNode extends SerializedLexicalNode {
  type: "html"
  data: HTMLData
  version: 1
}

export class HTMLNode extends DecoratorNode<JSX.Element> {
  __data: HTMLData

  static getType(): string {
    return "html"
  }

  static clone(node: HTMLNode): HTMLNode {
    return new HTMLNode(node.__data, node.__key)
  }

  constructor(data: HTMLData, key?: string) {
    super(key)
    this.__data = data
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: HTMLData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedHTMLNode {
    return {
      type: "html",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedHTMLNode): HTMLNode {
    return new HTMLNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <HTMLComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface HTMLComponentProps {
  data: HTMLData
  nodeKey: string
}

const selfClosingTags = new Set([
  "area",
  "base",
  "br",
  "col",
  "embed",
  "hr",
  "img",
  "input",
  "link",
  "meta",
  "param",
  "source",
  "track",
  "wbr",
])

function autoCloseTag(content: string, position: number): { text: string; newPosition: number } | null {
  const beforeCursor = content.slice(0, position)
  const match = beforeCursor.match(/<([a-zA-Z][a-zA-Z0-9]*)[^>]*>(?![^<]*<\/\1>)[^<]*$/)

  if (!match) return null

  const tagName = match[1]
  if (selfClosingTags.has(tagName.toLowerCase())) return null

  const closingTag = `</${tagName}>`
  return {
    text: content.slice(0, position) + closingTag + content.slice(position),
    newPosition: position + closingTag.length,
  }
}

function HTMLComponent({ data, nodeKey }: HTMLComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [isEditing, setIsEditing] = useState(!data.content && !isLoading)
  const [content, setContent] = useState(data.content)
  const [autoClose, setAutoClose] = useState(true)
  const htmlRef = useRef<HTMLDivElement>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const isClickInsideHtml = htmlRef.current && htmlRef.current.contains(e.target as Node)

      if (!isClickInsideHtml) {
        setIsEditing(false)
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => {
      document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [])

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false)
    }
  }, [isLoading])

  const updateHTML = (newContent: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof HTMLNode) {
        node.setData({ content: newContent })
      }
    })
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (!autoClose) return

    if (e.key === "Enter") {
      const textarea = e.currentTarget
      const cursorPosition = textarea.selectionStart
      const result = autoCloseTag(content, cursorPosition)

      if (result) {
        e.preventDefault()
        setContent(result.text)
        updateHTML(result.text)

        // Set cursor position after the next tick
        setTimeout(() => {
          textarea.selectionStart = result.newPosition
          textarea.selectionEnd = result.newPosition
        }, 0)
      }
    }
  }

  const getPreviewContent = () => {
    const sanitizedHtml = DOMPurify.sanitize(content)
    return `
      <!DOCTYPE html>
      <html>
        <head>
          <base target="_blank">
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
        </head>
        <body>
          ${sanitizedHtml}
        </body>
      </html>
    `
  }

  const placeholder = `<!DOCTYPE html>
<html>
<head>
  <style>
    .container {
      max-width: 600px;
      margin: 0 auto;
      padding: 20px;
      text-align: center;
      font-family: system-ui, sans-serif;
    }
    
    button {
      padding: 10px 20px;
      background: #0070f3;
      color: white;
      border: none;
      border-radius: 5px;
      cursor: pointer;
    }
    
    button:hover {
      background: #0051a2;
    }
  </style>
</head>
<body>
  <div class="container">
    <h1>Hello, World!</h1>
    <p>This is a sample HTML content with embedded CSS and JavaScript.</p>
    <button id="clickMe">Click Me!</button>
  </div>

  <script>
    document.getElementById('clickMe').addEventListener('click', () => {
      alert('Button clicked!')
    })
  </script>
</body>
</html>`

  return (
    <div ref={htmlRef} className="my-0 relative group" onClick={() => setIsEditing(true)}>
      <div className="rounded-lg border bg-muted/20 p-2" onClick={() => setIsEditing(true)}>
        {isEditing ? (
          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Code2 className="h-4 w-4" />
                  HTML Editor
                </div>
                <div className="flex items-center gap-2">
                  <Switch checked={autoClose} onCheckedChange={setAutoClose} id="auto-close" />
                  <label htmlFor="auto-close" className="text-sm text-muted-foreground cursor-pointer">
                    Auto-close tags
                  </label>
                </div>
              </div>
              <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)} disabled={!content}>
                Done
              </Button>
            </div>
            {autoClose && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground bg-muted/50 p-2 rounded">
                <CornerDownLeft className="h-4 w-4" />
                Press Enter after opening tag to auto-close
              </div>
            )}
            <Textarea
              ref={textareaRef}
              value={content}
              onChange={(e) => {
                setContent(e.target.value)
                updateHTML(e.target.value)
              }}
              onKeyDown={handleKeyDown}
              placeholder={placeholder}
              className="min-h-[300px] resize-y font-mono text-sm"
            />
          </div>
        ) : (
          <div
            className={cn(
              "w-full bg-background rounded-md",
              !content && "min-h-[2.5rem] flex items-center justify-center text-sm text-muted-foreground",
            )}
          >
            {content ? (
              <iframe
                srcDoc={getPreviewContent()}
                className="w-full rounded-md"
                style={{ height: content ? "auto" : "0" }}
                sandbox="allow-scripts allow-popups allow-same-origin"
                title="HTML Preview"
              />
            ) : (
              "Click to edit HTML..."
            )}
          </div>
        )}
      </div>
    </div>
  )
}

export function $createHTMLNode(): HTMLNode {
  return new HTMLNode({
    content: "",
  })
}
