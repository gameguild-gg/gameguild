/**
 * True/False Editor
 * Edit mode for creating/editing true/false questions
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@game-guild/ui/components/label"
import type { TrueFalseEntry } from "@game-guild/quiz"

export function TrueFalseEditor() {
  const { watch, setValue } = useFormContext<TrueFalseEntry>()
  const correctAnswer = watch("correctAnswer")

  return (
    <div className="space-y-4">
      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
        Correct Answer
      </Label>

      <div className="flex gap-4">
        <button
          type="button"
          onClick={() => setValue("correctAnswer", true)}
          className={`
            flex-1 p-4 rounded-lg border-2 font-medium text-center transition-all
            ${correctAnswer === true
              ? "border-green-500 bg-green-50 text-green-700"
              : "border-gray-200 hover:border-gray-300 hover:bg-gray-50"
            }
          `}
        >
          <svg
            className={`w-5 h-5 inline mr-2 ${correctAnswer === true ? "text-green-500" : "text-gray-400"}`}
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fillRule="evenodd"
              d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
              clipRule="evenodd"
            />
          </svg>
          True
        </button>

        <button
          type="button"
          onClick={() => setValue("correctAnswer", false)}
          className={`
            flex-1 p-4 rounded-lg border-2 font-medium text-center transition-all
            ${correctAnswer === false
              ? "border-red-500 bg-red-50 text-red-700"
              : "border-gray-200 hover:border-gray-300 hover:bg-gray-50"
            }
          `}
        >
          <svg
            className={`w-5 h-5 inline mr-2 ${correctAnswer === false ? "text-red-500" : "text-gray-400"}`}
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fillRule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clipRule="evenodd"
            />
          </svg>
          False
        </button>
      </div>
    </div>
  )
}
