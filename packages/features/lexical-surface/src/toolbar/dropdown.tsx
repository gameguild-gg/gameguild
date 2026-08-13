/**
 * Lightweight dropdown primitive used by the toolbar. Wraps the
 * shadcn `Popover` so dropdown content renders in a portal (avoids
 * z-index fights with floating bubbles and modals).
 *
 * Mirrors the API of the playground `DropDown` (button label + items)
 * without depending on its CSS.
 */
"use client"

import * as React from "react"
import { Popover, PopoverContent, PopoverTrigger } from "@game-guild/ui/components/popover"
import { cn } from "@game-guild/ui/lib/utils"
import { ChevronDownIcon } from "../icons"

type DropDownContextValue = {
  close: () => void
}
const DropDownContext = React.createContext<DropDownContextValue | null>(null)

export function DropDown({
  buttonLabel,
  buttonIcon,
  buttonClassName,
  buttonLabelStyle,
  buttonAriaLabel,
  disabled,
  showChevron = true,
  align = "start",
  children,
  title,
}: {
  buttonLabel?: React.ReactNode
  buttonIcon?: React.ReactNode
  buttonClassName?: string
  buttonLabelStyle?: React.CSSProperties
  buttonAriaLabel?: string
  disabled?: boolean
  showChevron?: boolean
  align?: "start" | "center" | "end"
  children: React.ReactNode
  title?: string
}) {
  const [open, setOpen] = React.useState(false)
  const close = React.useCallback(() => setOpen(false), [])
  const contentRef = React.useRef<HTMLDivElement | null>(null)

  // On open, scrolls to the active item (if any) — avoids having to scroll
  // to find the current option in long lists (e.g., fonts).
  React.useEffect(() => {
    if (!open) return
    const id = requestAnimationFrame(() => {
      const root = contentRef.current
      if (!root) return
      const active = root.querySelector<HTMLElement>('[data-dropdown-active="true"]')
      if (active) {
        active.scrollIntoView({ block: "nearest", inline: "nearest" })
      }
    })
    return () => cancelAnimationFrame(id)
  }, [open])

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          title={title}
          aria-label={buttonAriaLabel}
          className={cn(
            "inline-flex items-center gap-1 h-8 px-2 rounded text-sm",
            "hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none",
            buttonClassName,
          )}
        >
          {buttonIcon}
          {buttonLabel != null && (
            <span className="truncate max-w-[160px]" style={buttonLabelStyle}>
              {buttonLabel}
            </span>
          )}
          {showChevron && <ChevronDownIcon className="w-3 h-3 opacity-60" />}
        </button>
      </PopoverTrigger>
      <PopoverContent
        ref={contentRef}
        align={align}
        sideOffset={4}
        className="p-1 w-auto min-w-[180px] max-h-[60vh] overflow-y-auto"
      >
        <DropDownContext.Provider value={{ close }}>{children}</DropDownContext.Provider>
      </PopoverContent>
    </Popover>
  )
}

export function DropDownItem({
  children,
  onClick,
  active,
  className,
  shortcut,
  title,
  ariaLabel,
  closeOnClick = true,
}: {
  children: React.ReactNode
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void
  active?: boolean
  className?: string
  shortcut?: string
  title?: string
  ariaLabel?: string
  /** If `false`, the dropdown remains open after the click (useful for
   *  lists where the user wants to preview multiple options, e.g., fonts). */
  closeOnClick?: boolean
}) {
  const ctx = React.useContext(DropDownContext)
  return (
    <button
      type="button"
      title={title}
      aria-label={ariaLabel}
      data-dropdown-active={active ? "true" : undefined}
      onClick={(e) => {
        onClick?.(e)
        if (closeOnClick) ctx?.close()
      }}
      className={cn(
        "w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded",
        "hover:bg-gray-100 dark:hover:bg-gray-800",
        active && "bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300",
        className,
      )}
    >
      <span className="flex-1 flex items-center gap-2 min-w-0 text-left">{children}</span>
      {shortcut && (
        <span className="text-xs text-gray-500 dark:text-gray-400 tabular-nums">{shortcut}</span>
      )}
    </button>
  )
}

export function DropDownDivider() {
  return <div className="h-px my-1 bg-gray-200 dark:bg-gray-700" />
}
