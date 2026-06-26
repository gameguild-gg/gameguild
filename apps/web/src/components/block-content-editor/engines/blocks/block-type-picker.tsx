"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip"
import { BLOCK_REGISTRY, BLOCK_CELL_TYPES, type BlockCellType } from "./block-component-registry"
import type { Block } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { Search, LayoutGrid, FileText } from "lucide-react"
import { QUIZ_TEMPLATES, type QuizTypeTemplate } from "@/components/block-content-editor/extras/quiz/quiz-type-selector"

// ============================================================================
// Block categories for better organization
// ============================================================================

const BLOCK_CATEGORIES: { label: string; types: BlockCellType[] }[] = [
  { label: "Content", types: ["rich-text", "markdown", "html"] },
  { label: "Media", types: ["image", "video", "audio", "gallery"] },
  { label: "Interactive", types: ["quiz", "code-studio"] },
  { label: "Data & Diagrams", types: ["mermaid", "vega-lite"] },
  { label: "Structure", types: ["project"] },
]

// ============================================================================
// Component
// ============================================================================

interface BlockTypePickerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /**
   * Called with a factory that builds the selected block given an id —
   * the caller is responsible for computing the next sequential id (e.g.
   * via `nextBlockId(currentBlocks)`).
   */
  onSelect: (factory: (id: string) => Block) => void
  allowedBlockTypes?: BlockCellType[]
  /** Which tab to show by default */
  defaultTab?: "blocks" | "templates"
  /** Hide the Block Types tab entirely (e.g. when only 1 block type) */
  hideBlockTypesTab?: boolean
}

