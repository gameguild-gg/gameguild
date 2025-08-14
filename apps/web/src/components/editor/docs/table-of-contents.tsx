"use client"

import { useEffect, useState } from "react"
import { ChevronRight } from "lucide-react"

interface TableOfContentsProps {
  activeSection: string
}

interface TocItem {
  id: string
  title: string
  level: number
}

const tocMap: Record<string, TocItem[]> = {
  introduction: [
    { id: "what-is-gameguild-editor", title: "What is the GameGuild Editor?", level: 2 },
    { id: "key-features", title: "Key Features", level: 2 },
    { id: "getting-started", title: "Getting Started", level: 2 },
    { id: "system-requirements", title: "System Requirements", level: 3 },
    { id: "browser-support", title: "Browser Support", level: 3 },
  ],
  "about-editor": [
    { id: "editor-architecture", title: "Editor Architecture", level: 2 },
    { id: "core-components", title: "Core Components", level: 2 },
    { id: "lexical-framework", title: "Lexical Framework", level: 3 },
    { id: "plugin-system", title: "Plugin System", level: 3 },
    { id: "storage-system", title: "Storage System", level: 2 },
    { id: "indexeddb", title: "IndexedDB Integration", level: 3 },
  ],
  "getting-started": [
    { id: "your-first-project", title: "Your First Project", level: 2 },
    { id: "basic-navigation", title: "Basic Navigation", level: 2 },
    { id: "editor-interface", title: "Editor Interface", level: 3 },
    { id: "toolbar-overview", title: "Toolbar Overview", level: 3 },
    { id: "saving-and-loading", title: "Saving and Loading", level: 2 },
    { id: "auto-save", title: "Auto-save Feature", level: 3 },
  ],
  "text-formatting": [
    { id: "basic-formatting", title: "Basic Formatting", level: 2 },
    { id: "bold-italic-underline", title: "Bold, Italic, Underline", level: 3 },
    { id: "headings", title: "Headings", level: 3 },
    { id: "advanced-styling", title: "Advanced Styling", level: 2 },
    { id: "custom-styles", title: "Custom Styles", level: 3 },
    { id: "keyboard-shortcuts", title: "Keyboard Shortcuts", level: 2 },
  ],
  "media-content": [
    { id: "image-handling", title: "Image Handling", level: 2 },
    { id: "upload-images", title: "Uploading Images", level: 3 },
    { id: "image-gallery", title: "Image Gallery", level: 3 },
    { id: "video-content", title: "Video Content", level: 2 },
    { id: "embed-videos", title: "Embedding Videos", level: 3 },
    { id: "file-attachments", title: "File Attachments", level: 2 },
  ],
  "interactive-elements": [
    { id: "buttons-links", title: "Buttons and Links", level: 2 },
    { id: "forms", title: "Forms", level: 2 },
    { id: "input-fields", title: "Input Fields", level: 3 },
    { id: "checkboxes-radio", title: "Checkboxes and Radio Buttons", level: 3 },
    { id: "interactive-widgets", title: "Interactive Widgets", level: 2 },
  ],
  "code-blocks": [
    { id: "syntax-highlighting", title: "Syntax Highlighting", level: 2 },
    { id: "supported-languages", title: "Supported Languages", level: 3 },
    { id: "code-snippets", title: "Code Snippets", level: 2 },
    { id: "inline-code", title: "Inline Code", level: 3 },
    { id: "code-formatting", title: "Code Formatting", level: 2 },
  ],
  quizzes: [
    { id: "creating-quizzes", title: "Creating Quizzes", level: 2 },
    { id: "question-types", title: "Question Types", level: 3 },
    { id: "quiz-settings", title: "Quiz Settings", level: 3 },
    { id: "form-elements", title: "Form Elements", level: 2 },
    { id: "validation", title: "Form Validation", level: 3 },
  ],
  "project-management": [
    { id: "organizing-projects", title: "Organizing Projects", level: 2 },
    { id: "project-tags", title: "Project Tags", level: 3 },
    { id: "project-templates", title: "Project Templates", level: 3 },
    { id: "version-control", title: "Version Control", level: 2 },
    { id: "backup-restore", title: "Backup and Restore", level: 2 },
  ],
  settings: [
    { id: "editor-preferences", title: "Editor Preferences", level: 2 },
    { id: "auto-save-settings", title: "Auto-save Settings", level: 3 },
    { id: "interface-options", title: "Interface Options", level: 3 },
    { id: "accessibility", title: "Accessibility", level: 2 },
    { id: "performance-settings", title: "Performance Settings", level: 2 },
  ],
}

export function TableOfContents({ activeSection }: TableOfContentsProps) {
  const [activeHeading, setActiveHeading] = useState<string>("")
  const [isScrolling, setIsScrolling] = useState(false)

  const headings = tocMap[activeSection] || []

  useEffect(() => {
    if (headings.length === 0) return

    const handleScroll = () => {
      if (isScrolling) return

      const headingElements = headings
        .map((heading) => document.getElementById(heading.id))
        .filter(Boolean) as HTMLElement[]

      if (headingElements.length === 0) return

      const scrollPosition = window.scrollY + 100 // Offset for better UX

      let currentHeading = headingElements[0]?.id || ""

      for (const element of headingElements) {
        if (element.offsetTop <= scrollPosition) {
          currentHeading = element.id
        } else {
          break
        }
      }

      setActiveHeading(currentHeading)
    }

    window.addEventListener("scroll", handleScroll, { passive: true })
    handleScroll() // Initial call

    return () => window.removeEventListener("scroll", handleScroll)
  }, [headings, isScrolling])

  const scrollToHeading = (headingId: string) => {
    const element = document.getElementById(headingId)
    if (element) {
      setIsScrolling(true)
      element.scrollIntoView({ behavior: "smooth", block: "start" })

      // Reset scrolling flag after animation
      setTimeout(() => setIsScrolling(false), 1000)
    }
  }

  if (headings.length === 0) {
    return (
      <div className="p-4">
        <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-4">On this page</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">No headings available for this section.</p>
      </div>
    )
  }

  return (
    <nav className="p-4 sticky top-0">
      <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-4">On this page</h3>

      <ul className="space-y-1">
        {headings.map((heading) => (
          <li key={heading.id} className={`${heading.level === 3 ? "ml-4" : ""}`}>
            <button
              onClick={() => scrollToHeading(heading.id)}
              className={`w-full text-left text-sm py-1.5 px-2 rounded transition-colors group flex items-center gap-2 ${
                activeHeading === heading.id
                  ? "text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
                  : "text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700/50"
              }`}
            >
              {heading.level === 3 && <ChevronRight className="w-3 h-3 flex-shrink-0 opacity-50" />}
              <span className="truncate">{heading.title}</span>
            </button>
          </li>
        ))}
      </ul>

      {/* Quick actions */}
      <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="space-y-2">
          <button
            onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
            className="w-full text-left text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 py-1"
          >
            ↑ Back to top
          </button>
          <button
            onClick={() => scrollToHeading(headings[headings.length - 1]?.id)}
            className="w-full text-left text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 py-1"
          >
            ↓ Jump to bottom
          </button>
        </div>
      </div>
    </nav>
  )
}
