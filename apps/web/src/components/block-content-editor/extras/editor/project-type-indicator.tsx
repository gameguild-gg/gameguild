import { Badge } from "@/components/ui/badge"
import { FileText, HelpCircle, LayoutGrid } from "lucide-react"
import { type ProjectType, DEFAULT_PROJECT_TYPE } from "@/components/block-content-editor/lib/storage/editor/project-types"

interface ProjectTypeIndicatorProps {
  type: ProjectType | undefined
  className?: string
}

const TYPE_DISPLAY: Record<ProjectType, { label: string; icon: typeof FileText; className: string }> = {
  document: {
    label: "Document",
    icon: FileText,
    className: "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800",
  },
  quiz: {
    label: "Quiz",
    icon: HelpCircle,
    className: "bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-900/30 dark:text-purple-300 dark:border-purple-800",
  },
  general: {
    label: "General",
    icon: LayoutGrid,
    className: "bg-gray-50 text-gray-700 border-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-700",
  },
}

export function ProjectTypeIndicator({ type, className = "" }: ProjectTypeIndicatorProps) {
  const config = TYPE_DISPLAY[type ?? DEFAULT_PROJECT_TYPE]
  const Icon = config.icon
  return (
    <Badge variant="outline" className={`${config.className} ${className} gap-1`}>
      <Icon className="h-3 w-3" />
      {config.label}
    </Badge>
  )
}
