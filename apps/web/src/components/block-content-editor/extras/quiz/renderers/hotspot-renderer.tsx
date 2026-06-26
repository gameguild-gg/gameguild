/**
 * Hotspot Renderer
 * Displays an image for the student to click on.
 * After submission, reveals hotspot zones and shows whether the click was accurate.
 */

"use client"

import { useRef, useCallback } from "react"
import { CheckCircle, XCircle, MousePointerClick } from "lucide-react"
import type { HotspotEntry, QuizAnswerState } from "../types"

const ZONE_COLORS = [
  { bg: "rgba(34, 197, 94, 0.18)", border: "rgb(34, 197, 94)" },
  { bg: "rgba(234, 179, 8, 0.14)", border: "rgb(234, 179, 8)" },
  { bg: "rgba(249, 115, 22, 0.11)", border: "rgb(249, 115, 22)" },
  { bg: "rgba(239, 68, 68, 0.09)", border: "rgb(239, 68, 68)" },
]

interface HotspotRendererProps {
  entry: HotspotEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function HotspotRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: HotspotRendererProps) {
  const containerRef = useRef<HTMLDivElement>(null)

  const clickX = parseFloat(answerState.textAnswers["hotspot_x"] || "")
  const clickY = parseFloat(answerState.textAnswers["hotspot_y"] || "")
  const hasClicked = !isNaN(clickX) && !isNaN(clickY)

  // Determine which zone the click fell into (for feedback display)
  const getClickResult = () => {
    if (!hasClicked) return null
    for (const hp of entry.hotspots) {
      const dx = (clickX - hp.x) / 100 * entry.imageWidth
      const dy = (clickY - hp.y) / 100 * entry.imageHeight
      const distance = Math.sqrt(dx * dx + dy * dy)
      // Check zones from innermost to outermost
      const sorted = [...hp.zones].sort((a, b) => a.radius - b.radius)
      for (const zone of sorted) {
        const threshold = zone.radius / 100 * entry.imageWidth
        if (distance <= threshold) {
          return { pointId: hp.id, zone: zone.label, distance }
        }
      }
    }
    return null
  }

  const clickResult = showFeedback ? getClickResult() : null
  const revealFeedback = showFeedback && (entry.settings?.showFeedback ?? true)
  const isCorrect = revealFeedback && clickResult !== null

  const handleClick = useCallback((e: React.MouseEvent) => {
    if (disabled || showFeedback) return
    const rect = containerRef.current?.getBoundingClientRect()
    if (!rect) return
    const x = ((e.clientX - rect.left) / rect.width) * 100
    const y = ((e.clientY - rect.top) / rect.height) * 100

    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        hotspot_x: x.toFixed(2),
        hotspot_y: y.toFixed(2),
      },
    })
  }, [disabled, showFeedback, answerState.textAnswers, onAnswerChange])

  return (
    <div className="space-y-4">
      {/* Image with click overlay */}
      <div
        ref={containerRef}
        className={`relative select-none rounded-xl overflow-hidden border-2 ${
          revealFeedback
            ? isCorrect
              ? "border-green-300 dark:border-green-700"
              : "border-red-300 dark:border-red-700"
            : hasClicked
              ? "border-blue-300 dark:border-blue-700"
              : "border-gray-200 dark:border-gray-700"
        }`}
        onClick={handleClick}
        style={{ cursor: disabled || showFeedback ? "default" : "crosshair" }}
      >
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={entry.imageUrl} alt="Hotspot question" className="w-full block" draggable={false} />

        {/* Feedback: reveal hotspot zones */}
        {showFeedback && (entry.settings?.showCorrectAnswer ?? true) && entry.hotspots.map((hp) => {
          const sortedZones = [...hp.zones].sort((a, b) => b.radius - a.radius)
          return sortedZones.map((zone, zi) => {
            const colorIdx = hp.zones.indexOf(zone)
            const color = ZONE_COLORS[colorIdx % ZONE_COLORS.length] ?? ZONE_COLORS[ZONE_COLORS.length - 1]!
            return (
              <div
                key={`${hp.id}-z${zi}`}
                className="absolute rounded-full pointer-events-none"
                style={{
                  left: `${hp.x}%`,
                  top: `${hp.y}%`,
                  width: `${zone.radius * 2}%`,
                  aspectRatio: "1",
                  transform: "translate(-50%, -50%)",
                  backgroundColor: color.bg,
                  border: `2px solid ${color.border}`,
                }}
              />
            )
          })
        })}

        {/* Feedback: hotspot center markers */}
        {showFeedback && (entry.settings?.showCorrectAnswer ?? true) && entry.hotspots.map((hp) => (
          <div
            key={`center-${hp.id}`}
            className="absolute w-3 h-3 rounded-full bg-green-500 border-2 border-white shadow-md pointer-events-none z-10"
            style={{
              left: `${hp.x}%`,
              top: `${hp.y}%`,
              transform: "translate(-50%, -50%)",
            }}
          />
        ))}

        {/* Student click marker */}
        {hasClicked && (
          <div
            className={`absolute pointer-events-none z-20 flex items-center justify-center ${
              revealFeedback
                ? isCorrect
                  ? "text-green-500"
                  : "text-red-500"
                : "text-blue-500"
            }`}
            style={{
              left: `${clickX}%`,
              top: `${clickY}%`,
              transform: "translate(-50%, -50%)",
            }}
          >
            {/* Outer ring */}
            <div className={`absolute w-8 h-8 rounded-full border-2 ${
              revealFeedback
                ? isCorrect ? "border-green-500" : "border-red-500"
                : "border-blue-500"
            }`} />
            {/* Center dot */}
            <div className={`w-2.5 h-2.5 rounded-full ${
              revealFeedback
                ? isCorrect ? "bg-green-500" : "bg-red-500"
                : "bg-blue-500"
            }`} />
            {/* Crosshair lines */}
            <div className={`absolute w-px h-8 ${
              revealFeedback
                ? isCorrect ? "bg-green-500" : "bg-red-500"
                : "bg-blue-500"
            }`} />
            <div className={`absolute h-px w-8 ${
              revealFeedback
                ? isCorrect ? "bg-green-500" : "bg-red-500"
                : "bg-blue-500"
            }`} />
          </div>
        )}
      </div>

      {/* Instructions / Feedback text */}
      {!showFeedback && !hasClicked && (
        <div className="flex items-center justify-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <MousePointerClick className="h-4 w-4" />
          Click on the image to select your answer
        </div>
      )}
      {!showFeedback && hasClicked && (
        <div className="flex items-center justify-center gap-2 text-sm text-blue-600 dark:text-blue-400">
          <MousePointerClick className="h-4 w-4" />
          Click again to change your selection, then submit
        </div>
      )}
    </div>
  )
}
