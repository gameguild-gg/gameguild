import type React from "react"

export interface QuizWrapperProps {
  children: React.ReactNode
}

export function QuizWrapper({ children }: QuizWrapperProps) {
  return (
    <div className="my-8 rounded-xl p-6 transition-colors duration-200 bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800 text-gray-900 dark:text-gray-100">
      {children}
    </div>
  )
}
