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
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { cn } from "@/lib/utils"
import { ChevronDownIcon } from "../icons"

type DropDownContextValue = {
  close: () => void
}
const DropDownContext = React.createContext<DropDownContextValue | null>(null)

export function DropDown({
  buttonLabel,
  buttonIcon,
  buttonClassName,
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
  buttonAriaLabel?: string
  disabled?: boolean
  showChevron?: boolean
  align?: "start" | "center" | "end"
  children: React.ReactNode
  title?: string
}) {
  const [open, setOpen] = React.useState(false)
  const close = React.useCallback(() => setOpen(false), [])

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
          {buttonLabel != null && <span className="truncate max-w-[160px]">{buttonLabel}</span>}
          {showChevron && <ChevronDownIcon className="w-3 h-3 opacity-60" />}
        </button>
      </PopoverTrigger>
      <PopoverContent
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
}: {
  children: React.ReactNode
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void
  active?: boolean
  className?: string
  shortcut?: string
  title?: string
  ariaLabel?: string
}) {
  const ctx = React.useContext(DropDownContext)
  return (
    <button
      type="button"
      title={title}
      aria-label={ariaLabel}
      onClick={(e) => {
        onClick?.(e)
        ctx?.close()
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
