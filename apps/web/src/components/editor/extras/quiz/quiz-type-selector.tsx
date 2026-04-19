/**
 * Quiz Type Selector
 * UI for selecting the type of quiz question to create
 */

"use client"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import {
  CheckCircle,
  List,
  ToggleLeft,
  Type,
  ArrowRight,
  HelpCircle,
  Target,
  Zap,
  Layers,
  Star,
  FileText,
  Sigma,
  Hash,
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
} from "./types"

export interface QuizTypeTemplate {
  type: QuizEntryType
  title: string
  description: string
  icon: React.ComponentType<{ className?: string }>
  preview: string
  createEntry: () => QuizEntry
}

export const QUIZ_TEMPLATES: QuizTypeTemplate[] = [
  {
    type: QuizEntryType.SingleChoice,
    title: "Single Choice",
    description: "One correct answer from multiple options",
    icon: CheckCircle,
    preview: "What is 2+2? ○ 3 ○ 4 ○ 5",
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
    title: "True/False",
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
    preview: "The _Jupiter_ is the largest planet.",
    createEntry: () => createFillInTheBlankEntry("The _Jupiter_ is the largest planet in our solar system."),
  },
  {
    type: QuizEntryType.ShortAnswer,
    title: "Short Answer",
    description: "Brief written response",
    icon: HelpCircle,
    preview: "Answer in a few words...",
    createEntry: () => createShortAnswerEntry("What is the capital of Japan?"),
  },
  {
    type: QuizEntryType.Essay,
    title: "Essay",
    description: "Extended written response",
    icon: FileText,
    preview: "Write a paragraph about...",
    createEntry: () => createEssayEntry("Explain the process of photosynthesis."),
  },
  {
    type: QuizEntryType.Matching,
    title: "Matching",
    description: "Connect related items from two columns",
    icon: Target,
    preview: "Match countries with capitals",
    createEntry: () => createMatchingEntry("Match each country with its capital city:"),
  },
  {
    type: QuizEntryType.Ordering,
    title: "Ordering",
    description: "Arrange items in correct sequence",
    icon: List,
    preview: "Put events in order",
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
    preview: "Rate from 1 to 5",
    createEntry: () => createRatingEntry("How satisfied are you with this course?"),
  },
  {
    type: QuizEntryType.Numeric,
    title: "Numeric",
    description: "Compute numeric result from a formula with variables",
    icon: Hash,
    preview: "If x=3, y=5: x² + y = ?",
    createEntry: () => createNumericEntry("Calculate the result of the formula given the variable values:"),
  },
  {
    type: QuizEntryType.Formula,
    title: "Formula",
    description: "Discover the formula from variables and expected result",
    icon: Sigma,
    preview: "x=3, y=5: ? = 14 → find the formula",
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

interface QuizTypeSelectorProps {
  onSelect: (entry: QuizEntry) => void
  onCancel: () => void
}

export function QuizTypeSelector({ onSelect, onCancel }: QuizTypeSelectorProps) {
  return (
    <div className="p-6 border-b bg-gray-50 dark:bg-gray-900 max-h-[80vh] overflow-y-auto">
      <div className="text-center mb-6">
        <div className="mx-auto mb-3 p-3 bg-blue-100 dark:bg-blue-900/30 rounded-full w-fit">
          <Zap className="h-8 w-8 text-blue-600 dark:text-blue-400" />
        </div>
        <h3 className="text-lg font-semibold mb-2 text-gray-900 dark:text-gray-100">Choose a Question Type</h3>
        <p className="text-gray-600 dark:text-gray-400">Select the type of question you want to create</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 max-w-6xl mx-auto">
        {QUIZ_TEMPLATES.map((template) => {
          const IconComponent = template.icon
          return (
            <Card
              key={template.type}
              className="cursor-pointer hover:shadow-md transition-shadow border-2 hover:border-blue-300 dark:hover:border-blue-600 h-full flex flex-col"
              onClick={() => onSelect(template.createEntry())}
            >
              <CardHeader className="text-center pb-3 flex-1 flex flex-col items-center">
                <div className="mx-auto mb-2 p-3 bg-blue-100 dark:bg-blue-900/30 rounded-full w-fit">
                  <IconComponent className="h-6 w-6 text-blue-600 dark:text-blue-400" />
                </div>
                <CardTitle className="text-lg">{template.title}</CardTitle>
                <CardDescription className="text-sm">{template.description}</CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="bg-gray-100 dark:bg-gray-800 rounded p-3 mb-3 text-center">
                  <code className="text-sm text-gray-700 dark:text-gray-300 font-mono">{template.preview}</code>
                </div>
                <Button className="w-full bg-transparent" variant="outline">
                  <span>Select</span>
                  <ArrowRight className="h-4 w-4 ml-2" />
                </Button>
              </CardContent>
            </Card>
          )
        })}
      </div>

      <div className="text-center mt-6">
        <Button variant="ghost" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  )
}
