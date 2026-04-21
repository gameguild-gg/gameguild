/**
 * Quiz Node
 * Lexical decorator node for quiz questions
 */

"use client"

import { useState, useEffect } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { Pencil } from "lucide-react"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { JSX } from "react/jsx-runtime"

import { Button } from "@/components/ui/button"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import {
  QuizDisplay,
  QuizWrapper,
  type QuizEntry,
  QuizEntryType,
  createSingleChoiceEntry,
} from "@/components/editor/extras/quiz"
import { QuizSettingsDialog } from "@/components/editor/lazy-client-components"

// ============================================================================
// Serialization Types
// ============================================================================

export interface SerializedQuizNode extends SerializedLexicalNode {
  type: "quiz"
  entry: QuizEntry
  version: 1
}

// ============================================================================
// Lexical Node
// ============================================================================

export class QuizNode extends DecoratorNode<JSX.Element> {
  __entry: QuizEntry

  static getType(): string {
    return "quiz"
  }

  static clone(node: QuizNode): QuizNode {
    return new QuizNode(node.__entry, node.__key)
  }

  constructor(entry: QuizEntry, key?: string) {
    super(key)
    this.__entry = entry
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setEntry(entry: QuizEntry): void {
    const writable = this.getWritable()
    writable.__entry = entry
  }

  getEntry(): QuizEntry {
    return this.__entry
  }

  exportJSON(): SerializedQuizNode {
    return {
      type: "quiz",
      entry: this.__entry,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedQuizNode): QuizNode {
    return new QuizNode(serializedNode.entry)
  }

  decorate(): JSX.Element {
    return <QuizComponent entry={this.__entry} nodeKey={this.__key} />
  }
}

// ============================================================================
// React Component
// ============================================================================

interface QuizComponentProps {
  entry: QuizEntry
  nodeKey: string
}

function QuizComponent({ entry, nodeKey }: QuizComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isEditing, setIsEditing] = useState(false)
  const [hasAutoOpened, setHasAutoOpened] = useState(false)

  // Auto-open editor for new quiz
  useEffect(() => {
    const isNewQuiz = !entry.stem || entry.stem.trim() === ""
    if (isNewQuiz && !hasAutoOpened) {
      setIsEditing(true)
      setHasAutoOpened(true)
    }
  }, [entry.stem, hasAutoOpened])

  const handleSave = (newEntry: QuizEntry) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof QuizNode) {
        node.setEntry(newEntry)
      }
    })
  }

  // Empty state
  if (!entry.stem) {
    return (
      <div className="my-6 p-8 border-2 border-dashed border-gray-300 rounded-xl text-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center">
            <Pencil className="w-6 h-6 text-blue-600" />
          </div>
          <div>
            <h3 className="font-medium text-gray-900">Configure Quiz</h3>
            <p className="text-sm text-gray-500 mt-1">Click to set up your quiz question and answers</p>
          </div>
          <Button onClick={() => setIsEditing(true)}>Configure Quiz</Button>
        </div>

        <QuizSettingsDialog
          isOpen={isEditing}
          onClose={() => setIsEditing(false)}
          entry={entry}
          onSave={handleSave}
        />
      </div>
    )
  }

  // Configured state
  return (
    <>
      <div className="my-8 relative group">
        <div className="relative">
          <QuizWrapper>
            <QuizDisplay entry={entry} />
          </QuizWrapper>

          <ContentEditMenu
            options={[
              {
                id: "edit",
                icon: <Pencil className="h-4 w-4" />,
                label: "Edit Quiz",
                action: () => setIsEditing(true),
              },
            ]}
            className="opacity-100"
          />
        </div>
      </div>

      <QuizSettingsDialog
        isOpen={isEditing}
        onClose={() => setIsEditing(false)}
        entry={entry}
        onSave={handleSave}
      />
    </>
  )
}

// ============================================================================
// Factory Function
// ============================================================================

export function $createQuizNode(): QuizNode {
  return new QuizNode(createSingleChoiceEntry())
}
