"use client"

import { Button } from "@/components/ui/button"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { cn } from "@/lib/utils"

interface SlideNavigationControlsProps {
  currentIndex: number
  totalSlides: number
  onPrevious: () => void
  onNext: () => void
  className?: string
}

export function SlideNavigationControls({
  currentIndex,
  totalSlides,
  onPrevious,
  onNext,
  className,
}: SlideNavigationControlsProps) {
  const hasPrevious = currentIndex > 0
  const hasNext = currentIndex < totalSlides - 1

  return (
    <div className={cn("flex items-center gap-2", className)}>
      {/* Previous Button */}
      <Button
        onClick={onPrevious}
        disabled={!hasPrevious}
        variant="outline"
        size="sm"
        className="gap-1"
        title="Previous Slide (Ctrl + ←)"
      >
        <ChevronLeft className="h-4 w-4" />
        <span className="hidden sm:inline">Previous</span>
      </Button>

      {/* Slide Indicator */}
      <div className="px-3 py-1.5 bg-gray-100 dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700">
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {currentIndex + 1} / {totalSlides}
        </span>
      </div>

      {/* Next Button */}
      <Button
        onClick={onNext}
        disabled={!hasNext}
        variant="outline"
        size="sm"
        className="gap-1"
        title="Next Slide (Ctrl + →)"
      >
        <span className="hidden sm:inline">Next</span>
        <ChevronRight className="h-4 w-4" />
      </Button>
    </div>
  )
}
