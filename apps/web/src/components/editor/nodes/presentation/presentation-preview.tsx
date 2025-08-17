"use client"
import { LayoutGrid, FileText } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { SlidePlayer } from "@/components/editor/extras/slide-player"
import type { Slide } from "./types"

interface PresentationPreviewProps {
  slides: Slide[]
  currentSlideIndex: number
  setCurrentSlideIndex: (index: number) => void
  customThemeColor?: string | null
  autoAdvance?: boolean
  autoAdvanceDelay?: number
  autoAdvanceLoop?: boolean
  transitionEffect?: "none" | "fade" | "slide" | "zoom" | "flip" | "cube"
  showControls?: boolean
}

export function PresentationPreview({
  slides,
  currentSlideIndex,
  setCurrentSlideIndex,
  customThemeColor,
  autoAdvance = false,
  autoAdvanceDelay = 5,
  autoAdvanceLoop = false,
  transitionEffect = "fade",
  showControls = true,
}: PresentationPreviewProps) {
  const getSlideInfo = (slide: Slide) => {
    const info = []
    if (slide.images && slide.images.length > 0) {
      info.push(`${slide.images.length} image${slide.images.length > 1 ? "s" : ""}`)
    }
    if (slide.shapes && slide.shapes.length > 0) {
      info.push(`${slide.shapes.length} shape${slide.shapes.length > 1 ? "s" : ""}`)
    }
    if (slide.notes && slide.notes.trim()) {
      info.push("notes")
    }
    return info
  }

  if (slides.length === 0) {
    return null
  }

  const currentSlide = slides[currentSlideIndex] || slides[0]
  const slideInfo = getSlideInfo(currentSlide)

  return (
    <div className="border-t pt-6">
      <div className="flex items-center gap-3 mb-4">
        <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
          <LayoutGrid className="h-4 w-4 text-green-600 dark:text-green-400" />
        </div>
        <div className="flex-1">
          <h3 className="text-lg font-semibold">Presentation Preview</h3>
          <p className="text-sm text-muted-foreground">Preview your imported presentation</p>
        </div>
        <div className="flex items-center gap-2">
          {slideInfo.length > 0 && (
            <div className="flex gap-1">
              {slideInfo.map((info, index) => (
                <Badge key={index} variant="secondary" className="text-xs">
                  {info}
                </Badge>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4">
        <SlidePlayer
          slides={slides}
          currentSlideIndex={currentSlideIndex}
          onSlideChange={setCurrentSlideIndex}
          autoAdvance={autoAdvance}
          autoAdvanceDelay={autoAdvanceDelay}
          autoAdvanceLoop={autoAdvanceLoop}
          transitionEffect={transitionEffect}
          showControls={showControls}
          showThumbnails={true}
          showHeader={false}
          customThemeColor={customThemeColor}
          size="lg"
          isEditing={false}
          canFullscreen={true}
          title="Presentation Preview"
        />

        {/* Current slide details */}
        {currentSlide.notes && (
          <div className="mt-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg border border-yellow-200 dark:border-yellow-800">
            <div className="flex items-center gap-2 mb-2">
              <FileText className="h-4 w-4 text-yellow-600 dark:text-yellow-400" />
              <span className="text-sm font-medium text-yellow-800 dark:text-yellow-200">Speaker Notes</span>
            </div>
            <p className="text-sm text-yellow-700 dark:text-yellow-300">{currentSlide.notes}</p>
          </div>
        )}
      </div>
    </div>
  )
}