export function BlockTypePicker({ open, onOpenChange, onSelect, allowedBlockTypes, defaultTab = "blocks", hideBlockTypesTab }: BlockTypePickerProps) {
  const effectiveDefault = hideBlockTypesTab ? "templates" : defaultTab
  const [search, setSearch] = useState("")
  const [tab, setTab] = useState<"blocks" | "templates">(effectiveDefault)

  const filtered = BLOCK_CELL_TYPES.filter((type) => {
    if (allowedBlockTypes && !allowedBlockTypes.includes(type)) return false
    if (!search.trim()) return true
    const config = BLOCK_REGISTRY[type]
    const q = search.toLowerCase()
    return (
      config.label.toLowerCase().includes(q) ||
      config.description.toLowerCase().includes(q) ||
      type.includes(q)
    )
  })

  const filteredTemplates = QUIZ_TEMPLATES.filter((t) => {
    if (!search.trim()) return true
    const q = search.toLowerCase()
    return (
      t.title.toLowerCase().includes(q) ||
      t.description.toLowerCase().includes(q) ||
      t.preview.toLowerCase().includes(q)
    )
  })

  const handleSelect = (type: BlockCellType) => {
    const config = BLOCK_REGISTRY[type]
    onSelect((id) => config.createEmpty(id))
    onOpenChange(false)
    setSearch("")
    setTab("blocks")
  }

  const handleSelectQuizTemplate = (template: QuizTypeTemplate) => {
    onSelect((id) => ({ id, type: "quiz", data: template.createEntry() }))
    onOpenChange(false)
    setSearch("")
    setTab(effectiveDefault)
  }

  const handleClose = (v: boolean) => {
    onOpenChange(v)
    if (!v) {
      setSearch("")
      setTab(effectiveDefault)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-6xl w-[95vw] h-[80vh] overflow-hidden flex flex-col p-0">
        <DialogHeader className="px-6 pt-5 pb-0">
          <DialogTitle className="text-lg">Add Block</DialogTitle>
        </DialogHeader>

        <Tabs value={tab} onValueChange={(v) => setTab(v as "blocks" | "templates")} className="flex flex-col flex-1 overflow-hidden">
          <div className="px-6 pt-2 pb-0 flex flex-col gap-2">
            <TabsList className={`w-full ${hideBlockTypesTab ? '' : 'grid grid-cols-2'}`}>
              {!hideBlockTypesTab && (
                <TabsTrigger value="blocks" className="gap-2">
                  <LayoutGrid className="h-4 w-4" />
                  Block Types
                </TabsTrigger>
              )}
              <TabsTrigger value="templates" className="gap-2">
                <FileText className="h-4 w-4" />
                Templates
                <Badge variant="secondary" className="ml-1 text-[10px] px-1.5 py-0">Quiz</Badge>
              </TabsTrigger>
            </TabsList>

            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder={tab === "blocks" ? "Search block types…" : "Search templates…"}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 h-9"
                autoFocus
              />
            </div>
          </div>

          {/* ============ Block Types Tab ============ */}
          <TabsContent value="blocks" className="mt-0 px-6 pb-5 overflow-y-auto">
            <TooltipProvider delayDuration={300}>
              {search.trim() ? (
                /* Flat filtered grid */
                <div className="grid grid-cols-3 sm:grid-cols-5 md:grid-cols-7 gap-2 pt-2">
                  {filtered.map((type) => {
                    const config = BLOCK_REGISTRY[type]
                    const Icon = config.icon
                    return (
                      <Tooltip key={type}>
                        <TooltipTrigger asChild>
                          <button
                            type="button"
                            onClick={() => handleSelect(type)}
                            className="flex flex-col items-center gap-1.5 p-3 rounded-lg border border-border hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/30 transition-all text-center group"
                          >
                            <Icon className="h-5 w-5 text-muted-foreground group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors" />
                            <span className="text-xs font-medium leading-tight">{config.label}</span>
                          </button>
                        </TooltipTrigger>
                        <TooltipContent side="bottom"><p>{config.description}</p></TooltipContent>
                      </Tooltip>
                    )
                  })}
                </div>
              ) : (
                /* Categorized compact view */
                <div className="space-y-3 pt-2">
                  {BLOCK_CATEGORIES.map((cat) => {
                    const types = cat.types.filter((t) => filtered.includes(t))
                    if (types.length === 0) return null
                    return (
                      <div key={cat.label}>
                        <h3 className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">{cat.label}</h3>
                        <div className="grid grid-cols-3 sm:grid-cols-5 md:grid-cols-7 gap-2">
                          {types.map((type) => {
                            const config = BLOCK_REGISTRY[type]
                            const Icon = config.icon
                            return (
                              <Tooltip key={type}>
                                <TooltipTrigger asChild>
                                  <button
                                    type="button"
                                    onClick={() => handleSelect(type)}
                                    className="flex flex-col items-center gap-1.5 p-3 rounded-lg border border-border hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/30 transition-all text-center group"
                                  >
                                    <Icon className="h-5 w-5 text-muted-foreground group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors" />
                                    <span className="text-xs font-medium leading-tight">{config.label}</span>
                                  </button>
                                </TooltipTrigger>
                                <TooltipContent side="bottom"><p>{config.description}</p></TooltipContent>
                              </Tooltip>
                            )
                          })}
                        </div>
                      </div>
                    )
                  })}
                </div>
              )}
              {filtered.length === 0 && (
                <div className="py-8 text-center text-sm text-muted-foreground">
                  No block types matching &ldquo;{search}&rdquo;
                </div>
              )}
            </TooltipProvider>
          </TabsContent>

          {/* ============ Templates Tab ============ */}
          <TabsContent value="templates" className="mt-0 px-6 pb-5 overflow-y-auto">
            <div className="pt-2 mb-2">
              <p className="text-xs text-muted-foreground">Pick a quiz type — the editor opens pre-filled, ready to customize.</p>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-2">
              {filteredTemplates.map((template) => {
                const Icon = template.icon
                return (
                  <button
                    key={template.type}
                    type="button"
                    onClick={() => handleSelectQuizTemplate(template)}
                    className="flex flex-col items-center gap-1.5 p-3 rounded-lg border border-border hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/30 transition-all text-center group"
                  >
                    <Icon className="h-5 w-5 text-muted-foreground group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors" />
                    <span className="text-xs font-medium leading-tight">{template.title}</span>
                    <span className="text-[10px] text-muted-foreground/70 leading-tight">{template.description}</span>
                  </button>
                )
              })}
            </div>

            {filteredTemplates.length === 0 && (
              <div className="py-8 text-center text-sm text-muted-foreground">
                No templates matching &ldquo;{search}&rdquo;
              </div>
            )}
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
