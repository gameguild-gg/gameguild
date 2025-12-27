import type React from "react"

interface QuizWrapperProps {
  children: React.ReactNode
  backgroundColor?: string
}

export function QuizWrapper({ children, backgroundColor = "white" }: QuizWrapperProps) {
  const getBackgroundClasses = () => {
    switch (backgroundColor) {
      case "blue":
        return "bg-gradient-to-br from-blue-50/30 to-indigo-50/30"
      case "green":
        return "bg-gradient-to-br from-green-50/30 to-emerald-50/30"
      case "purple":
        return "bg-gradient-to-br from-purple-50/30 to-violet-50/30"
      case "orange":
        return "bg-gradient-to-br from-orange-50/30 to-amber-50/30"
      case "gray":
        return "bg-gradient-to-br from-gray-50/30 to-slate-50/30"
      default:
        return "bg-transparent"
    }
  }

  return (
    <div
      className={`my-8 rounded-lg p-6 transition-colors duration-200 ${getBackgroundClasses()}`}
    >
      {children}
    </div>
  )
}
