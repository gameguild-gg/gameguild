/**
 * EquationEditor — Tailwind port of the playground inline/block
 * KaTeX equation editor.
 */
"use client"

import * as React from "react"
import { isHTMLElement } from "lexical"
import { cn } from "@/lib/utils"

type Props = {
  equation: string
  inline: boolean
  setEquation: (equation: string) => void
}

export const EquationEditor = React.forwardRef<
  HTMLInputElement | HTMLTextAreaElement,
  Props
>(function EquationEditor({ equation, setEquation, inline }, forwardedRef) {
  const onChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setEquation(event.target.value)
  }

  if (inline && isHTMLElement(forwardedRef)) {
    return (
      <span className="inline-flex items-center gap-0.5 px-1 py-0.5 bg-gray-100 dark:bg-gray-800 rounded">
        <span className="text-gray-500">$</span>
        <input
          className={cn(
            "min-w-[40px] bg-transparent outline-none text-sm font-mono",
            "text-gray-900 dark:text-gray-100",
          )}
          value={equation}
          onChange={onChange}
          autoFocus
          ref={forwardedRef as React.RefObject<HTMLInputElement>}
        />
        <span className="text-gray-500">$</span>
      </span>
    )
  }
  return (
    <div className="inline-flex flex-col gap-1 p-2 bg-gray-100 dark:bg-gray-800 rounded">
      <span className="text-gray-500 text-xs font-mono">{"$$"}</span>
      <textarea
        className={cn(
          "min-w-[200px] min-h-[60px] bg-transparent outline-none text-sm font-mono resize",
          "text-gray-900 dark:text-gray-100",
        )}
        value={equation}
        onChange={onChange}
        autoFocus
        ref={forwardedRef as React.RefObject<HTMLTextAreaElement>}
      />
      <span className="text-gray-500 text-xs font-mono">{"$$"}</span>
    </div>
  )
})
