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
import { useEffect, useRef } from "react"
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

  // Marca `<body data-lexical-dragging>` durante o drag para podermos
  // aplicar `cursor: grabbing` global e desabilitar text-selection,
  // tornando o gesto mais fluido (sem highlights acidentais e sem
  // perder o cursor ao sair do bloco de origem).
  useEffect(() => {
    const menu = menuRef.current
    if (!menu) return
    const onPointerDown = () => {
      document.body.setAttribute("data-lexical-dragging", "true")
    }
    const stop = () => {
      document.body.removeAttribute("data-lexical-dragging")
    }
    menu.addEventListener("pointerdown", onPointerDown)
    document.addEventListener("pointerup", stop)
    document.addEventListener("dragend", stop)
    return () => {
      menu.removeEventListener("pointerdown", onPointerDown)
      document.removeEventListener("pointerup", stop)
      document.removeEventListener("dragend", stop)
      stop()
    }
  }, [])

  return (
    <>
      {/* Estilos globais aplicados enquanto um bloco está sendo arrastado:
          cursor consistente e sem seleção de texto acidental. */}
      <style>{`
        body[data-lexical-dragging],
        body[data-lexical-dragging] * {
          cursor: grabbing !important;
          user-select: none !important;
        }
      `}</style>
      <DraggableBlockPlugin_EXPERIMENTAL
        anchorElem={anchorElem}
        menuRef={menuRef as React.RefObject<HTMLElement>}
        targetLineRef={targetLineRef as React.RefObject<HTMLElement>}
        menuComponent={
          <div
            ref={menuRef}
            className={cn(
              DRAGGABLE_BLOCK_MENU_CLASSNAME,
              "absolute left-0 top-0 z-20 flex items-center justify-center",
              "w-6 h-7 rounded-md cursor-grab active:cursor-grabbing",
              "text-gray-500 dark:text-gray-400",
              "bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm",
              "border border-transparent hover:border-gray-200 dark:hover:border-gray-700",
              "hover:text-gray-800 dark:hover:text-gray-100",
              "hover:bg-white dark:hover:bg-gray-800 hover:shadow-sm",
              "active:scale-95 active:shadow-md",
              "opacity-0 will-change-transform transition-[opacity,box-shadow,transform] duration-150",
            )}
            title="Arraste para reordenar"
          >
            <DragHandleIcon className="w-4 h-4" />
          </div>
        }
        targetLineComponent={
          <div
            ref={targetLineRef}
            className={cn(
              "pointer-events-none absolute left-0 right-0 z-30",
              "h-[3px] rounded-full",
              "bg-blue-500 dark:bg-blue-400",
              "shadow-[0_0_0_1px_rgba(59,130,246,0.25),0_0_12px_2px_rgba(59,130,246,0.45)]",
              "opacity-0 will-change-transform transition-opacity duration-100",
              // Bolinhas nas extremidades para reforçar o ponto de inserção.
              "before:content-[''] before:absolute before:-left-1 before:top-1/2 before:-translate-y-1/2",
              "before:w-2 before:h-2 before:rounded-full before:bg-blue-500 dark:before:bg-blue-400",
              "after:content-[''] after:absolute after:-right-1 after:top-1/2 after:-translate-y-1/2",
              "after:w-2 after:h-2 after:rounded-full after:bg-blue-500 dark:after:bg-blue-400",
            )}
          />
        }
        isOnMenu={isOnMenu}
      />
    </>
  )
}
