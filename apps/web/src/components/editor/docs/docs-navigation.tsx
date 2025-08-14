"use client"

import type React from "react"

import { ChevronDown, ChevronRight, Book, FileText, Settings, Zap, Search, X } from "lucide-react"
import { useState, useMemo } from "react"

interface NavigationItem {
  id: string
  title: string
  icon?: React.ComponentType<{ className?: string }>
  children?: NavigationItem[]
  description?: string
}

const navigationItems: NavigationItem[] = [
  {
    id: "introduction",
    title: "Introduction",
    icon: Book,
    description: "Get started with the GameGuild Editor",
    children: [
      { id: "about-editor", title: "About the Editor", description: "Learn about the editor's purpose and features" },
      { id: "getting-started", title: "Getting Started", description: "Quick start guide for new users" },
      { id: "basic-concepts", title: "Basic Concepts", description: "Core concepts and terminology" },
      { id: "installation", title: "Installation", description: "How to set up and install the editor" },
    ],
  },
  {
    id: "editor-features",
    title: "Editor Features",
    icon: FileText,
    description: "Comprehensive guide to editor capabilities",
    children: [
      { id: "text-formatting", title: "Text Formatting", description: "Rich text editing and styling options" },
      { id: "media-content", title: "Media Content", description: "Images, videos, and file attachments" },
      {
        id: "interactive-elements",
        title: "Interactive Elements",
        description: "Buttons, forms, and user interactions",
      },
      { id: "code-blocks", title: "Code Blocks", description: "Syntax highlighting and code snippets" },
      { id: "quizzes", title: "Quizzes & Forms", description: "Interactive quizzes and form elements" },
      { id: "tables", title: "Tables", description: "Creating and formatting tables" },
      { id: "lists", title: "Lists & Outlines", description: "Ordered and unordered lists" },
    ],
  },
  {
    id: "advanced-features",
    title: "Advanced Features",
    icon: Zap,
    description: "Power user features and advanced functionality",
    children: [
      { id: "project-management", title: "Project Management", description: "Organizing and managing projects" },
      { id: "collaboration", title: "Collaboration", description: "Working with teams and sharing content" },
      { id: "export-options", title: "Export Options", description: "Exporting content in various formats" },
      { id: "plugins", title: "Plugins & Extensions", description: "Extending editor functionality" },
      { id: "automation", title: "Automation", description: "Automated workflows and scripting" },
      { id: "api-integration", title: "API Integration", description: "Connecting with external services" },
    ],
  },
  {
    id: "configuration",
    title: "Configuration",
    icon: Settings,
    description: "Customizing and configuring the editor",
    children: [
      { id: "settings", title: "Editor Settings", description: "Configuring editor behavior and preferences" },
      { id: "themes", title: "Themes & Appearance", description: "Customizing the visual appearance" },
      { id: "shortcuts", title: "Keyboard Shortcuts", description: "Complete list of keyboard shortcuts" },
      { id: "storage", title: "Storage Options", description: "Local and cloud storage configuration" },
      { id: "performance", title: "Performance", description: "Optimizing editor performance" },
    ],
  },
]

interface DocsNavigationProps {
  activeSection: string
  onSectionChange: (section: string) => void
}

export function DocsNavigation({ activeSection, onSectionChange }: DocsNavigationProps) {
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(["introduction"]))
  const [searchQuery, setSearchQuery] = useState("")
  const [showSearch, setShowSearch] = useState(false)

  const filteredItems = useMemo(() => {
    if (!searchQuery.trim()) return navigationItems

    const filterItems = (items: NavigationItem[]): NavigationItem[] => {
      return items.reduce((acc: NavigationItem[], item) => {
        const matchesSearch =
          item.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
          item.description?.toLowerCase().includes(searchQuery.toLowerCase())

        const filteredChildren = item.children ? filterItems(item.children) : undefined

        if (matchesSearch || (filteredChildren && filteredChildren.length > 0)) {
          acc.push({
            ...item,
            children: filteredChildren,
          })
        }

        return acc
      }, [])
    }

    return filterItems(navigationItems)
  }, [searchQuery])

  const toggleSection = (sectionId: string) => {
    const newExpanded = new Set(expandedSections)
    if (newExpanded.has(sectionId)) {
      newExpanded.delete(sectionId)
    } else {
      newExpanded.add(sectionId)
    }
    setExpandedSections(newExpanded)
  }

  const handleItemClick = (itemId: string) => {
    onSectionChange(itemId)
  }

  const displayItems = searchQuery.trim() ? filteredItems : navigationItems
  const shouldExpand = (sectionId: string) => (searchQuery.trim() ? true : expandedSections.has(sectionId))

  return (
    <nav className="p-4 h-full flex flex-col">
      {/* Header */}
      <div className="mb-6">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Documentation</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Learn how to use the GameGuild Editor</p>
      </div>

      <div className="mb-4">
        {showSearch ? (
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search documentation..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-10 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              autoFocus
            />
            <button
              onClick={() => {
                setShowSearch(false)
                setSearchQuery("")
              }}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        ) : (
          <button
            onClick={() => setShowSearch(true)}
            className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-700 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          >
            <Search className="w-4 h-4" />
            <span>Search documentation...</span>
          </button>
        )}
      </div>

      {/* Navigation Items */}
      <div className="flex-1 overflow-y-auto">
        <div className="space-y-1">
          {displayItems.map((item) => (
            <div key={item.id}>
              <button
                onClick={() => {
                  if (item.children) {
                    toggleSection(item.id)
                  } else {
                    handleItemClick(item.id)
                  }
                }}
                className={`w-full flex items-center justify-between px-3 py-2 text-sm rounded-md transition-colors group ${
                  activeSection === item.id
                    ? "bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300"
                    : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                }`}
              >
                <div className="flex items-center gap-2 min-w-0">
                  {item.icon && <item.icon className="w-4 h-4 flex-shrink-0" />}
                  <div className="text-left min-w-0">
                    <div className="font-medium truncate">{item.title}</div>
                    {item.description && (
                      <div className="text-xs text-gray-500 dark:text-gray-400 truncate mt-0.5">{item.description}</div>
                    )}
                  </div>
                </div>
                {item.children &&
                  (shouldExpand(item.id) ? (
                    <ChevronDown className="w-4 h-4 flex-shrink-0" />
                  ) : (
                    <ChevronRight className="w-4 h-4 flex-shrink-0" />
                  ))}
              </button>

              {item.children && shouldExpand(item.id) && (
                <div className="ml-6 mt-1 space-y-1">
                  {item.children.map((child) => (
                    <button
                      key={child.id}
                      onClick={() => handleItemClick(child.id)}
                      className={`w-full text-left px-3 py-2 text-sm rounded-md transition-colors group ${
                        activeSection === child.id
                          ? "bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300"
                          : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
                      }`}
                    >
                      <div className="font-medium">{child.title}</div>
                      {child.description && (
                        <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{child.description}</div>
                      )}
                    </button>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>

        {searchQuery.trim() && displayItems.length === 0 && (
          <div className="text-center py-8 text-gray-500 dark:text-gray-400">
            <Search className="w-8 h-8 mx-auto mb-2 opacity-50" />
            <p className="text-sm">No results found for "{searchQuery}"</p>
            <button
              onClick={() => setSearchQuery("")}
              className="text-blue-600 dark:text-blue-400 text-sm mt-2 hover:underline"
            >
              Clear search
            </button>
          </div>
        )}
      </div>
    </nav>
  )
}
