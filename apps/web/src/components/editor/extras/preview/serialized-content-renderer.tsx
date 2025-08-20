"use client"
import type { SerializedEditorState } from "lexical"

// Import all preview components
import { PreviewQuiz } from "@/components/editor/plugins/preview-components/preview-quiz"
import { PreviewImage } from "@/components/editor/plugins/preview-components/preview-image"
import { PreviewGallery } from "@/components/editor/plugins/preview-components/preview-gallery"
import { PreviewMarkdown } from "@/components/editor/plugins/preview-components/preview-markdown"
import { PreviewHTML } from "@/components/editor/plugins/preview-components/preview-html"
import { PreviewVideo } from "@/components/editor/plugins/preview-components/preview-video"
import { PreviewAudio } from "@/components/editor/plugins/preview-components/preview-audio"
import { PreviewHeader } from "@/components/editor/plugins/preview-components/preview-header"
import { PreviewDivider } from "@/components/editor/plugins/preview-components/preview-divider"
import { PreviewPresentation } from "@/components/editor/plugins/preview-components/preview-presentation"
import { PreviewSource } from "@/components/editor/plugins/preview-components/preview-source"
import { PreviewYouTube } from "@/components/editor/plugins/preview-components/preview-youtube"
import { PreviewSpotify } from "@/components/editor/plugins/preview-components/preview-spotify"
import { PreviewSourceCode } from "@/components/editor/plugins/preview-components/preview-source-code"
import { PreviewAdmonition } from "@/components/editor/plugins/preview-components/preview-admonition"
import { PreviewButton } from "@/components/editor/plugins/preview-components/preview-button"
import { PreviewText } from "@/components/editor/plugins/preview-components/preview-text"
import { PreviewMermaid } from "@/components/editor/plugins/preview-components/preview-mermaid"
import { PreviewParagraph } from "@/components/editor/plugins/preview-components/preview-paragraph"
import { PreviewQuote } from "@/components/editor/plugins/preview-components/preview-quote"
import { PreviewList } from "@/components/editor/plugins/preview-components/preview-list"
import { PreviewListItem } from "@/components/editor/plugins/preview-components/preview-list-item"
import { PreviewLink } from "@/components/editor/plugins/preview-components/preview-link"
import { PreviewHeading } from "@/components/editor/plugins/preview-components/preview-heading"

interface SerializedContentRendererProps {
  serializedState: SerializedEditorState
  className?: string
}

export function SerializedContentRenderer({
  serializedState,
  className = "prose prose-stone dark:prose-invert max-w-none",
}: SerializedContentRendererProps) {
  let headingCounter = 0

  const renderNode = (node: any, index = 0, parentPath = "") => {
    // Create unique key using path and index
    const uniqueKey = `${parentPath}-${node.type}-${index}-${node.version || 0}`

    // Handle quiz nodes
    if (node.type === "quiz") {
      return <PreviewQuiz key={uniqueKey} node={node} />
    }

    // Handle image nodes
    if (node.type === "image") {
      return <PreviewImage key={uniqueKey} node={node} />
    }

    // Handle gallery nodes
    if (node.type === "gallery") {
      return <PreviewGallery key={uniqueKey} node={node} />
    }

    // Handle markdown nodes
    if (node.type === "markdown") {
      return <PreviewMarkdown key={uniqueKey} node={node} />
    }

    // Handle HTML nodes
    if (node.type === "html") {
      return <PreviewHTML key={uniqueKey} node={node} />
    }

    // Handle video nodes
    if (node.type === "video") {
      return <PreviewVideo key={uniqueKey} node={node} />
    }

    // Handle audio nodes
    if (node.type === "audio") {
      return <PreviewAudio key={uniqueKey} node={node} />
    }

    // Handle header nodes
    if (node.type === "header") {
      return <PreviewHeader key={uniqueKey} node={node} />
    }

    // Handle divider nodes
    if (node.type === "divider") {
      return <PreviewDivider key={uniqueKey} node={node} />
    }

    // Handle button nodes
    if (node.type === "button") {
      return <PreviewButton key={uniqueKey} node={node} />
    }

    // Handle admonition nodes
    if (node.type === "admonition") {
      return <PreviewAdmonition key={uniqueKey} node={node} />
    }

    // Handle presentation nodes
    if (node.type === "presentation") {
      return <PreviewPresentation key={uniqueKey} node={node} />
    }

    // Handle source nodes
    if (node.type === "source") {
      return <PreviewSource key={uniqueKey} node={node} />
    }

    // Handle YouTube nodes
    if (node.type === "youtube") {
      return <PreviewYouTube key={uniqueKey} node={node} />
    }

    // Handle Spotify nodes
    if (node.type === "spotify") {
      return <PreviewSpotify key={uniqueKey} node={node} />
    }

    // Handle source code nodes
    if (node.type === "source-code") {
      return <PreviewSourceCode key={uniqueKey} node={node} />
    }

    // For text content - now using the new component
    if (node.type === "text") {
      return <PreviewText key={uniqueKey} node={node} />
    }

    // For Mermaid diagrams
    if (node.type === "mermaid") {
      return <PreviewMermaid key={uniqueKey} data={node.data} />
    }

    if (node.children) {
      const children = node.children.map((child: any, childIndex: number) => renderNode(child, childIndex, uniqueKey))

      switch (node.type) {
        case "paragraph":
          return (
            <PreviewParagraph key={uniqueKey} node={node}>
              {children}
            </PreviewParagraph>
          )
        case "quote":
          return (
            <PreviewQuote key={uniqueKey} node={node}>
              {children}
            </PreviewQuote>
          )
        case "list":
          return (
            <PreviewList key={uniqueKey} node={node}>
              {children}
            </PreviewList>
          )
        case "listitem":
          return (
            <PreviewListItem key={uniqueKey} node={node}>
              {children}
            </PreviewListItem>
          )
        case "link":
          return (
            <PreviewLink key={uniqueKey} node={node}>
              {children}
            </PreviewLink>
          )
        case "heading":
          const currentHeadingIndex = headingCounter++
          console.log("[v0] Rendering heading with index:", currentHeadingIndex)
          return (
            <PreviewHeading key={uniqueKey} node={node} index={currentHeadingIndex}>
              {children}
            </PreviewHeading>
          )
        default:
          return <div key={uniqueKey}>{children}</div>
      }
    }

    return null
  }

  return (
    <div className={className}>
      {serializedState.root.children.map((node: any, index: number) => renderNode(node, index, "root"))}
    </div>
  )
} 