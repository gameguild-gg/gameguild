/**
 * EquationsPlugin — registers `INSERT_EQUATION_COMMAND` and provides
 * a small dialog (`InsertEquationDialog`) used by the ComponentPicker
 * "/Equation" item.
 *
 * Ported from `packages/lexical-playground/src/plugins/EquationsPlugin`.
 */
"use client"

import "katex/dist/katex.css"

import { useCallback, useEffect, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $wrapNodeInElement } from "@lexical/utils"
import {
  $createParagraphNode,
  $insertNodes,
  $isRootOrShadowRoot,
  COMMAND_PRIORITY_EDITOR,
  createCommand,
  type LexicalCommand,
  type LexicalEditor,
} from "lexical"
import { cn } from "@game-guild/ui/lib/utils"
import { $createEquationNode, EquationNode } from "./equation-node"
import { KatexRenderer } from "./katex-renderer"
import { MathInput } from "./math-input"

type CommandPayload = {
  equation: string
  inline: boolean
}

export const INSERT_EQUATION_COMMAND: LexicalCommand<CommandPayload> = createCommand(
  "INSERT_EQUATION_COMMAND",
)

export function EquationsPlugin() {
  const [editor] = useLexicalComposerContext()
  useEffect(() => {
    if (!editor.hasNodes([EquationNode])) {
      throw new Error("EquationsPlugin: EquationNode not registered on editor")
    }
    return editor.registerCommand<CommandPayload>(
      INSERT_EQUATION_COMMAND,
      ({ equation, inline }) => {
        const equationNode = $createEquationNode(equation, inline)
        $insertNodes([equationNode])
        if ($isRootOrShadowRoot(equationNode.getParentOrThrow())) {
          $wrapNodeInElement(equationNode, $createParagraphNode).selectEnd()
        }
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])
  return null
}

/**
 * Corpo reutilizável do diálogo de equação (criação e edição).
 * Usa MathLive (`MathInput`) como editor visual + preview KaTeX.
 */
export function EquationDialogBody({
  initialEquation = "",
  initialInline = true,
  initialFontSize = 2,
  submitLabel = "Insert",
  onClose,
  onSubmit,
}: {
  initialEquation?: string
  initialInline?: boolean
  initialFontSize?: number
  submitLabel?: string
  onClose: () => void
  onSubmit: (payload: CommandPayload) => void
}) {
  const [equation, setEquation] = useState(initialEquation)
  const [inline, setInline] = useState(initialInline)
  // Tamanho da fonte do editor (em rem). Apenas afeta a edição visual —
  // o tamanho final aplicado no documento é controlado separadamente
  // via mini-toolbar quando a equação está selecionada.
  const [editorFontSize, setEditorFontSize] = useState(initialFontSize)

  const onConfirm = useCallback(() => {
    onSubmit({ equation, inline })
    onClose()
  }, [equation, inline, onClose, onSubmit])

  return (
    <div className="flex flex-col gap-3 min-w-[640px]">
      <div className="flex items-center justify-between gap-3">
        <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
          <input
            type="checkbox"
            checked={inline}
            onChange={() => setInline((v) => !v)}
            className="h-4 w-4"
          />
          Inline
        </label>
        <label className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
          <span className="whitespace-nowrap">Editor zoom</span>
          <input
            type="range"
            min={1}
            max={4}
            step={0.1}
            value={editorFontSize}
            onChange={(e) => setEditorFontSize(parseFloat(e.target.value))}
            className="w-32 accent-blue-600"
          />
          <span className="tabular-nums w-10 text-right">
            {editorFontSize.toFixed(1)}×
          </span>
        </label>
      </div>
      <div className="flex flex-col gap-1 text-sm text-gray-700 dark:text-gray-300">
        <span>Equation</span>
        {/* MathLive virtual keyboard / visual formula editor. The value
            is stored as LaTeX, exactly what the EquationNode expects. */}
        <MathInput
          value={equation}
          onChange={setEquation}
          autoFocus
          placeholder="\\frac{a}{b}"
          className={cn("min-h-[5.5rem]")}
          style={{ fontSize: `${editorFontSize}rem` }}
        />
        <details className="text-xs text-gray-500 dark:text-gray-400">
          <summary className="cursor-pointer select-none">Raw LaTeX</summary>
          <input
            value={equation}
            onChange={(e) => setEquation(e.target.value)}
            className={cn(
              "mt-1 w-full h-7 px-2 rounded border text-xs font-mono",
              "border-gray-300 dark:border-gray-700",
              "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100",
              "focus:outline-none focus:ring-1 focus:ring-blue-500",
            )}
          />
        </details>
      </div>
      <div className="flex flex-col gap-1 text-sm text-gray-700 dark:text-gray-300">
        Preview
        <div className="min-h-[40px] p-2 rounded border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
          <KatexRenderer equation={equation} inline={false} />
        </div>
      </div>
      <div className="flex justify-end gap-2">
        <button
          type="button"
          onClick={onClose}
          className="h-8 px-3 rounded border text-sm border-gray-300 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={onConfirm}
          disabled={!equation.trim()}
          className="h-8 px-3 rounded text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:pointer-events-none"
        >
          {submitLabel}
        </button>
      </div>
    </div>
  )
}

export function InsertEquationDialog({
  activeEditor,
  onClose,
}: {
  activeEditor: LexicalEditor
  onClose: () => void
}) {
  return (
    <EquationDialogBody
      onClose={onClose}
      onSubmit={(payload) => activeEditor.dispatchCommand(INSERT_EQUATION_COMMAND, payload)}
    />
  )
}
