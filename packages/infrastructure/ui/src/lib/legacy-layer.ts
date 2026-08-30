import * as React from "react"

export type LegacyLayerEventHandler = (event: Event) => void

export interface LegacyLayerHandlers {
  onOpenAutoFocus?: LegacyLayerEventHandler
  onCloseAutoFocus?: LegacyLayerEventHandler
  onPointerDownOutside?: LegacyLayerEventHandler
  onFocusOutside?: LegacyLayerEventHandler
  onInteractOutside?: LegacyLayerEventHandler
  onEscapeKeyDown?: LegacyLayerEventHandler
}

export function useMergedRefs<T>(...refs: Array<React.Ref<T> | undefined>) {
  return React.useCallback((value: T | null) => {
    for (const ref of refs) {
      if (typeof ref === "function") ref(value)
      else if (ref) (ref as React.MutableRefObject<T | null>).current = value
    }
  }, refs)
}

export function useLegacyLayerHandlers(
  elementRef: React.RefObject<HTMLElement | null>,
  handlers: LegacyLayerHandlers
) {
  React.useEffect(() => {
    handlers.onOpenAutoFocus?.(new Event("openAutoFocus", { cancelable: true }))

    const isOutside = (target: EventTarget | null) =>
      target instanceof Node && !elementRef.current?.contains(target)
    const onPointerDown = (event: PointerEvent) => {
      if (!isOutside(event.target)) return
      handlers.onPointerDownOutside?.(event)
      handlers.onInteractOutside?.(event)
    }
    const onFocusIn = (event: FocusEvent) => {
      if (!isOutside(event.target)) return
      handlers.onFocusOutside?.(event)
      handlers.onInteractOutside?.(event)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") handlers.onEscapeKeyDown?.(event)
    }

    document.addEventListener("pointerdown", onPointerDown, true)
    document.addEventListener("focusin", onFocusIn, true)
    document.addEventListener("keydown", onKeyDown, true)
    return () => {
      document.removeEventListener("pointerdown", onPointerDown, true)
      document.removeEventListener("focusin", onFocusIn, true)
      document.removeEventListener("keydown", onKeyDown, true)
      handlers.onCloseAutoFocus?.(new Event("closeAutoFocus", { cancelable: true }))
    }
  }, [
    elementRef,
    handlers.onOpenAutoFocus,
    handlers.onCloseAutoFocus,
    handlers.onPointerDownOutside,
    handlers.onFocusOutside,
    handlers.onInteractOutside,
    handlers.onEscapeKeyDown,
  ])
}
