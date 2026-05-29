/**
 * ToolbarContext — ported from facebook/lexical playground
 * (`packages/lexical-playground/src/context/ToolbarContext.tsx`).
 *
 * Minor adjustments for our Wave A scope:
 * - Removed `codeLanguage` / `codeTheme` (we don't ship code-prism or
 *   code-shiki — Wave B). Block-format dropdown still recognises the
 *   plain `code` block.
 * - Kept all remaining keys (isHighlight, lowercase/upper/capitalize,
 *   listStartNumber, isImageCaption) so future ports plug in trivially.
 */
"use client"

import type { JSX, ReactNode } from "react"
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react"
import type { ElementFormatType } from "lexical"
import { DEFAULT_PAGE_SETTINGS, type PageSettings } from "../page"

export const MIN_ALLOWED_FONT_SIZE = 8
export const MAX_ALLOWED_FONT_SIZE = 72
export const DEFAULT_FONT_SIZE = 16

export const blockTypeToBlockName = {
  bullet: "Bulleted List",
  check: "Check List",
  code: "Code Block",
  h1: "Heading 1",
  h2: "Heading 2",
  h3: "Heading 3",
  h4: "Heading 4",
  h5: "Heading 5",
  h6: "Heading 6",
  number: "Numbered List",
  paragraph: "Normal",
  quote: "Quote",
} as const

export const rootTypeToRootName = {
  root: "Root",
  table: "Table",
} as const

const INITIAL_TOOLBAR_STATE = {
  bgColor: "#fff",
  blockType: "paragraph" as keyof typeof blockTypeToBlockName,
  canRedo: false,
  canUndo: false,
  elementFormat: "left" as ElementFormatType,
  fontColor: "#000",
  fontFamily: "Arial",
  fontSize: `${DEFAULT_FONT_SIZE}px`,
  fontSizeInputValue: `${DEFAULT_FONT_SIZE}`,
  isBold: false,
  isCode: false,
  isHighlight: false,
  isImageCaption: false,
  isItalic: false,
  isLink: false,
  isRTL: false,
  isStrikethrough: false,
  isSubscript: false,
  isSuperscript: false,
  isUnderline: false,
  isLowercase: false,
  isUppercase: false,
  isCapitalize: false,
  rootType: "root" as keyof typeof rootTypeToRootName,
  listStartNumber: null as number | null,
}

type ToolbarState = typeof INITIAL_TOOLBAR_STATE
type ToolbarStateKey = keyof ToolbarState
type ToolbarStateValue<Key extends ToolbarStateKey> = ToolbarState[Key]

type ContextShape = {
  toolbarState: ToolbarState
  updateToolbarState<Key extends ToolbarStateKey>(key: Key, value: ToolbarStateValue<Key>): void
  pageSettings: PageSettings
  setPageSettings: (next: PageSettings) => void
}

const Context = createContext<ContextShape | undefined>(undefined)

export const ToolbarContextProvider = ({
  children,
  initialPageSettings,
}: {
  children: ReactNode
  initialPageSettings?: PageSettings
}): JSX.Element => {
  const [toolbarState, setToolbarState] = useState(INITIAL_TOOLBAR_STATE)
  const [pageSettings, setPageSettings] = useState<PageSettings>(
    initialPageSettings ?? DEFAULT_PAGE_SETTINGS,
  )
  const selectionFontSize = toolbarState.fontSize

  const updateToolbarState = useCallback(
    <Key extends ToolbarStateKey>(key: Key, value: ToolbarStateValue<Key>) => {
      setToolbarState((prev) => ({
        ...prev,
        [key]: value,
      }))
    },
    [],
  )

  useEffect(() => {
    updateToolbarState("fontSizeInputValue", selectionFontSize.slice(0, -2))
  }, [selectionFontSize, updateToolbarState])

  const contextValue = useMemo(
    () => ({
      toolbarState,
      updateToolbarState,
      pageSettings,
      setPageSettings,
    }),
    [toolbarState, updateToolbarState, pageSettings],
  )

  return <Context.Provider value={contextValue}>{children}</Context.Provider>
}

export const useToolbarState = () => {
  const context = useContext(Context)
  if (context === undefined) {
    throw new Error("useToolbarState must be used within a ToolbarContextProvider")
  }
  return context
}
