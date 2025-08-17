import type React from "react"
import { BookOpen } from "lucide-react"

interface QuizWrapperProps {
  children: React.ReactNode
  backgroundColor?: string
}

export function QuizWrapper({ children, backgroundColor = "white" }: QuizWrapperProps) {
  const getBackgroundClasses = () => {
    switch (backgroundColor) {
      case "blue":
        return "bg-gradient-to-br from-blue-50 to-indigo-50 border-blue-200"
      case "green":
        return "bg-gradient-to-br from-green-50 to-emerald-50 border-green-200"
      case "purple":
        return "bg-gradient-to-br from-purple-50 to-violet-50 border-purple-200"
      case "orange":
        return "bg-gradient-to-br from-orange-50 to-amber-50 border-orange-200"
      case "gray":
        return "bg-gradient-to-br from-gray-50 to-slate-50 border-gray-200"
      default:
        return "bg-white border-gray-200"
    }
  }

  const getHeaderClasses = () => {
    switch (backgroundColor) {
      case "blue":
        return "bg-blue-100 border-blue-200 text-blue-800"
      case "green":
        return "bg-green-100 border-green-200 text-green-800"
      case "purple":
        return "bg-purple-100 border-purple-200 text-purple-800"
      case "orange":
        return "bg-orange-100 border-orange-200 text-orange-800"
      case "gray":
        return "bg-gray-100 border-gray-200 text-gray-800"
      default:
        return "bg-gray-100 border-gray-200 text-gray-800"
    }
  }

  const getIconClasses = () => {
    switch (backgroundColor) {
      case "blue":
        return "bg-blue-500"
      case "green":
        return "bg-green-500"
      case "purple":
        return "bg-purple-500"
      case "orange":
        return "bg-orange-500"
      case "gray":
        return "bg-gray-500"
      default:
        return "bg-gray-500"
    }
  }

  return (
    <div
      className={`my-6 rounded-xl border-2 shadow-sm hover:shadow-md transition-shadow duration-200 relative overflow-hidden ${getBackgroundClasses()}`}
    >
      {/* Quiz indicator header */}
      <div className={`flex items-center gap-2 px-4 py-3 border-b ${getHeaderClasses()}`}>
        <div className={`flex items-center justify-center w-6 h-6 rounded-full ${getIconClasses()}`}>
          <BookOpen className="w-3.5 h-3.5 text-white" />
        </div>
        <span className="text-sm font-medium">Quiz</span>
      </div>

      {/* Quiz content */}
      <div className="p-6">{children}</div>

      {/* Decorative element - only show for colored backgrounds */}
      {backgroundColor !== "white" && (
        <div
          className={`absolute top-0 right-0 w-20 h-20 rounded-full -translate-y-10 translate-x-10 opacity-30 ${backgroundColor === "blue" ? "bg-blue-100" : backgroundColor === "green" ? "bg-green-100" : backgroundColor === "purple" ? "bg-purple-100" : backgroundColor === "orange" ? "bg-orange-100" : "bg-gray-100"}`}
        />
      )}
    </div>
  )
}
