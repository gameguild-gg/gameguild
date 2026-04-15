"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { CheckCircle, List, ToggleLeft, Type, ArrowRight, HelpCircle, Target, Zap } from "lucide-react"

interface QuizTemplate {
  type: "multiple-choice" | "true-false" | "fill-blank" | "short-answer" | "matching" | "ordering"
  title: string
  description: string
  icon: React.ComponentType<{ className?: string }>
  preview: string
  defaultData: {
    questions: any[]
    settings: any
  }
}

const quizTemplates: QuizTemplate[] = [
  {
    type: "multiple-choice",
    title: "Multiple Choice",
    description: "Questions with multiple options where users select the correct answer",
    icon: CheckCircle,
    preview: "What is 2+2? A) 3 B) 4 C) 5",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "What is the capital of France?",
          type: "multiple-choice",
          options: ["London", "Berlin", "Paris", "Madrid"],
          correctAnswer: 2,
          explanation: "Paris is the capital and largest city of France.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        shuffleOptions: false,
        timeLimit: 0,
      },
    },
  },
  {
    type: "true-false",
    title: "True/False",
    description: "Simple binary choice questions with true or false answers",
    icon: ToggleLeft,
    preview: "The Earth is flat. True / False",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "The Earth revolves around the Sun.",
          type: "true-false",
          correctAnswer: true,
          explanation: "Yes, the Earth orbits around the Sun in our solar system.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        timeLimit: 0,
      },
    },
  },
  {
    type: "fill-blank",
    title: "Fill in the Blank",
    description: "Questions where users complete sentences by filling in missing words",
    icon: Type,
    preview: "The ___ is the largest planet in our solar system.",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "The ___ is the largest planet in our solar system.",
          type: "fill-blank",
          correctAnswer: "Jupiter",
          explanation: "Jupiter is indeed the largest planet in our solar system.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        caseSensitive: false,
        timeLimit: 0,
      },
    },
  },
  {
    type: "short-answer",
    title: "Short Answer",
    description: "Open-ended questions requiring brief written responses",
    icon: HelpCircle,
    preview: "Explain the water cycle in 2-3 sentences.",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "Explain the process of photosynthesis.",
          type: "short-answer",
          sampleAnswer:
            "Photosynthesis is the process by which plants convert sunlight, carbon dioxide, and water into glucose and oxygen.",
          explanation: "This is a fundamental biological process that sustains most life on Earth.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        timeLimit: 0,
      },
    },
  },
  {
    type: "matching",
    title: "Matching",
    description: "Connect related items from two columns",
    icon: Target,
    preview: "Match countries with their capitals",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "Match each country with its capital city:",
          type: "matching",
          leftColumn: ["France", "Germany", "Italy", "Spain"],
          rightColumn: ["Berlin", "Madrid", "Paris", "Rome"],
          correctMatches: [
            { left: "France", right: "Paris" },
            { left: "Germany", right: "Berlin" },
            { left: "Italy", right: "Rome" },
            { left: "Spain", right: "Madrid" },
          ],
          explanation: "These are the capital cities of major European countries.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        shuffleOptions: true,
        timeLimit: 0,
      },
    },
  },
  {
    type: "ordering",
    title: "Ordering",
    description: "Arrange items in the correct sequence or priority",
    icon: List,
    preview: "Put these events in chronological order",
    defaultData: {
      questions: [
        {
          id: "1",
          question: "Arrange these historical events in chronological order:",
          type: "ordering",
          items: ["World War I", "Renaissance", "Industrial Revolution", "World War II"],
          correctOrder: ["Renaissance", "Industrial Revolution", "World War I", "World War II"],
          explanation:
            "This represents the correct chronological sequence of these major historical periods and events.",
        },
      ],
      settings: {
        showResults: true,
        allowRetry: true,
        shuffleQuestions: false,
        shuffleItems: true,
        timeLimit: 0,
      },
    },
  },
]

interface QuizTypeSelectorProps {
  onSelect: (template: QuizTemplate) => void
  onCancel: () => void
}

export function QuizTypeSelector({ onSelect, onCancel }: QuizTypeSelectorProps) {
  return (
    <div className="p-6 border-b bg-gray-50 max-h-[80vh] overflow-y-auto">
      <div className="text-center mb-6">
        <div className="mx-auto mb-3 p-3 bg-blue-100 rounded-full w-fit">
          <Zap className="h-8 w-8 text-blue-600" />
        </div>
        <h3 className="text-lg font-semibold mb-2">Choose a Quiz Type</h3>
        <p className="text-gray-600">Select the type of quiz you want to create</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 max-w-6xl mx-auto">
        {quizTemplates.map((template) => {
          const IconComponent = template.icon
          return (
            <Card
              key={template.type}
              className="cursor-pointer hover:shadow-md transition-shadow border-2 hover:border-blue-300"
              onClick={() => onSelect(template)}
            >
              <CardHeader className="text-center pb-3">
                <div className="mx-auto mb-2 p-3 bg-blue-100 rounded-full w-fit">
                  <IconComponent className="h-6 w-6 text-blue-600" />
                </div>
                <CardTitle className="text-lg">{template.title}</CardTitle>
                <CardDescription className="text-sm">{template.description}</CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="bg-gray-100 rounded p-3 mb-3 text-center">
                  <code className="text-sm text-gray-700 font-mono">{template.preview}</code>
                </div>
                <Button className="w-full bg-transparent" variant="outline">
                  <span>Select Quiz Type</span>
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
