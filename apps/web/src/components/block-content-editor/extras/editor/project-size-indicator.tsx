"use client"

import { HardDrive } from "lucide-react"

interface ProjectSizeIndicatorProps {
  currentProjectSize: number
  currentProjectAssetsSize: number
  formatSize: (sizeInKB: number) => string
  getSizeIndicatorColor: () => string
  onClick: () => void
}

export function ProjectSizeIndicator({
  currentProjectSize,
  currentProjectAssetsSize,
  formatSize,
  getSizeIndicatorColor,
  onClick,
}: ProjectSizeIndicatorProps) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-2 px-3 py-1.5 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors cursor-pointer"
      title="Click to see size details"
    >
      <HardDrive className="h-4 w-4 text-gray-500 dark:text-gray-400" />
      <span className={`text-sm font-medium ${getSizeIndicatorColor()}`}>
        {formatSize(currentProjectSize + currentProjectAssetsSize)}
      </span>
    </button>
  )
}
