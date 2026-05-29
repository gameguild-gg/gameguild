/**
 * EquationComponent — decorator rendered by `EquationNode`. Shows the
 * KaTeX-rendered equation; double-click opens the full equation dialog
 * (same MathLive-based editor used to insert new equations).
 */
"use client"

import { useCallback, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalEditable } from "@lexical/react/useLexicalEditable"
import { $getNodeByKey, type NodeKey } from "lexical"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { KatexRenderer } from "./katex-renderer"
import { $createEquationNode, $isEquationNode } from "./equation-node"
import { EquationDialogBody } from "./equations-plugin"

type Props = {
  equation: string
  inline: boolean
  nodeKey: NodeKey
}

export default function EquationComponent({ equation, inline, nodeKey }: Props) {
  const [editor] = useLexicalComposerContext()
  const isEditable = useLexicalEditable()
  const [open, setOpen] = useState(false)

  const onSubmit = useCallback(
    ({ equation: nextEquation, inline: nextInline }: { equation: string; inline: boolean }) => {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (!$isEquationNode(node)) return
        if (node.__inline === nextInline) {
          node.setEquation(nextEquation)
        } else {
          // Trocar inline/block exige recriar o nó (createDOM usa o flag).
          node.replace($createEquationNode(nextEquation, nextInline))
        }
      })
    },
    [editor, nodeKey],
  )

  return (
    <>
      <KatexRenderer
        equation={equation}
        inline={inline}
        onDoubleClick={() => {
          if (isEditable) setOpen(true)
        }}
      />
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent
          onPointerDownOutside={(e) => {
            const target = e.target as HTMLElement | null
            if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
            else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
          }}
          onInteractOutside={(e) => {
            const target = e.target as HTMLElement | null
            if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
            else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
          }}
          onFocusOutside={(e) => {
            const target = e.target as HTMLElement | null
            if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
            else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
          }}
          onEscapeKeyDown={(e) => {
            if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
          }}
        >
          <DialogHeader>
            <DialogTitle>Edit Equation</DialogTitle>
          </DialogHeader>
          <EquationDialogBody
            initialEquation={equation}
            initialInline={inline}
            submitLabel="Save"
            onClose={() => setOpen(false)}
            onSubmit={onSubmit}
          />
        </DialogContent>
      </Dialog>
    </>
  )
}

