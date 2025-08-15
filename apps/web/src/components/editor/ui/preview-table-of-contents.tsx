"use client"

import { useState, useEffect } from "react"
import { List, ChevronRight } from "lucide-react"

interface HeadingItem {
  id: string
  text: string
  level: number
  element?: HTMLElement
}

interface PreviewTableOfContentsProps {
  serializedState: any
  className?: string
}

export function PreviewTableOfContents({ serializedState, className = "" }: PreviewTableOfContentsProps) {
  const [headings, setHeadings] = useState<HeadingItem[]>([])
  const [activeHeading, setActiveHeading] = useState<string>("")

  // Extract headings from lexical data
  useEffect(() => {
    if (!serializedState?.root?.children) {
      setHeadings([])
      return
    }

    const extractHeadings = (nodes: any[], level = 0): HeadingItem[] => {
      const headingItems: HeadingItem[] = []
      let headingIndex = 0 // Use proper counter starting from 0

      const processNode = (node: any) => {
        if (node.type === "heading" && node.tag) {
          const headingLevel = Number.parseInt(node.tag.replace("h", ""))
          const text = extractTextFromNode(node)
          if (text.trim()) {
            const id = `heading-${headingIndex}`
            console.log("[v0] TOC - Generated heading ID:", id)
            console.log("[v0] TOC - Heading index:", headingIndex)
            console.log("[v0] TOC - Heading text:", text.trim())
            headingItems.push({
              id,
              text: text.trim(),
              level: headingLevel,
            })
            headingIndex++ // Increment after creating the heading
          }
        }

        // Recursively process children
        if (node.children && Array.isArray(node.children)) {
          node.children.forEach(processNode)
        }
      }

      serializedState.root.children.forEach(processNode)
      return headingItems
    }

    const extractTextFromNode = (node: any): string => {
      if (node.type === "text") {
        return node.text || ""
      }

      if (node.children && Array.isArray(node.children)) {
        return node.children.map(extractTextFromNode).join("")
      }

      return ""
    }

    const extractedHeadings = extractHeadings(serializedState.root.children)
    setHeadings(extractedHeadings)
  }, [serializedState])

  const scrollToHeading = (headingId: string) => {
    console.log("[v0] Attempting to scroll to heading:", headingId)

    setActiveHeading(headingId)

    const element = document.querySelector(`[data-heading-id="${headingId}"]`)
    console.log("[v0] Found element:", element)

    if (element) {
      console.log("[v0] Scrolling to element")
      const elementRect = element.getBoundingClientRect()
      const absoluteElementTop = elementRect.top + window.pageYOffset
      const offset = 80 // Account for any fixed headers or padding

      window.scrollTo({
        top: absoluteElementTop - offset,
        behavior: "smooth",
      })
    } else {
      console.log("[v0] Element not found, checking all heading elements:")
      const allHeadings = document.querySelectorAll("[data-heading-id]")
      allHeadings.forEach((el, index) => {
        console.log(`[v0] Heading ${index}:`, el.getAttribute("data-heading-id"))
      })
    }
  }

  if (headings.length === 0) {
    return (
      <div
        className={`bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm ${className}`}
      >
        <div className="p-6">
          <div className="flex items-center gap-2 mb-4">
            <List className="w-5 h-5 text-gray-600 dark:text-gray-400" />
            <h3 className="font-semibold text-gray-900 dark:text-gray-100">Table of Contents</h3>
          </div>
          <p className="text-sm text-gray-500 dark:text-gray-400">No headings found in this document</p>
        </div>
      </div>
    )
  }

  return (
    <div
      className={`bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm ${className}`}
    >
      <div className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <List className="w-5 h-5 text-gray-600 dark:text-gray-400" />
          <h3 className="font-semibold text-gray-900 dark:text-gray-100">Table of Contents</h3>
        </div>

        <nav className="space-y-1">
          {headings.map((heading) => (
            <button
              key={heading.id}
              onClick={() => scrollToHeading(heading.id)}
              className={`
                w-full text-left px-3 py-2 rounded-lg text-sm transition-colors
                hover:bg-gray-100 dark:hover:bg-gray-800
                ${
                  activeHeading === heading.id
                    ? "bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 border-l-2 border-blue-500"
                    : "text-gray-700 dark:text-gray-300"
                }
              `}
              style={{
                paddingLeft: `${Math.max(0.75, heading.level * 0.5)}rem`,
                marginLeft: `${(heading.level - 1) * 0.5}rem`,
              }}
            >
              <div className="flex items-center gap-2">
                {heading.level > 1 && (
                  <ChevronRight className="w-3 h-3 text-gray-400 dark:text-gray-500 flex-shrink-0" />
                )}
                <span className="truncate">{heading.text}</span>
              </div>
            </button>
          ))}
        </nav>
      </div>
    </div>
  )
}
