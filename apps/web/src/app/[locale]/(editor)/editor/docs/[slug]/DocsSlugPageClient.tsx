"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { DocsNavigation } from "@/components/editor/docs/docs-navigation"
import { DocsContent } from "@/components/editor/docs/docs-content"
import { TableOfContents } from "@/components/editor/docs/table-of-contents"

// Valid documentation sections
const validSections = [
  "introduction",
  "about-editor",
  "getting-started",
  "basic-concepts",
  "installation",
  "text-formatting",
  "media-content",
  "interactive-elements",
  "code-blocks",
  "quizzes",
  "tables",
  "lists",
  "project-management",
  "collaboration",
  "export-options",
  "plugins",
  "automation",
  "api-integration",
  "settings",
  "themes",
  "shortcuts",
  "storage",
  "performance",
]

interface DocsSlugPageClientProps {
  slug: string
}

export default function DocsSlugPageClient({ slug: initialSlug }: DocsSlugPageClientProps) {
  const router = useRouter()
  const [activeSection, setActiveSection] = useState(initialSlug || "introduction")

  useEffect(() => {
    // Validate slug and redirect if invalid
    if (!validSections.includes(initialSlug)) {
      router.replace("/editor/docs/introduction")
      return
    }

    setActiveSection(initialSlug)
  }, [initialSlug, router])

  const handleSectionChange = (section: string) => {
    if (section !== activeSection) {
      router.push(`/editor/docs/${section}`)
    }
  }

  // Don't render if slug is invalid
  if (!validSections.includes(initialSlug)) {
    return null
  }

  return (
    <>
      {/* Left Sidebar - Navigation */}
      <aside className="w-64 flex-shrink-0 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 h-full overflow-y-auto">
        <DocsNavigation activeSection={activeSection} onSectionChange={handleSectionChange} />
      </aside>

      {/* Main Content */}
      <main className="flex-1 min-w-0 h-full overflow-y-auto">
        <div className="max-w-4xl mx-auto px-6 py-8">
          <DocsContent activeSection={activeSection} />
        </div>
      </main>

      {/* Right Sidebar - Table of Contents */}
      <aside className="w-64 flex-shrink-0 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 h-full overflow-y-auto">
        <TableOfContents activeSection={activeSection} />
      </aside>
    </>
  )
}
