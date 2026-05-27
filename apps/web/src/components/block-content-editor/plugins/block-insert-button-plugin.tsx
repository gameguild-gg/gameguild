"use client"

/**
 * Renders a small "+" button (typically displayed by the host below or
 * beside the editor area) that opens the `BlockTypePicker` filtered to
 * embeddable block types. The chosen block is dispatched as
 * `INSERT_BLOCK_COMMAND`.
 */

import { useCallback, useState } from "react"
import { Plus } from "lucide-react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"

import { Button } from "@/components/ui/button"
import { BlockTypePicker } from "../engines/blocks/block-type-picker"
import type { Block } from "../lib/storage/editor/block-structure"
import { EMBEDDABLE_BLOCK_TYPES, isEmbeddableBlockType, type EmbeddableBlock } from "../embed/types"
import { INSERT_BLOCK_COMMAND } from "./block-embed-plugin"
import { cn } from "@/lib/utils"

interface BlockInsertButtonPluginProps {
  className?: string
}

export function BlockInsertButtonPlugin({ className }: BlockInsertButtonPluginProps) {
  const [editor] = useLexicalComposerContext()
  const [open, setOpen] = useState(false)

  const handleSelect = useCallback(
    (block: Block) => {
      if (!isEmbeddableBlockType(block.type)) return
      const embeddable = {
        ...block,
        data: { ...(block.data as Record<string, unknown>), isNew: true },
      } as EmbeddableBlock
      editor.dispatchCommand(INSERT_BLOCK_COMMAND, embeddable)
      setOpen(false)
    },
    [editor],
  )

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        className={cn("gap-1.5 text-muted-foreground hover:text-foreground", className)}
        onClick={() => setOpen(true)}
        aria-label="Insert block"
      >
        <Plus className="size-4" aria-hidden />
        <span className="text-xs">Block</span>
      </Button>
      <BlockTypePicker
        open={open}
        onOpenChange={setOpen}
        onSelect={handleSelect}
        allowedBlockTypes={[...EMBEDDABLE_BLOCK_TYPES]}
      />
    </>
  )
}
