"use client"

import type React from "react"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { ArrowRight, Search } from "lucide-react"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import { getAllTemplates, searchTemplates, type MermaidTemplate } from "./templates/template-loader"

interface MermaidTemplateSelectorProps {
  onSelect: (template: { type: MermaidData["type"]; code: string }) => void
  onCancel: () => void
}

export function MermaidTemplateSelector({ onSelect, onCancel }: MermaidTemplateSelectorProps) {
  const [templates, setTemplates] = useState<MermaidTemplate[]>([])
  const [filteredTemplates, setFilteredTemplates] = useState<MermaidTemplate[]>([])
  const [searchTerm, setSearchTerm] = useState("")
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const allTemplates = await getAllTemplates()
        setTemplates(allTemplates)
        setFilteredTemplates(allTemplates)
        setLoading(false)
      } catch (error) {
        console.error("Failed to load templates:", error)
        setLoading(false)
      }
    }

    loadTemplates()
  }, [])

  useEffect(() => {
    const filterTemplates = async () => {
      if (searchTerm.trim() === "") {
        setFilteredTemplates(templates)
      } else {
        const results = await searchTemplates(searchTerm)
        setFilteredTemplates(results)
      }
    }

    filterTemplates()
  }, [searchTerm, templates])

  const handleSelectTemplate = (template: MermaidTemplate) => {
    onSelect({
      type: template.type,
      code: template.code,
    })
  }

  return (
    <div className="p-6 border-b bg-gray-50 h-[80vh] flex flex-col">
      <div className="text-center mb-4">
        <h3 className="text-lg font-semibold mb-2">Choose a Diagram Template</h3>
        <p className="text-sm text-gray-600">Select a template to get started with your Mermaid diagram</p>
      </div>

      <div className="mb-4 relative">
        <Search className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
        <Input
          placeholder="Search templates..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-10 h-8 text-sm"
        />
      </div>

      <div className="flex-1 overflow-y-auto">
        {loading ? (
          <div className="text-center py-8">
            <p className="text-gray-500">Loading templates...</p>
          </div>
        ) : filteredTemplates.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-gray-500">No templates found matching "{searchTerm}"</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 max-w-7xl mx-auto pb-4">
            {filteredTemplates.map((template) => {
              const IconComponent = template.icon
              return (
                <Card
                  key={template.id}
                  className="cursor-pointer hover:shadow-md transition-shadow border-2 hover:border-blue-300 flex flex-col"
                  onClick={() => handleSelectTemplate(template)}
                >
                  <CardHeader className="text-center pb-3 flex-1">
                    <div className="mx-auto mb-2 p-2 bg-blue-100 rounded-full w-fit">
                      <IconComponent className="h-5 w-5 text-blue-600" />
                    </div>
                    <CardTitle className="text-sm">{template.title}</CardTitle>
                    <CardDescription className="text-xs line-clamp-2">{template.description}</CardDescription>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="bg-gray-100 rounded p-2 mb-2 text-center">
                      <code className="text-xs text-gray-700 font-mono">{template.preview}</code>
                    </div>
                    <Button className="w-full h-8 text-xs" variant="outline" size="sm">
                      <span>Select</span>
                      <ArrowRight className="h-3 w-3 ml-1" />
                    </Button>
                  </CardContent>
                </Card>
              )
            })}
          </div>
        )}
      </div>

      <div className="text-center pt-4 border-t">
        <Button variant="ghost" onClick={onCancel} size="sm">
          Cancel
        </Button>
      </div>
    </div>
  )
}
