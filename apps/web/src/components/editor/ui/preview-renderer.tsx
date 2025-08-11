"use client"

import type React from "react"
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
import { PreviewCallout } from "@/components/editor/plugins/preview-components/preview-callout"
import { PreviewButton } from "@/components/editor/plugins/preview-components/preview-button"
import { PreviewText } from "@/components/editor/plugins/preview-components/preview-text"

interface PreviewRendererProps {
  serializedState: SerializedEditorState
  showHeader?: boolean
  projectName?: string
  className?: string
}

export function PreviewRenderer({
  serializedState,
  showHeader = false,
  projectName,
  className = "prose prose-stone dark:prose-invert max-w-none",
}: PreviewRendererProps) {
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

    // For text content - now using the new component
    if (node.type === "text") {
      return <PreviewText key={node.version} node={node} />
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
    <div className={className}>
      {showHeader && projectName && (
        <div className="mb-8 pb-4 border-b border-gray-200 dark:border-gray-700">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">{projectName}</h1>
          <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
              />
            </svg>
            <span>Preview Mode</span>
          </div>
        </div>
      )}
      {serializedState.root.children.map((node: any) => renderNode(node))}
    </div>
  )
}
