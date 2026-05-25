"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Save, FileText, Plus, X } from "lucide-react"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import rehypeRaw from "rehype-raw"
import { useTheme } from "next-themes"
import { BaseMonacoEditor } from "@/components/block-content-editor/lib/monaco"
import type { MarkdownData } from "@/components/block-content-editor/nodes/markdown-node"
import { useEditorSettings } from "../settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { getAllTemplates, searchTemplates, type MarkdownTemplate } from "./markdown-templates"
import { Input } from "@/components/ui/input"
import { useMarkdownComponents } from "./markdown-components"

interface MarkdownEditorProps {
  initialData?: MarkdownData
  onSave: (data: MarkdownData) => void
  onCancel: () => void
}

export function MarkdownEditor({ initialData, onSave, onCancel }: MarkdownEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  const markdownComponents = useMarkdownComponents()
  
  const [content, setContent] = useState(initialData?.content || "")
  const [showTemplates, setShowTemplates] = useState(!initialData?.content)
  const [selectedTemplate, setSelectedTemplate] = useState<MarkdownTemplate | null>(null)
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState("")
  const [templates, setTemplates] = useState<MarkdownTemplate[]>([])
  const settings = useEditorSettings("markdown")
  const editorRef = useRef<any>(null)

  // Get unique categories
  const categories = Array.from(new Set(getAllTemplates().map(t => t.category)))

  // Load templates
  useEffect(() => {
    const allTemplates = getAllTemplates()
    setTemplates(allTemplates)
  }, [])

  // Filter templates based on search
  useEffect(() => {
    let filtered = getAllTemplates()
    
    if (searchTerm) {
      filtered = searchTemplates(searchTerm)
    }
    
    if (selectedCategory) {
      filtered = filtered.filter(t => t.category === selectedCategory)
    }
    
    setTemplates(filtered)
  }, [searchTerm, selectedCategory])

  const handleSave = () => {
    onSave({
      content,
      title: initialData?.title,
      caption: initialData?.caption,
    })
  }

  const handleCancel = () => {
    onCancel()
  }

  const handleEditorMount = (editor: any) => {
    editorRef.current = editor
  }

  const insertTemplateAtCursor = () => {
    if (!selectedTemplate || !editorRef.current) return

    const editor = editorRef.current
    const position = editor.getPosition()
    const model = editor.getModel()
    
    if (model && position) {
      const range = {
        startLineNumber: position.lineNumber,
        startColumn: position.column,
        endLineNumber: position.lineNumber,
        endColumn: position.column,
      }
      
      const text = selectedTemplate.code
      editor.executeEdits("insert-template", [{
        range,
        text: text,
        forceMoveMarkers: true,
      }])
      
      // Update content
      setContent(model.getValue())
      
      // Clear selection but keep templates open
      setSelectedTemplate(null)
      
      // Focus editor
      editor.focus()
    }
  }

  return (
    <BlockEditorShell
      settings={settings}
      onClose={handleCancel}
      icon={<FileText className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Markdown Editor"
      headerActions={
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowTemplates(!showTemplates)}
          className="border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          <Plus className="h-4 w-4 mr-1" />
          Templates
        </Button>
      }
      secondaryHeader={
        showTemplates ? (
          <div className="">
            {/* Top Bar - Search and Actions */}
            <div className="flex items-center gap-3 px-4 py-2 border-b border-gray-200 dark:border-gray-700">
              <FileText className="h-4 w-4 text-gray-500 shrink-0" />
              <Input
                placeholder="Search templates..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="h-8 text-sm bg-white dark:bg-gray-800 flex-1 max-w-md"
              />
              <div className="flex gap-2 ml-auto">
                {selectedTemplate && (
                  <Button
                    onClick={insertTemplateAtCursor}
                    size="sm"
                    className="bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 h-8"
                  >
                    <Plus className="h-3 w-3 mr-1" />
                    Insert at cursor
                  </Button>
                )}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setShowTemplates(false)
                    setSelectedTemplate(null)
                    setSelectedCategory(null)
                  }}
                  className="h-8"
                >
                  <X className="h-3 w-3 mr-1" />
                  Close
                </Button>
              </div>
            </div>

            {/* Bottom - Categories (1/3) and Templates Grid (2/3) */}
            <div className="flex h-40">
              {/* Left - Categories */}
              <div className="w-1/3 border-r border-gray-200 dark:border-gray-700 overflow-y-auto p-3">
                <div className="grid grid-cols-2 gap-2">
                  <button
                    onClick={() => setSelectedCategory(null)}
                    className={`px-2.5 py-2 rounded-lg text-xs font-medium transition-colors text-center ${
                      selectedCategory === null
                        ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                        : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
                    }`}
                  >
                    All
                  </button>
                  {categories.map((category) => (
                    <button
                      key={category}
                      onClick={() => setSelectedCategory(category)}
                      className={`px-2.5 py-2 rounded-lg text-xs font-medium capitalize transition-colors text-center ${
                        selectedCategory === category
                          ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                          : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
                      }`}
                    >
                      {category}
                    </button>
                  ))}
                </div>
              </div>

              {/* Right - Templates Grid */}
              <div className="w-2/3 overflow-y-auto p-3">
                <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-2">
                  {templates.map((template) => {
                    const IconComponent = template.icon
                    const isSelected = selectedTemplate?.id === template.id
                    
                    return (
                      <button
                        key={template.id}
                        onClick={() => setSelectedTemplate(template)}
                        className={`p-2.5 rounded-lg border-2 transition-all text-left ${
                          isSelected
                            ? "border-blue-500 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/20"
                            : "border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 hover:bg-gray-50 dark:hover:bg-gray-800/50"
                        }`}
                      >
                        <div className="flex items-start gap-2">
                          <div className={`p-1.5 rounded shrink-0 ${
                            isSelected
                              ? "bg-blue-100 dark:bg-blue-800/30"
                              : "bg-gray-100 dark:bg-gray-800"
                          }`}>
                            <IconComponent className={`h-3.5 w-3.5 ${
                              isSelected
                                ? "text-blue-600 dark:text-blue-400"
                                : "text-gray-600 dark:text-gray-400"
                            }`} />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className={`text-xs font-medium truncate ${
                              isSelected
                                ? "text-blue-700 dark:text-blue-300"
                                : "text-gray-900 dark:text-gray-100"
                            }`}>
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
        ) : undefined
      }
      footer={
        <div className="flex gap-2 justify-end">
          <Button
            variant="outline"
            onClick={handleCancel}
            disabled={showTemplates && selectedTemplate !== null}
            className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            disabled={showTemplates && selectedTemplate !== null}
            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Save className="h-4 w-4" />
            Save Markdown
          </Button>
        </div>
      }
    >
      {/* Main Content */}
      <div className="flex-1 overflow-hidden flex">
          {/* Left Panel - Monaco Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Editor</h3>
            </div>
            
            <div className="flex-1 overflow-hidden">
              <BaseMonacoEditor
                language="markdown"
                value={content}
                onChange={(value) => setContent(value || "")}
                onMount={handleEditorMount}
                isDark={isDarkMode}
                options={settings.editor}
                extraOptions={{ roundedSelection: true }}
              />
            </div>
          </div>

          {/* Right Panel - Preview */}
          <div className="w-1/2 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center justify-between">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Live Preview</h3>
              {selectedTemplate && (
                <div className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
                  <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded">
                    Preview: {selectedTemplate.title}
                  </span>
                </div>
              )}
            </div>
            
            <div className="flex-1 overflow-auto p-6 bg-white dark:bg-gray-950">
              {selectedTemplate ? (
                <div className="space-y-4">
                  <div className="p-4 bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800 rounded-lg">
                    <p className="text-sm text-blue-800 dark:text-blue-200 font-medium mb-2">
                      Template Preview: {selectedTemplate.title}
                    </p>
                    <p className="text-xs text-blue-600 dark:text-blue-300">
                      {selectedTemplate.description}
                    </p>
                  </div>
                  <ReactMarkdown 
                    remarkPlugins={[remarkGfm]}
                    rehypePlugins={[rehypeRaw]}
                    components={markdownComponents}
                  >
                    {selectedTemplate.code}
                  </ReactMarkdown>
                </div>
              ) : content ? (
                <ReactMarkdown 
                  remarkPlugins={[remarkGfm]}
                  rehypePlugins={[rehypeRaw]}
                  components={markdownComponents}
                >
                  {content}
                </ReactMarkdown>
              ) : (
                <p className="text-gray-400 dark:text-gray-600 italic">
                  Your markdown preview will appear here...
                </p>
              )}
            </div>
          </div>
        </div>
    </BlockEditorShell>
  )
}
