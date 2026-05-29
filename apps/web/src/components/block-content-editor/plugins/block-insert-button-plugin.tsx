"use client"

/**
 * Renders the "+ Block" toolbar entry. Instead of opening a modal
 * picker, lists the embeddable block types directly inside the
 * toolbar's `DropDown` (same pattern as the Insert / Format menus).
 */

import { useCallback } from "react"
import { Plus } from "lucide-react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"

import { DropDown, DropDownItem } from "../lexical-surface/toolbar/dropdown"
import { BLOCK_REGISTRY } from "../engines/blocks/block-component-registry"
import type { Block } from "../lib/storage/editor/block-structure"
import { EMBEDDABLE_BLOCK_TYPES, isEmbeddableBlockType, type EmbeddableBlock } from "../embed/types"
import { INSERT_BLOCK_COMMAND } from "./block-embed-plugin"

interface BlockInsertButtonPluginProps {
  disabled?: boolean
}

export function BlockInsertButtonPlugin({ disabled }: BlockInsertButtonPluginProps = {}) {
  const [editor] = useLexicalComposerContext()

  const insert = useCallback(
    (block: Block) => {
      if (!isEmbeddableBlockType(block.type)) return
      const embeddable = {
        ...block,
        data: { ...(block.data as Record<string, unknown>), isNew: true },
      } as EmbeddableBlock
      editor.dispatchCommand(INSERT_BLOCK_COMMAND, embeddable)
    },
    [editor],
  )

  return (
    <DropDown
      disabled={disabled}
      buttonLabel="Block"
      buttonIcon={<Plus className="w-4 h-4" />}
      buttonAriaLabel="Insert block"
      title="Insert block"
    >
      {EMBEDDABLE_BLOCK_TYPES.map((type) => {
        const config = BLOCK_REGISTRY[type]
        if (!config) return null
        const Icon = config.icon
        return (
          <DropDownItem
            key={type}
            onClick={() => insert(config.createEmpty())}
            title={config.description}
          >
            <Icon className="w-4 h-4 opacity-80" />
            <span>{config.label}</span>
          </DropDownItem>
        )
      })}
    </DropDown>
  )
}

