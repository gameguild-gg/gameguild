"use client"

/**
 * Double-slash trigger ( "//" ) typeahead menu allowing the user to
 * insert any embeddable block type into a Lexical surface. Dispatches
 * the `INSERT_BLOCK_COMMAND` registered by `BlockEmbedPlugin`.
 *
 * The single `/` trigger is owned by the playground-faithful
 * `ComponentPickerPlugin` (paragraph, headings, lists, quote, code, hr)
 * so the two pickers no longer collide.
 */

import { useCallback, useMemo, useState } from "react"
import { createPortal } from "react-dom"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  LexicalTypeaheadMenuPlugin,
  MenuOption,
} from "@lexical/react/LexicalTypeaheadMenuPlugin"
import type { MenuTextMatch } from "@lexical/react/LexicalTypeaheadMenuPlugin"
import { ChevronRight } from "lucide-react"
import type { LucideIcon } from "lucide-react"

import { BLOCK_REGISTRY } from "../engines/blocks/block-component-registry"
import { createEmbeddableBlock } from "../embed/embeddable-blocks"
import { EMBEDDABLE_BLOCK_TYPES, type EmbeddableBlockType } from "../embed/types"
import { INSERT_BLOCK_COMMAND } from "./block-embed-plugin"
import { cn } from "@/lib/utils"

class BlockMenuOption extends MenuOption {
  readonly blockType: EmbeddableBlockType
  readonly title: string
  readonly description: string
  readonly Icon: LucideIcon

  constructor(blockType: EmbeddableBlockType) {
    super(blockType)
    const entry = BLOCK_REGISTRY[blockType]
    this.blockType = blockType
    this.title = entry.label
    this.description = entry.description
    this.Icon = entry.icon
  }
}

export function BlockInsertMenuPlugin() {
  const [editor] = useLexicalComposerContext()
  const [query, setQuery] = useState<string | null>(null)

  // Custom trigger: matches `//` (optionally followed by query chars).
  // `useBasicTypeaheadTriggerMatch` only handles a single-character trigger
  // and would fire on the first `/`, colliding with `ComponentPickerPlugin`.
  const triggerFn = useCallback((text: string): MenuTextMatch | null => {
    const match = /(?:^|\s)(\/\/)([\w-]{0,75})$/.exec(text)
    if (match === null) return null
    const matchingString = match[2] ?? ""
    return {
      leadOffset: match.index + (match[0]?.startsWith(" ") ? 1 : 0),
      matchingString,
      replaceableString: `//${matchingString}`,
    }
  }, [])

  const options = useMemo<BlockMenuOption[]>(() => {
    const all = EMBEDDABLE_BLOCK_TYPES.map((t) => new BlockMenuOption(t))
    if (!query) return all
    const needle = query.toLowerCase()
    return all.filter(
      (o) =>
        o.title.toLowerCase().includes(needle) ||
        o.description.toLowerCase().includes(needle) ||
        o.blockType.toLowerCase().includes(needle),
    )
  }, [query])

  const onSelectOption = useCallback(
    (
      selected: BlockMenuOption,
      nodeToReplace: import("lexical").TextNode | null,
      closeMenu: () => void,
    ) => {
      editor.update(() => {
        nodeToReplace?.remove()
        const block = createEmbeddableBlock(selected.blockType)
        // mark as new so the view auto-opens the editor
        block.data = { ...(block.data as unknown as Record<string, unknown>), isNew: true } as unknown as typeof block.data
        editor.dispatchCommand(INSERT_BLOCK_COMMAND, block)
      })
      closeMenu()
    },
    [editor],
  )

  return (
    <LexicalTypeaheadMenuPlugin<BlockMenuOption>
      onQueryChange={setQuery}
      onSelectOption={onSelectOption}
      triggerFn={triggerFn}
      options={options}
      anchorClassName="z-[60]"
      menuRenderFn={(anchorElementRef, { selectedIndex, selectOptionAndCleanUp, setHighlightedIndex }, matchingString) => {
        if (!anchorElementRef.current || options.length === 0) return null
        const highlighted = selectedIndex != null ? options[selectedIndex] : null
        const typed = matchingString ?? ""
        const headerTerm = highlighted?.title ?? typed
        return createPortal(
          <div
            className={cn(
              "z-50 min-w-[280px] max-h-[360px] overflow-y-auto rounded-md border-2 border-blue-500/40 bg-popover shadow-2xl",
            )}
            role="listbox"
          >
            {/* Sticky header showing currently highlighted/typed term */}
            
            <div className="p-1">
              {options.map((option, i) => {
                const Icon = option.Icon
                const isSelected = selectedIndex === i
                return (
                  <button
                    key={option.key}
                    ref={(el) => option.setRefElement(el)}
                    type="button"
                    role="option"
                    aria-selected={isSelected}
                    tabIndex={-1}
                    onMouseEnter={() => setHighlightedIndex(i)}
                    onClick={() => selectOptionAndCleanUp(option)}
                    className={cn(
                      "relative flex w-full items-start gap-2 rounded-sm px-2 py-1.5 text-left text-sm outline-none transition-colors",
                      isSelected
                        ? "bg-blue-600 text-white ring-2 ring-blue-400 ring-inset"
                        : "hover:bg-accent/60",
                    )}
                  >
                    {isSelected && (
                      <ChevronRight className="absolute left-0 top-1/2 -translate-y-1/2 size-3 text-white" aria-hidden />
                    )}
                    <Icon
                      className={cn(
                        "mt-0.5 size-4 shrink-0",
                        isSelected ? "text-white" : "text-muted-foreground",
                      )}
                      aria-hidden
                    />
                    <div className="flex min-w-0 flex-col">
                      <span className="truncate font-medium">{option.title}</span>
                      <span
                        className={cn(
                          "truncate text-xs",
                          isSelected ? "text-blue-100" : "text-muted-foreground",
                        )}
                      >
                        {option.description}
                      </span>
                    </div>
                  </button>
                )
              })}
            </div>
          </div>,
          anchorElementRef.current,
        )
      }}
    />
  )
}
