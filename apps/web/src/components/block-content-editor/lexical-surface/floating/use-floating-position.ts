/**
 * Helpers for positioning floating UI elements relative to a Lexical
 * range or anchor element. Ported from facebook/lexical playground
 * (`utils/setFloatingElemPosition.ts` + `utils/getDOMRangeRect.ts`).
 *
 * Used by `FloatingTextFormatToolbarPlugin`. The link-editor uses
 * `@floating-ui/react` directly.
 */

const VERTICAL_GAP = 10
const HORIZONTAL_OFFSET = 5

export function getDOMRangeRect(nativeSelection: Selection, rootElement: HTMLElement): DOMRect {
  const domRange = nativeSelection.getRangeAt(0)
  let rect: DOMRect
  if (nativeSelection.anchorNode === rootElement) {
    let inner: HTMLElement = rootElement
    while (inner.firstElementChild != null) {
      inner = inner.firstElementChild as HTMLElement
    }
    rect = inner.getBoundingClientRect()
  } else {
    rect = domRange.getBoundingClientRect()
  }
  return rect
}

export function setFloatingElemPosition(
  targetRect: DOMRect | null,
  floatingElem: HTMLElement,
  anchorElem: HTMLElement,
  isLink: boolean = false,
  verticalGap: number = VERTICAL_GAP,
  horizontalOffset: number = HORIZONTAL_OFFSET,
): void {
  const scrollerElem = anchorElem.parentElement

  if (targetRect === null || !scrollerElem) {
    floatingElem.style.opacity = "0"
    floatingElem.style.transform = "translate(-10000px, -10000px)"
    return
  }

  const floatingElemRect = floatingElem.getBoundingClientRect()
  const anchorElementRect = anchorElem.getBoundingClientRect()
  const editorScrollerRect = scrollerElem.getBoundingClientRect()

  let top = targetRect.top - floatingElemRect.height - verticalGap
  let left = targetRect.left - horizontalOffset

  // Mirror upstream: re-anchor right when text is end-aligned.
  const selection = window.getSelection()
  if (selection && selection.rangeCount > 0) {
    const range = selection.getRangeAt(0)
    const textNode = range.startContainer
    if (textNode.nodeType === Node.ELEMENT_NODE || textNode.parentElement) {
      const textElement =
        textNode.nodeType === Node.ELEMENT_NODE
          ? (textNode as Element)
          : (textNode.parentElement as Element)
      const textAlign = window.getComputedStyle(textElement).textAlign
      if (textAlign === "right" || textAlign === "end") {
        left = targetRect.right - floatingElemRect.width + horizontalOffset
      }
    }
  }

  if (top < editorScrollerRect.top) {
    top += floatingElemRect.height + targetRect.height + verticalGap * (isLink ? 9 : 2)
  }
  if (left + floatingElemRect.width > editorScrollerRect.right) {
    left = editorScrollerRect.right - floatingElemRect.width - horizontalOffset
  }
  if (left < editorScrollerRect.left) {
    left = editorScrollerRect.left + horizontalOffset
  }

  top -= anchorElementRect.top
  left -= anchorElementRect.left

  floatingElem.style.opacity = "1"
  floatingElem.style.transform = `translate(${left}px, ${top}px)`
}
