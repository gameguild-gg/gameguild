/**
 * Highlight Editor
 * Write text using __marked__ syntax to define correct highlight spans.
 * Shows a live preview with highlights rendered below the textarea.
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { HighlightEntry } from "../types"
import { parseHighlightSource } from "../types"

export function HighlightEditor() {
  const { watch, setValue } = useFormContext<HighlightEntry>()
  const sourceText = watch("sourceText") || ""
  const { plainText, highlights } = parseHighlightSource(sourceText)

  const handleSourceChange = (value: string) => {
    const parsed = parseHighlightSource(value)
    setValue("sourceText", value)
    setValue("plainText", parsed.plainText)
    setValue("highlights", parsed.highlights)
  }

  // Render preview with highlighted spans
  const renderPreview = () => {
    if (!plainText) return null
    const parts: React.ReactNode[] = []
    let cursor = 0

    // Sort highlights by start position
    const sorted = [...highlights].sort((a, b) => a.start - b.start)

    for (const span of sorted) {
      if (span.start > cursor) {
        parts.push(
          <span key={`t-${cursor}`}>{plainText.substring(cursor, span.start)}</span>
        )
      }
      parts.push(
        <mark
          key={`h-${span.start}`}
          className="bg-yellow-200 dark:bg-yellow-700/50 text-yellow-900 dark:text-yellow-100 px-0.5 rounded-sm"
        >
          {plainText.substring(span.start, span.end)}
        </mark>
      )
      cursor = span.end
    }
    if (cursor < plainText.length) {
      parts.push(
        <span key={`t-${cursor}`}>{plainText.substring(cursor)}</span>
      )
    }
    return parts
  }

  return (
    <div className="space-y-4">
      {/* Source text input */}
      <div className="space-y-2">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Text with Highlights
        </Label>
        <div className="text-xs text-gray-500 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
          Wrap the words or phrases the student must highlight with double underscores.
          <br />
          Example: <code className="text-blue-700 dark:text-blue-300">The __mitochondria__ is the powerhouse of the __cell__.</code>
        </div>
        <Textarea
          value={sourceText}
          onChange={(e) => handleSourceChange(e.target.value)}
          placeholder="The __mitochondria__ is the powerhouse of the __cell__."
          className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 min-h-[100px] font-mono text-sm"
          autoComplete="off"
        />
      </div>

      {/* Preview */}
      {plainText && (
        <div className="space-y-2">
          <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Preview ({highlights.length} highlight{highlights.length !== 1 ? "s" : ""})
          </Label>
          <div className="p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 text-base leading-relaxed">
            {renderPreview()}
          </div>
        </div>
      )}
    </div>
  )
}
