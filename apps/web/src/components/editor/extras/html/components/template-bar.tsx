"use client"

import { useState, useEffect } from "react"
import { Plus, X, Code2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  getAllHTMLTemplates,
  getHTMLCategories,
  searchHTMLTemplates,
  type HTMLTemplate,
} from "../templates"

interface TemplateBarProps {
  onInsert: (code: string) => void
  onClose: () => void
  selectedTemplate: HTMLTemplate | null
  onSelectTemplate: (template: HTMLTemplate | null) => void
}

export function TemplateBar({ onInsert, onClose, selectedTemplate, onSelectTemplate }: TemplateBarProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null)
  const [templates, setTemplates] = useState<HTMLTemplate[]>(getAllHTMLTemplates())
  const categories = getHTMLCategories()

  useEffect(() => {
    let filtered = getAllHTMLTemplates()
    if (searchTerm) {
      filtered = searchHTMLTemplates(searchTerm)
    }
    if (selectedCategory) {
      filtered = filtered.filter((t) => t.category === selectedCategory)
    }
    setTemplates(filtered)
  }, [searchTerm, selectedCategory])

  return (
    <div className="border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
      {/* Top Bar */}
      <div className="flex items-center gap-3 px-4 py-2 border-b border-gray-200 dark:border-gray-700">
        <Code2 className="h-4 w-4 text-gray-500 shrink-0" />
        <Input
          placeholder="Search templates..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="h-8 text-sm bg-white dark:bg-gray-800 flex-1 max-w-md"
        />
        <div className="flex gap-2 ml-auto">
          {selectedTemplate && (
            <Button
              onClick={() => onInsert(selectedTemplate.code)}
              size="sm"
              className="bg-orange-600 hover:bg-orange-700 dark:bg-orange-500 dark:hover:bg-orange-600 h-8"
            >
              <Plus className="h-3 w-3 mr-1" />
              Insert at cursor
            </Button>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              onClose()
              onSelectTemplate(null)
              setSelectedCategory(null)
            }}
            className="h-8"
          >
            <X className="h-3 w-3 mr-1" />
            Close
          </Button>
        </div>
      </div>

      {/* Categories + Templates */}
      <div className="flex h-40">
        {/* Left — Categories */}
        <div className="w-1/3 border-r border-gray-200 dark:border-gray-700 overflow-y-auto p-3">
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => setSelectedCategory(null)}
              className={`px-2.5 py-2 rounded-lg text-xs font-medium transition-colors text-center ${
                selectedCategory === null
                  ? "bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300"
                  : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
              }`}
            >
              All
            </button>
            {categories.map((category) => (
              <button
                key={category}
                type="button"
                onClick={() => setSelectedCategory(category)}
                className={`px-2.5 py-2 rounded-lg text-xs font-medium capitalize transition-colors text-center ${
                  selectedCategory === category
                    ? "bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300"
                    : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
                }`}
              >
                {category}
              </button>
            ))}
          </div>
        </div>

        {/* Right — Template grid */}
        <div className="w-2/3 overflow-y-auto p-3">
          <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-2">
            {templates.map((template) => {
              const IconComponent = template.icon
              const isSelected = selectedTemplate?.id === template.id

              return (
                <button
                  key={template.id}
                  type="button"
                  onClick={() => onSelectTemplate(template)}
                  className={`p-2.5 rounded-lg border-2 transition-all text-left ${
                    isSelected
                      ? "border-orange-500 dark:border-orange-400 bg-orange-50 dark:bg-orange-900/20"
                      : "border-gray-200 dark:border-gray-700 hover:border-orange-300 dark:hover:border-orange-600 hover:bg-gray-50 dark:hover:bg-gray-800/50"
                  }`}
                >
                  <div className="flex items-start gap-2">
                    <div
                      className={`p-1.5 rounded shrink-0 ${
                        isSelected
                          ? "bg-orange-100 dark:bg-orange-800/30"
                          : "bg-gray-100 dark:bg-gray-800"
                      }`}
                    >
                      <IconComponent
                        className={`h-3.5 w-3.5 ${
                          isSelected
                            ? "text-orange-600 dark:text-orange-400"
                            : "text-gray-600 dark:text-gray-400"
                        }`}
                      />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p
                        className={`text-xs font-medium truncate ${
                          isSelected
                            ? "text-orange-700 dark:text-orange-300"
                            : "text-gray-900 dark:text-gray-100"
                        }`}
                      >
                        {template.title}
                      </p>
                      <p className="text-[10px] text-gray-500 dark:text-gray-400 truncate mt-0.5">
                        {template.description}
                      </p>
                    </div>
                  </div>
                </button>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}
