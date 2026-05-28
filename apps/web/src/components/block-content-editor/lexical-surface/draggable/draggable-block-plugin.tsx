/**
 * DraggableBlockPlugin — thin wrapper over Lexical's experimental
 * `DraggableBlockPlugin_EXPERIMENTAL`. Renders only the drag handle
 * (no "+" button — our `BlockContentEditor` already provides the
 * block-insert affordance outside the Lexical surface).
 *
 * Styling follows our Tailwind theme. Handle uses `GripVertical` from
 * lucide.
 */
"use client"

import * as React from "react"
import { useRef } from "react"
import { DraggableBlockPlugin_EXPERIMENTAL } from "@lexical/react/LexicalDraggableBlockPlugin"
import { cn } from "@/lib/utils"
import { DragHandleIcon } from "../icons"

const DRAGGABLE_BLOCK_MENU_CLASSNAME = "lexical-draggable-block-menu"

function isOnMenu(element: HTMLElement): boolean {
  return !!element.closest(`.${DRAGGABLE_BLOCK_MENU_CLASSNAME}`)
}

export default function DraggableBlockPlugin({ anchorElem }: { anchorElem: HTMLElement }) {
  const menuRef = useRef<HTMLDivElement>(null)
  const targetLineRef = useRef<HTMLDivElement>(null)

  return (
    <DraggableBlockPlugin_EXPERIMENTAL
      anchorElem={anchorElem}
      menuRef={menuRef as React.RefObject<HTMLElement>}
      targetLineRef={targetLineRef as React.RefObject<HTMLElement>}
      menuComponent={
        <div
          ref={menuRef}
          className={cn(
            DRAGGABLE_BLOCK_MENU_CLASSNAME,
            "absolute left-0 top-0 flex items-center justify-center",
            "w-5 h-6 rounded cursor-grab active:cursor-grabbing",
            "text-gray-400 hover:text-gray-700 dark:hover:text-gray-200",
            "hover:bg-gray-100 dark:hover:bg-gray-800",
            "opacity-0 will-change-transform transition-opacity",
            "data-[state=visible]:opacity-100",
          )}
          // The experimental plugin toggles visibility via `style.opacity`
          // directly; we expose Tailwind classes too in case future
          // versions switch to a data-attribute API.
        >
          <DragHandleIcon className="w-4 h-4" />
        </div>
      }
      targetLineComponent={
        <div
          ref={targetLineRef}
          className={cn(
            "pointer-events-none absolute left-0 right-0 h-0.5",
            "bg-blue-500 dark:bg-blue-400",
            "opacity-0 will-change-transform",
          )}
        />
      }
      isOnMenu={isOnMenu}
    />
  )
}
