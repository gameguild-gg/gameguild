/**
 * Color picker dropdown — trigger button (matches playground style) with
 * a popover that hosts the fully-ported `ColorPicker` (HEX, swatches,
 * HSV picker, hue slider).
 */
"use client";

import * as React from "react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@game-guild/ui/components/popover";
import { cn } from "@game-guild/ui/lib/utils";
import { ChevronDownIcon } from "../../icons";
import ColorPicker from "./color-picker";

export function DropdownColorPicker({
  color,
  onChange,
  buttonAriaLabel,
  buttonIcon,
  title,
  disabled,
  preserveSelection = false,
}: {
  color: string;
  onChange: (
    next: string,
    skipHistoryStack: boolean,
    skipRefocus: boolean,
  ) => void;
  buttonAriaLabel?: string;
  buttonIcon?: React.ReactNode;
  title?: string;
  disabled?: boolean;
  /** Keeps the editor selection while a floating-toolbar picker is opened. */
  preserveSelection?: boolean;
}) {
  const [open, setOpen] = React.useState(false);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          title={title}
          aria-label={buttonAriaLabel}
          onMouseDown={
            preserveSelection ? (event) => event.preventDefault() : undefined
          }
          className={cn(
            "inline-flex items-center gap-1 h-8 px-2 rounded text-sm",
            "hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none",
          )}
        >
          <span className="relative inline-flex items-center justify-center w-4 h-4">
            {buttonIcon}
            <span
              aria-hidden
              className="absolute -bottom-1 left-0 right-0 h-1 rounded-sm border border-black/10"
              style={{ background: color }}
            />
          </span>
          <ChevronDownIcon className="w-3 h-3 opacity-60" />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={4}
        data-lexical-floating-toolbar-popover={
          preserveSelection ? "true" : undefined
        }
        className="w-auto p-3"
        onFocusOutside={(e) => {
          const target = (e as any).detail?.originalEvent?.target;
          if (
            target instanceof Element &&
            target.closest('[contenteditable="true"]')
          ) {
            e.preventDefault();
          }
        }}
      >
        <ColorPicker color={color} onChange={onChange} />
      </PopoverContent>
    </Popover>
  );
}
