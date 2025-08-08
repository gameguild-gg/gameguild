"use client"

import type React from "react"

import { useCallback, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Eye } from 'lucide-react'
import type { SerializedEditorState } from "lexical"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"

// Import all preview components
import { PreviewQuiz } from "./preview-components/preview-quiz"
import { PreviewImage } from "./preview-components/preview-image"
import { PreviewGallery } from "./preview-components/preview-gallery"
import { PreviewMarkdown } from "./preview-components/preview-markdown"
import { PreviewHTML } from "./preview-components/preview-html"
import { PreviewVideo } from "./preview-components/preview-video"
import { PreviewAudio } from "./preview-components/preview-audio"
import { PreviewHeader } from "./preview-components/preview-header"
import { PreviewDivider } from "./preview-components/preview-divider"
import { PreviewPresentation } from "./preview-components/preview-presentation"
import { PreviewSource } from "./preview-components/preview-source"
import { PreviewYouTube } from "./preview-components/preview-youtube"
import { PreviewSpotify } from "./preview-components/preview-spotify"
import { PreviewSourceCode } from "./preview-components/preview-source-code"
import { PreviewCallout } from "./preview-components/preview-callout"
import { PreviewButton } from "./preview-components/preview-button"

function PreviewContent({ serializedState }: { serializedState: SerializedEditorState }) {
  const renderNode = (node: any) => {
    // Handle quiz nodes
    if (node.type === "quiz") {
      return <PreviewQuiz key={node.version} node={node} />
    }

    // Handle image nodes
    if (node.type === "image") {
      return <PreviewImage key={node.version} node={node} />
    }

    // Handle gallery nodes
    if (node.type === "gallery") {
      return <PreviewGallery key={node.version} node={node} />
    }

    // Handle markdown nodes
    if (node.type === "markdown") {
      return <PreviewMarkdown key={node.version} node={node} />
    }

    // Handle HTML nodes
    if (node.type === "html") {
      return <PreviewHTML key={node.version} node={node} />
    }

    // Handle video nodes
    if (node.type === "video") {
      return <PreviewVideo key={node.version} node={node} />
    }

    // Handle audio nodes
    if (node.type === "audio") {
      return <PreviewAudio key={node.version} node={node} />
    }

    // Handle header nodes
    if (node.type === "header") {
      return <PreviewHeader key={node.version} node={node} />
    }

    // Handle divider nodes
    if (node.type === "divider") {
      return <PreviewDivider key={node.version} node={node} />
    }

    // Handle button nodes
    if (node.type === "button") {
      return <PreviewButton key={node.version} node={node} />
    }

    // Handle callout nodes
    if (node.type === "callout") {
      return <PreviewCallout key={node.version} node={node} />
    }

    // Handle presentation nodes
    if (node.type === "presentation") {
      return <PreviewPresentation key={node.version} node={node} />
    }

    // Handle source nodes
    if (node.type === "source") {
      return <PreviewSource key={node.version} node={node} />
    }

    // Handle YouTube nodes
    if (node.type === "youtube") {
      return <PreviewYouTube key={node.version} node={node} />
    }

    // Handle Spotify nodes
    if (node.type === "spotify") {
      return <PreviewSpotify key={node.version} node={node} />
    }

    // Handle source code nodes
    if (node.type === "source-code") {
      return <PreviewSourceCode key={node.version} node={node} />
    }

    // For text content
    if (node.type === "text") {
      let textContent = node.text

      // Get inline styles from the node
      const inlineStyles: React.CSSProperties = {}
      if (node.style) {
        // Parse the style string and convert to React CSSProperties
        const styleString = node.style
        const styleRules = styleString.split(";").filter((rule: string) => rule.trim())

        styleRules.forEach((rule: { split: (arg0: string) => { (): any; new(): any; map: { (arg0: (s: any) => any): [any, any]; new(): any } } }) => {
          const [property, value] = rule.split(":").map((s: string) => s.trim())
          if (property && value) {
            // Convert CSS property names to camelCase for React
            const camelCaseProperty = property.replace(/-([a-z])/g, (match: any, letter: string) => letter.toUpperCase())
            inlineStyles[camelCaseProperty as keyof React.CSSProperties] = value
          }
        })
      }

      // Apply text formatting
      if (node.format) {
        if (node.format & 1) {
          // Bold
          textContent = (
            <strong key={`bold-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </strong>
          )
        }
        if (node.format & 2) {
          // Italic
          textContent = (
            <em key={`italic-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </em>
          )
        }
        if (node.format & 4) {
          // Underline (format flag 4)
          textContent = (
            <u key={`underline-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </u>
          )
        }
        if (node.format & 32) {
          // Subscript (format flag 32)
          textContent = (
            <sub key={`subscript-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </sub>
          )
        }
        if (node.format & 16) {
          // Superscript (format flag 16)
          textContent = (
            <sup key={`superscript-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </sup>
          )
        }
        if (node.format & 64) {
          // Code
          textContent = (
            <code
              key={`code-${node.version || Math.random()}`}
              style={inlineStyles}
              className="bg-muted px-1 py-0.5 rounded text-sm"
            >
              {textContent}
            </code>
          )
        }
        if (node.format & 128) {
          // Assuming format flag 128 for short quote
          textContent = (
            <q key={`quote-${node.version || Math.random()}`} style={inlineStyles}>
              {textContent}
            </q>
          )
        }
        // Remove the following lines from the `if (node.format)` block:
      } else if (Object.keys(inlineStyles).length > 0) {
        // Apply inline styles even if no formatting flags are set
        textContent = (
          <span key={`styled-${node.version || Math.random()}`} style={inlineStyles}>
            {textContent}
          </span>
        )
      }

      return textContent
    }

    // For paragraphs and other block elements
    if (node.children) {
      const children = node.children.map((child: any) => renderNode(child))
      switch (node.type) {
        case "paragraph":
          const paragraphClasses = ["my-4"]
          if (node.format === "left") paragraphClasses.push("text-left")
          else if (node.format === "center") paragraphClasses.push("text-center")
          else if (node.format === "right") paragraphClasses.push("text-right")
          else if (node.format === "justify") paragraphClasses.push("text-justify")

          // Always use <p> tag for paragraphs
          return (
            <p key={node.version} className={paragraphClasses.join(" ")}>
              {children}
            </p>
          )
        case "quote":
          return (
            <blockquote key={node.version} className="border-l-4 border-muted pl-4 italic my-4">
              {children}
            </blockquote>
          )
        case "list":
          const ListTag = node.listType === "bullet" ? "ul" : "ol"
          const listClass = node.listType === "bullet" ? "list-disc list-inside" : "list-decimal list-inside"
          return (
            <ListTag key={node.version} className={`${listClass} my-4`}>
              {children}
            </ListTag>
          )
        case "listitem":
          const listItemClasses = ["my-1"]
          if (node.format === "left") listItemClasses.push("text-left")
          else if (node.format === "center") listItemClasses.push("text-center")
          else if (node.format === "right") listItemClasses.push("text-right")
          else if (node.format === "justify") listItemClasses.push("text-justify")
          return (
            <li key={node.version} className={listItemClasses.join(" ")}>
              {children}
            </li>
          )
        case "link":
          return (
            <a
              key={node.version}
              href={node.url}
              className="text-primary hover:underline"
              target="_blank"
              rel="noopener noreferrer"
            >
              {children}
            </a>
          )
        case "heading":
          const headingClasses = ["font-bold my-4"]
          if (node.format === "left") headingClasses.push("text-left")
          else if (node.format === "center") headingClasses.push("text-center")
          else if (node.format === "right") headingClasses.push("text-right")
          else if (node.format === "justify") headingClasses.push("text-justify")

          // Get inline styles from the node if they exist
          const headingStyles: React.CSSProperties = {}
          if (node.style) {
            const styleString = node.style
            const styleRules = styleString.split(";").filter((rule: string) => rule.trim())

            styleRules.forEach((rule: { split: (arg0: string) => { (): any; new(): any; map: { (arg0: (s: any) => any): [any, any]; new(): any } } }) => {
              const [property, value] = rule.split(":").map((s: string) => s.trim())
              if (property && value) {
                const camelCaseProperty = property.replace(/-([a-z])/g, (match: any, letter: string) => letter.toUpperCase())
                headingStyles[camelCaseProperty as keyof React.CSSProperties] = value
              }
            })
          }

          switch (node.tag) {
            case "h1":
            case 1:
              return (
                <h1 key={node.version} className={`text-4xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h1>
              )
            case "h2":
            case 2:
              return (
                <h2 key={node.version} className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h2>
              )
            case "h3":
            case 3:
              return (
                <h3 key={node.version} className={`text-2xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h3>
              )
            case "h4":
            case 4:
              return (
                <h4 key={node.version} className={`text-xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h4>
              )
            case "h5":
            case 5:
              return (
                <h5 key={node.version} className={`text-lg ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h5>
              )
            case "h6":
            case 6:
              return (
                <h6 key={node.version} className={`text-base ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h6>
              )
            default:
              return (
                <h2 key={node.version} className={`text-3xl ${headingClasses.join(" ")}`} style={headingStyles}>
                  {children}
                </h2>
              )
          }
        default:
          return <div key={node.version}>{children}</div>
      }
    }

    return null
  }

  return (
    <div className="prose prose-stone dark:prose-invert max-w-none">
      {serializedState.root.children.map((node: any) => renderNode(node))}
    </div>
  )
}

export function PreviewPlugin() {
  const [editor] = useLexicalComposerContext()
  const [isOpen, setIsOpen] = useState(false)
  const [serializedState, setSerializedState] = useState<SerializedEditorState | null>(null)

  const onClick = useCallback(() => {
    const editorState = editor.getEditorState()
    const serialized = editorState.toJSON()
    setSerializedState(serialized)
    setIsOpen(true)
  }, [editor])

  return (
    <>
      <Button variant="outline" size="sm" onClick={onClick} className="flex items-center gap-2 bg-transparent">
        <Eye className="h-4 w-4" />
        Preview
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="max-w-6xl max-h-[95vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
            <DialogDescription>Preview of your content as it would appear to readers</DialogDescription>
          </DialogHeader>
          {serializedState && <PreviewContent serializedState={serializedState} />}
        </DialogContent>
      </Dialog>
    </>
  )
}
