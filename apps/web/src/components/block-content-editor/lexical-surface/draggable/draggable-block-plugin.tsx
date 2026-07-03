/**
 * DraggableBlockPlugin — block reorder handle for Lexical.
 *
 * Why custom (instead of `DraggableBlockPlugin_EXPERIMENTAL` directly):
 * the upstream plugin computes draggable targets from root children only.
 * In paged mode our root children are `PageNode`s, so dragging would pick
 * whole pages. This local variant flattens each page content children and
 * reorders real content blocks.
 *
 * Styling follows our Tailwind theme. Handle uses `GripVertical` from
 * lucide.
 */
"use client"

import { useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { eventFiles } from "@lexical/rich-text"
import { calculateZoomLevel } from "@lexical/utils"
import {
  $getNearestNodeFromDOMNode,
  $getNodeByKey,
  $getRoot,
  COMMAND_PRIORITY_HIGH,
  COMMAND_PRIORITY_LOW,
  DRAGOVER_COMMAND,
  DROP_COMMAND,
  getComposedEventTarget,
  getParentElement,
  isHTMLElement,
  mergeRegister,
  type LexicalEditor,
} from "lexical"
import { createPortal } from "react-dom"
import { cn } from "@/lib/utils"
import { DragHandleIcon } from "../icons"
import { $isPageNode } from "../page/page-node"

const DRAGGABLE_BLOCK_MENU_CLASSNAME = "lexical-draggable-block-menu"
const SPACE = 4
const TARGET_LINE_HALF_HEIGHT = 2
const TEXT_BOX_HORIZONTAL_PADDING = 28
const DRAG_DATA_FORMAT = "application/x-lexical-drag-block"

const Downward = 1
const Upward = -1
const Indeterminate = 0

let prevIndex = Infinity

function getCurrentIndex(blocksLength: number): number {
  if (blocksLength === 0) {
    return Infinity
  }
  if (prevIndex >= 0 && prevIndex < blocksLength) {
    return prevIndex
  }
  return Math.floor(blocksLength / 2)
}

function getCollapsedMargins(elem: HTMLElement): {
  marginTop: number
  marginBottom: number
} {
  const getMargin = (
    element: Element | null,
    margin: "marginTop" | "marginBottom",
  ): number => (element ? parseFloat(window.getComputedStyle(element)[margin]) : 0)

  const { marginTop, marginBottom } = window.getComputedStyle(elem)
  const prevElemSiblingMarginBottom = getMargin(elem.previousElementSibling, "marginBottom")
  const nextElemSiblingMarginTop = getMargin(elem.nextElementSibling, "marginTop")

  return {
    marginTop: Math.max(parseFloat(marginTop), prevElemSiblingMarginBottom),
    marginBottom: Math.max(parseFloat(marginBottom), nextElemSiblingMarginTop),
  }
}

function getDraggableBlockElements(editor: LexicalEditor): HTMLElement[] {
  return editor.read("latest", () => {
    const result: HTMLElement[] = []
    for (const child of $getRoot().getChildren()) {
      if ($isPageNode(child)) {
        const pageContent = child.getContentNode()
        for (const pageChild of pageContent.getChildren()) {
          const elem = editor.getElementByKey(pageChild.getKey())
          if (elem) {
            result.push(elem)
          }
        }
        continue
      }

      const elem = editor.getElementByKey(child.getKey())
      if (elem) {
        result.push(elem)
      }
    }
    return result
  })
}

function getBlockElement(
  anchorElem: HTMLElement,
  editor: LexicalEditor,
  event: MouseEvent,
  useEdgeAsDefault = false,
): HTMLElement | null {
  const anchorRect = anchorElem.getBoundingClientRect()
  const blockElements = getDraggableBlockElements(editor)
  if (blockElements.length === 0) {
    return null
  }

  if (useEdgeAsDefault) {
    const firstNode = blockElements[0]
    const lastNode = blockElements[blockElements.length - 1]
    const firstNodeRect = firstNode?.getBoundingClientRect()
    const lastNodeRect = lastNode?.getBoundingClientRect()

    if (firstNode && lastNode && firstNodeRect && lastNodeRect) {
      const firstZoom = calculateZoomLevel(firstNode)
      const lastZoom = calculateZoomLevel(lastNode)
      if (event.clientY / firstZoom < firstNodeRect.top) {
        return firstNode
      }
      if (event.clientY / lastZoom > lastNodeRect.bottom) {
        return lastNode
      }
    }
  }

  let index = getCurrentIndex(blockElements.length)
  let direction = Indeterminate

  while (index >= 0 && index < blockElements.length) {
    const elem = blockElements[index]
    const zoom = calculateZoomLevel(elem)
    const pointX = event.clientX / zoom
    const pointY = event.clientY / zoom

    const domRect = elem.getBoundingClientRect()
    const { marginTop, marginBottom } = getCollapsedMargins(elem)

    const top = domRect.top - marginTop
    const bottom = domRect.bottom + marginBottom
    const left = anchorRect.left
    const right = anchorRect.right

    const isOnTopSide = pointY < top
    const isOnBottomSide = pointY > bottom
    const isOnLeftSide = pointX < left
    const isOnRightSide = pointX > right

    const containsPoint = !isOnTopSide && !isOnBottomSide && !isOnLeftSide && !isOnRightSide
    if (containsPoint) {
      prevIndex = index
      return elem
    }

    if (direction === Indeterminate) {
      if (isOnTopSide) {
        direction = Upward
      } else if (isOnBottomSide) {
        direction = Downward
      } else {
        break
      }
    }

    index += direction
  }

  return null
}

function setMenuPosition(
  targetElem: HTMLElement | null,
  floatingElem: HTMLElement,
  anchorElem: HTMLElement,
  zoomLevel: number,
): void {
  if (!targetElem) {
    floatingElem.style.display = "none"
    return
  }

  const targetRect = targetElem.getBoundingClientRect()
  const targetStyle = window.getComputedStyle(targetElem)
  const floatingRect = floatingElem.getBoundingClientRect()
  const anchorRect = anchorElem.getBoundingClientRect()

  let targetLineHeight = Number.parseInt(targetStyle.lineHeight, 10)
  if (Number.isNaN(targetLineHeight)) {
    targetLineHeight = targetRect.bottom - targetRect.top
  }

  const top =
    (targetRect.top +
      (targetLineHeight - (floatingRect.height || targetLineHeight)) / 2 -
      anchorRect.top +
      anchorElem.scrollTop) /
    zoomLevel

  floatingElem.style.display = "flex"
  floatingElem.style.opacity = "1"
  floatingElem.style.transform = `translate(${SPACE}px, ${top}px)`
}

function setDragImage(dataTransfer: DataTransfer, draggableBlockElem: HTMLElement): void {
  const { transform } = draggableBlockElem.style
  draggableBlockElem.style.transform = "translateZ(0)"
  dataTransfer.setDragImage(draggableBlockElem, 0, 0)
  setTimeout(() => {
    draggableBlockElem.style.transform = transform
  })
}

function setTargetLine(
  targetLineElem: HTMLElement,
  targetBlockElem: HTMLElement,
  mouseY: number,
  anchorElem: HTMLElement,
): void {
  const { top: targetTop, height: targetHeight } = targetBlockElem.getBoundingClientRect()
  const { top: anchorTop, width: anchorWidth } = anchorElem.getBoundingClientRect()
  const { marginTop, marginBottom } = getCollapsedMargins(targetBlockElem)

  let lineTop = targetTop
  if (mouseY >= targetTop) {
    lineTop += targetHeight + marginBottom / 2
  } else {
    lineTop -= marginTop / 2
  }

  const top = lineTop - anchorTop - TARGET_LINE_HALF_HEIGHT + anchorElem.scrollTop
  const left = TEXT_BOX_HORIZONTAL_PADDING - SPACE

  targetLineElem.style.transform = `translate(${left}px, ${top}px)`
  targetLineElem.style.width = `${anchorWidth - (TEXT_BOX_HORIZONTAL_PADDING - SPACE) * 2}px`
  targetLineElem.style.opacity = "0.4"
}

function hideTargetLine(targetLineElem: HTMLElement | null): void {
  if (!targetLineElem) {
    return
  }
  targetLineElem.style.opacity = "0"
  targetLineElem.style.transform = "translate(-10000px, -10000px)"
}

function isOnMenu(element: HTMLElement): boolean {
  return !!element.closest(`.${DRAGGABLE_BLOCK_MENU_CLASSNAME}`)
}

export default function DraggableBlockPlugin({ anchorElem }: { anchorElem: HTMLElement }) {
  const [editor] = useLexicalComposerContext()
  const menuRef = useRef<HTMLDivElement>(null)
  const targetLineRef = useRef<HTMLDivElement>(null)
  const isDraggingBlockRef = useRef(false)
  const [draggableBlockElem, setDraggableBlockElem] = useState<HTMLElement | null>(null)
  const scrollerElem = getParentElement(anchorElem)

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

  useEffect(() => {
    const onMouseMove = (event: MouseEvent) => {
      const target = getComposedEventTarget(event)
      if (!isHTMLElement(target)) {
        setDraggableBlockElem(null)
        return
      }
      if (isOnMenu(target)) {
        return
      }
      setDraggableBlockElem(getBlockElement(anchorElem, editor, event))
    }

    const onMouseLeave = () => {
      setDraggableBlockElem(null)
    }

    if (scrollerElem) {
      scrollerElem.addEventListener("mousemove", onMouseMove)
      scrollerElem.addEventListener("mouseleave", onMouseLeave)
    }

    return () => {
      if (scrollerElem) {
        scrollerElem.removeEventListener("mousemove", onMouseMove)
        scrollerElem.removeEventListener("mouseleave", onMouseLeave)
      }
    }
  }, [anchorElem, editor, scrollerElem])

  useEffect(() => {
    const menu = menuRef.current
    if (!menu) {
      return
    }
    const zoomLevel = calculateZoomLevel(editor.getRootElement(), true)
    setMenuPosition(draggableBlockElem, menu, anchorElem, zoomLevel)
  }, [anchorElem, draggableBlockElem, editor])

  useEffect(() => {
    const onDragOver = (event: DragEvent): boolean => {
      if (!isDraggingBlockRef.current) {
        return false
      }
      const [isFileTransfer] = eventFiles(event)
      if (isFileTransfer) {
        return false
      }
      const target = getComposedEventTarget(event)
      if (!isHTMLElement(target)) {
        return false
      }

      const targetBlockElem = getBlockElement(anchorElem, editor, event, true)
      const targetLineElem = targetLineRef.current
      if (!targetBlockElem || !targetLineElem) {
        return false
      }

      setTargetLine(
        targetLineElem,
        targetBlockElem,
        event.pageY / calculateZoomLevel(target),
        anchorElem,
      )
      event.preventDefault()
      return true
    }

    const onDrop = (event: DragEvent): boolean => {
      if (!isDraggingBlockRef.current) {
        return false
      }
      const [isFileTransfer] = eventFiles(event)
      if (isFileTransfer) {
        return false
      }

      const { dataTransfer, pageY } = event
      const target = getComposedEventTarget(event)
      if (!isHTMLElement(target)) {
        return false
      }

      const dragData = dataTransfer?.getData(DRAG_DATA_FORMAT) ?? ""
      const draggedNode = $getNodeByKey(dragData)
      if (!draggedNode) {
        return false
      }

      const targetBlockElem = getBlockElement(anchorElem, editor, event, true)
      if (!targetBlockElem) {
        return false
      }

      const targetNode = $getNearestNodeFromDOMNode(targetBlockElem)
      if (!targetNode || targetNode === draggedNode) {
        return true
      }

      const targetBlockTop = targetBlockElem.getBoundingClientRect().top
      if (pageY / calculateZoomLevel(target) >= targetBlockTop) {
        targetNode.insertAfter(draggedNode)
      } else {
        targetNode.insertBefore(draggedNode)
      }

      setDraggableBlockElem(null)
      return true
    }

    return mergeRegister(
      editor.registerCommand(
        DRAGOVER_COMMAND,
        (event) => {
          return onDragOver(event)
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerCommand(
        DROP_COMMAND,
        (event) => {
          return onDrop(event)
        },
        COMMAND_PRIORITY_HIGH,
      ),
    )
  }, [anchorElem, editor])

  const onDragStart = (event: React.DragEvent<HTMLDivElement>): void => {
    const dataTransfer = event.dataTransfer
    if (!dataTransfer || !draggableBlockElem) {
      return
    }

    setDragImage(dataTransfer, draggableBlockElem)
    let nodeKey = ""
    editor.update(() => {
      const node = $getNearestNodeFromDOMNode(draggableBlockElem)
      if (node) {
        nodeKey = node.getKey()
      }
    })

    isDraggingBlockRef.current = true
    dataTransfer.setData(DRAG_DATA_FORMAT, nodeKey)
  }

  const onDragEnd = (): void => {
    isDraggingBlockRef.current = false
    hideTargetLine(targetLineRef.current)
  }

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
      {createPortal(
        <>
          <div draggable={true} onDragStart={onDragStart} onDragEnd={onDragEnd}>
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
          </div>
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
        </>,
        anchorElem,
      )}
    </>
  )
}
