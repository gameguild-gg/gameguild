/**
 * EquationComponent — decorator rendered by `EquationNode`. Shows the
 * KaTeX-rendered equation; double-click switches to the inline editor.
 * Ported from `packages/lexical-playground/src/nodes/EquationComponent.tsx`.
 */
"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalEditable } from "@lexical/react/useLexicalEditable"
import { mergeRegister } from "@lexical/utils"
import {
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  COMMAND_PRIORITY_HIGH,
  KEY_ESCAPE_COMMAND,
  type NodeKey,
  SELECTION_CHANGE_COMMAND,
} from "lexical"
import { EquationEditor } from "./equation-editor"
import { KatexRenderer } from "./katex-renderer"
import { $isEquationNode } from "./equation-node"

type Props = {
  equation: string
  inline: boolean
  nodeKey: NodeKey
}

export default function EquationComponent({ equation, inline, nodeKey }: Props) {
  const [editor] = useLexicalComposerContext()
  const isEditable = useLexicalEditable()
  const [equationValue, setEquationValue] = useState(equation)
  const [showEditor, setShowEditor] = useState(false)
  const inputRef = useRef<HTMLTextAreaElement | HTMLInputElement>(null)

  const onHide = useCallback(
    (restoreSelection?: boolean) => {
      setShowEditor(false)
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if ($isEquationNode(node)) {
          node.setEquation(equationValue)
          if (restoreSelection) {
            node.selectNext(0, 0)
          }
        }
      })
    },
    [editor, equationValue, nodeKey],
  )

  useEffect(() => {
    if (!showEditor && equationValue !== equation) {
      setEquationValue(equation)
    }
  }, [showEditor, equation, equationValue])

  useEffect(() => {
    if (!isEditable) return
    if (showEditor) {
      return mergeRegister(
        editor.registerCommand(
          SELECTION_CHANGE_COMMAND,
          () => {
            if (inputRef.current !== document.activeElement) {
              onHide()
            }
            return false
          },
          COMMAND_PRIORITY_HIGH,
        ),
        editor.registerCommand(
          KEY_ESCAPE_COMMAND,
          () => {
            if (inputRef.current === document.activeElement) {
              onHide(true)
              return true
            }
            return false
          },
          COMMAND_PRIORITY_HIGH,
        ),
      )
    }
    return editor.registerUpdateListener(({ editorState }) => {
      const isSelected = editorState.read(() => {
        const sel = $getSelection()
        return (
          $isNodeSelection(sel) &&
          sel.has(nodeKey) &&
          sel.getNodes().length === 1
        )
      })
      if (isSelected) setShowEditor(true)
    })
  }, [editor, nodeKey, onHide, showEditor, isEditable])

  if (showEditor && isEditable) {
    return (
      <EquationEditor
        equation={equationValue}
        setEquation={setEquationValue}
        inline={inline}
        ref={inputRef}
      />
    )
  }
  return (
    <KatexRenderer
      equation={equationValue}
      inline={inline}
      onDoubleClick={() => {
        if (isEditable) setShowEditor(true)
      }}
    />
  )
}
