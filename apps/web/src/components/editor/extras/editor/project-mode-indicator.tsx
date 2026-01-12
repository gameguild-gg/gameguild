"use client"

import { Code, CheckSquare, FileText } from "lucide-react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"

interface ProjectModeIndicatorProps {
  mode: ProjectMode
  className?: string
}

const MODE_CONFIG = {
  "free-page": {
    icon: FileText,
    label: "Free",
    color: "text-blue-600 dark:text-blue-400",
    bg: "bg-blue-50 dark:bg-blue-900/30",
    border: "border-blue-200 dark:border-blue-800"
  },
  "code-page": {
    icon: Code,
    label: "Code",
    color: "text-purple-600 dark:text-purple-400",
    bg: "bg-purple-50 dark:bg-purple-900/30",
    border: "border-purple-200 dark:border-purple-800"
  },
  "quiz-page": {
    icon: CheckSquare,
    label: "Quiz",
    color: "text-green-600 dark:text-green-400",
    bg: "bg-green-50 dark:bg-green-900/30",
    border: "border-green-200 dark:border-green-800"
  }
}

export function ProjectModeIndicator({ mode, className = "" }: ProjectModeIndicatorProps) {
  const config = MODE_CONFIG[mode]
  const Icon = config.icon

  return (
    <div 
      className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-md border text-xs font-medium ${config.bg} ${config.color} ${config.border} ${className}`}
      title={`Project mode: ${config.label}`}
    >
      <Icon className="h-3 w-3" />
      <span>{config.label}</span>
    </div>
  )
}
