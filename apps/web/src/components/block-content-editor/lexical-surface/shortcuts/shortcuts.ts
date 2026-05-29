/**
 * Keyboard shortcut detectors. Ported from
 * `packages/lexical-playground/src/plugins/ShortcutsPlugin/shortcuts.ts`.
 *
 * Each `isXxx(event)` returns true when the KeyboardEvent matches the
 * playground's shortcut combo. Display strings live in `SHORTCUTS`.
 */
import { IS_APPLE } from "@lexical/utils"
import { isModifierMatch } from "lexical"

const CONTROL_OR_META = { ctrlKey: !IS_APPLE, metaKey: IS_APPLE }

export const SHORTCUTS = Object.freeze({
  NORMAL: IS_APPLE ? "⌘+Opt+0" : "Ctrl+Alt+0",
  HEADING1: IS_APPLE ? "⌘+Opt+1" : "Ctrl+Alt+1",
  HEADING2: IS_APPLE ? "⌘+Opt+2" : "Ctrl+Alt+2",
  HEADING3: IS_APPLE ? "⌘+Opt+3" : "Ctrl+Alt+3",
  NUMBERED_LIST: IS_APPLE ? "⌘+Shift+7" : "Ctrl+Shift+7",
  BULLET_LIST: IS_APPLE ? "⌘+Shift+8" : "Ctrl+Shift+8",
  CHECK_LIST: IS_APPLE ? "⌘+Shift+9" : "Ctrl+Shift+9",
  CODE_BLOCK: IS_APPLE ? "⌘+Opt+C" : "Ctrl+Alt+C",
  QUOTE: IS_APPLE ? "⌃+Shift+Q" : "Ctrl+Shift+Q",
  STRIKETHROUGH: IS_APPLE ? "⌘+Shift+X" : "Ctrl+Shift+X",
  LOWERCASE: IS_APPLE ? "⌃+Shift+1" : "Ctrl+Shift+1",
  UPPERCASE: IS_APPLE ? "⌃+Shift+2" : "Ctrl+Shift+2",
  CAPITALIZE: IS_APPLE ? "⌃+Shift+3" : "Ctrl+Shift+3",
  CENTER_ALIGN: IS_APPLE ? "⌘+Shift+E" : "Ctrl+Shift+E",
  JUSTIFY_ALIGN: IS_APPLE ? "⌘+Shift+J" : "Ctrl+Shift+J",
  LEFT_ALIGN: IS_APPLE ? "⌘+Shift+L" : "Ctrl+Shift+L",
  RIGHT_ALIGN: IS_APPLE ? "⌘+Shift+R" : "Ctrl+Shift+R",
  SUBSCRIPT: IS_APPLE ? "⌘+," : "Ctrl+,",
  SUPERSCRIPT: IS_APPLE ? "⌘+." : "Ctrl+.",
  INDENT: IS_APPLE ? "⌘+]" : "Ctrl+]",
  OUTDENT: IS_APPLE ? "⌘+[" : "Ctrl+[",
  CLEAR_FORMATTING: IS_APPLE ? "⌘+\\" : "Ctrl+\\",
  REDO: IS_APPLE ? "⌘+Shift+Z" : "Ctrl+Y",
  UNDO: IS_APPLE ? "⌘+Z" : "Ctrl+Z",
  BOLD: IS_APPLE ? "⌘+B" : "Ctrl+B",
  ITALIC: IS_APPLE ? "⌘+I" : "Ctrl+I",
  UNDERLINE: IS_APPLE ? "⌘+U" : "Ctrl+U",
  INSERT_LINK: IS_APPLE ? "⌘+K" : "Ctrl+K",
})

const codeKeyNumber = (code: string) => code[code.length - 1]

export function isFormatParagraph(e: KeyboardEvent): boolean {
  const { code } = e
  return (
    (code === "Numpad0" || code === "Digit0") &&
    isModifierMatch(e, { ...CONTROL_OR_META, altKey: true })
  )
}

export function isFormatHeading(e: KeyboardEvent): "h1" | "h2" | "h3" | null {
  if (!e.code) return null
  const n = codeKeyNumber(e.code)
  if (!n || !(["1", "2", "3"] as const).includes(n as "1" | "2" | "3")) return null
  if (!isModifierMatch(e, { ...CONTROL_OR_META, altKey: true })) return null
  return `h${n}` as "h1" | "h2" | "h3"
}

export function isFormatNumberedList(e: KeyboardEvent): boolean {
  const { code } = e
  return (
    (code === "Numpad7" || code === "Digit7") &&
    isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
  )
}

export function isFormatBulletList(e: KeyboardEvent): boolean {
  const { code } = e
  return (
    (code === "Numpad8" || code === "Digit8") &&
    isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
  )
}

export function isFormatCheckList(e: KeyboardEvent): boolean {
  const { code } = e
  return (
    (code === "Numpad9" || code === "Digit9") &&
    isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
  )
}

export function isFormatCode(e: KeyboardEvent): boolean {
  return e.code === "KeyC" && isModifierMatch(e, { ...CONTROL_OR_META, altKey: true })
}

export function isFormatQuote(e: KeyboardEvent): boolean {
  return e.code === "KeyQ" && isModifierMatch(e, { ctrlKey: true, shiftKey: true })
}

export function isLowercase(e: KeyboardEvent): boolean {
  return (
    (e.code === "Numpad1" || e.code === "Digit1") &&
    isModifierMatch(e, { ctrlKey: true, shiftKey: true })
  )
}
export function isUppercase(e: KeyboardEvent): boolean {
  return (
    (e.code === "Numpad2" || e.code === "Digit2") &&
    isModifierMatch(e, { ctrlKey: true, shiftKey: true })
  )
}
export function isCapitalize(e: KeyboardEvent): boolean {
  return (
    (e.code === "Numpad3" || e.code === "Digit3") &&
    isModifierMatch(e, { ctrlKey: true, shiftKey: true })
  )
}

export function isStrikethrough(e: KeyboardEvent): boolean {
  return e.code === "KeyX" && isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
}

export function isIndent(e: KeyboardEvent): boolean {
  return e.code === "BracketRight" && isModifierMatch(e, CONTROL_OR_META)
}
export function isOutdent(e: KeyboardEvent): boolean {
  return e.code === "BracketLeft" && isModifierMatch(e, CONTROL_OR_META)
}

export function isCenterAlign(e: KeyboardEvent): boolean {
  return e.code === "KeyE" && isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
}
export function isLeftAlign(e: KeyboardEvent): boolean {
  return e.code === "KeyL" && isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
}
export function isRightAlign(e: KeyboardEvent): boolean {
  return e.code === "KeyR" && isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
}
export function isJustifyAlign(e: KeyboardEvent): boolean {
  return e.code === "KeyJ" && isModifierMatch(e, { ...CONTROL_OR_META, shiftKey: true })
}

export function isSubscript(e: KeyboardEvent): boolean {
  return e.code === "Comma" && isModifierMatch(e, CONTROL_OR_META)
}
export function isSuperscript(e: KeyboardEvent): boolean {
  return e.code === "Period" && isModifierMatch(e, CONTROL_OR_META)
}

export function isClearFormatting(e: KeyboardEvent): boolean {
  return e.code === "Backslash" && isModifierMatch(e, CONTROL_OR_META)
}
