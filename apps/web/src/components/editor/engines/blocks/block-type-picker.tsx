"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip"
import { BLOCK_REGISTRY, BLOCK_CELL_TYPES, type BlockCellType } from "./block-component-registry"
import type { Block } from "@/lib/storage/editor/block-structure"
import { Search, LayoutGrid, FileText, ArrowRight } from "lucide-react"
import {
  CheckCircle,
  List,
  ToggleLeft,
  Type,
  HelpCircle,
  Target,
  Layers,
  Star,
  Hash,
  Sigma,
  Crosshair,
  Highlighter,
} from "lucide-react"
import {
  QuizEntryType,
  createSingleChoiceEntry,
  createMultipleChoiceEntry,
  createTrueFalseEntry,
  createFillInTheBlankEntry,
  createShortAnswerEntry,
  createEssayEntry,
  createMatchingEntry,
  createOrderingEntry,
  createCategorizationEntry,
  createRatingEntry,
  createNumericEntry,
  createFormulaEntry,
  createHotspotEntry,
  createHighlightEntry,
  type QuizEntry,
} from "@/components/editor/extras/quiz/types"

// ============================================================================
// Quiz Templates
// ============================================================================

interface QuizTemplate {
  type: QuizEntryType
  title: string
  description: string
  icon: React.ComponentType<{ className?: string }>
  preview: string
  createEntry: () => QuizEntry
}

const QUIZ_TEMPLATES: QuizTemplate[] = [
  {
    type: QuizEntryType.SingleChoice,
    title: "Single Choice",
    description: "One correct answer from multiple options",
    icon: CheckCircle,
    preview: "What is 2+2? ○ 3 ● 4 ○ 5",
    createEntry: () => createSingleChoiceEntry("What is the capital of France?"),
  },
  {
    type: QuizEntryType.MultipleChoice,
    title: "Multiple Choice",
    description: "Multiple correct answers possible",
    icon: List,
    preview: "Select all that apply: ☑ ☐ ☑",
    createEntry: () => createMultipleChoiceEntry("Which of these are prime numbers?"),
  },
  {
    type: QuizEntryType.TrueFalse,
    title: "True / False",
    description: "Binary choice between true and false",
    icon: ToggleLeft,
    preview: "The Earth is flat. True / False",
    createEntry: () => createTrueFalseEntry("The Earth revolves around the Sun."),
  },
  {
    type: QuizEntryType.FillInTheBlank,
    title: "Fill in the Blank",
    description: "Complete sentences with missing words",
    icon: Type,
    preview: "The ___ is the largest planet.",
    createEntry: () => createFillInTheBlankEntry("The ___ is the largest planet in our solar system."),
  },
  {
    type: QuizEntryType.ShortAnswer,
    title: "Short Answer",
    description: "Brief written response",
    icon: HelpCircle,
    preview: "Answer in a few words…",
    createEntry: () => createShortAnswerEntry("What is the capital of Japan?"),
  },
  {
    type: QuizEntryType.Essay,
    title: "Essay",
    description: "Extended written response",
    icon: FileText,
    preview: "Write a paragraph about…",
    createEntry: () => createEssayEntry("Explain the process of photosynthesis."),
  },
  {
    type: QuizEntryType.Matching,
    title: "Matching",
    description: "Connect related items from two columns",
    icon: Target,
    preview: "Match countries → capitals",
    createEntry: () => createMatchingEntry("Match each country with its capital city:"),
  },
  {
    type: QuizEntryType.Ordering,
    title: "Ordering",
    description: "Arrange items in correct sequence",
    icon: List,
    preview: "Put events in order: 1 → 2 → 3",
    createEntry: () => createOrderingEntry("Arrange these events in chronological order:"),
  },
  {
    type: QuizEntryType.Categorization,
    title: "Categorization",
    description: "Sort items into categories",
    icon: Layers,
    preview: "Drag items to categories",
    createEntry: () => createCategorizationEntry("Categorize the following items:"),
  },
  {
    type: QuizEntryType.Rating,
    title: "Rating Scale",
    description: "Rate on a numerical scale",
    icon: Star,
    preview: "★ ★ ★ ☆ ☆  Rate 1–5",
    createEntry: () => createRatingEntry("How satisfied are you with this course?"),
  },
  {
    type: QuizEntryType.Numeric,
    title: "Numeric",
    description: "Compute a numeric result from a formula",
    icon: Hash,
    preview: "If x=3, y=5: x² + y = ?",
    createEntry: () => createNumericEntry("Calculate the result of the formula given the variable values:"),
  },
  {
    type: QuizEntryType.Formula,
    title: "Formula",
    description: "Discover the formula from variables and result",
    icon: Sigma,
    preview: "x=3, y=5: ? = 14 → find it",
    createEntry: () => createFormulaEntry("Discover the formula that produces the given result:"),
  },
  {
    type: QuizEntryType.Hotspot,
    title: "Hotspot",
    description: "Click on the correct area of an image",
    icon: Crosshair,
    preview: "Click on the correct point ⊕",
    createEntry: () => createHotspotEntry("Click on the correct location in the image:"),
  },
  {
    type: QuizEntryType.Highlight,
    title: "Highlight",
    description: "Select the correct parts of a text",
    icon: Highlighter,
    preview: "The ██████ is the powerhouse",
    createEntry: () => createHighlightEntry("Highlight the correct words in the text below:"),
  },
]

// ============================================================================
// Block categories for better organization
// ============================================================================

const BLOCK_CATEGORIES: { label: string; types: BlockCellType[] }[] = [
  { label: "Content", types: ["rt", "md", "html", "hdr"] },
  { label: "Media", types: ["img", "vid", "aud", "gal", "yt", "spot"] },
  { label: "Interactive", types: ["quiz", "code", "btn"] },
  { label: "Data & Diagrams", types: ["mmd", "vega", "tbl", "pres"] },
  { label: "Structure", types: ["div", "adm", "src", "proj"] },
]

// ============================================================================
// Component
// ============================================================================

interface BlockTypePickerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelect: (block: Block) => void
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
    const block = config.createEmpty()
    onSelect(block)
    onOpenChange(false)
    setSearch("")
    setTab("blocks")
  }

  const handleSelectQuizTemplate = (template: QuizTemplate) => {
    const entry = template.createEntry()
    const block: Block = { id: crypto.randomUUID(), type: "quiz", data: entry }
    onSelect(block)
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
