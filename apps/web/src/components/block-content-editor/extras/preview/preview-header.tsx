"use client"

interface PreviewHeaderProps {
  projectName: string
  className?: string
}

export function PreviewHeader({ projectName, className = "" }: PreviewHeaderProps) {
  return (
    <div className={`mb-8 pb-4 border-b border-gray-200 dark:border-gray-700 ${className}`}>
      <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">{projectName}</h1>
      <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
          />
        </svg>
        <span>Preview Mode</span>
      </div>
    </div>
  )
} 