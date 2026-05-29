/**
 * Consolidated Tailwind theme for every Lexical surface in the
 * block-content-editor. Ported from facebook/lexical playground
 * (PlaygroundEditorTheme.css) to Tailwind classes with dark-mode variants.
 *
 * Replaces `SHARED_LEXICAL_THEME` from `../lib/lexical/shared-lexical-config`.
 *
 * Entries that map to nodes we haven't registered yet (layout, etc.)
 * are kept intentionally — Lexical silently ignores theme entries for
 * unregistered node types, and pre-declaring them keeps future
 * integration trivial (just register the node).
 */
import type { EditorThemeClasses } from "lexical"

export const LEXICAL_SURFACE_THEME: EditorThemeClasses = {
  ltr: "text-left",
  rtl: "text-right",
  paragraph: "my-2 relative",
  quote:
    "border-l-4 border-gray-300 dark:border-gray-600 pl-4 italic text-gray-600 dark:text-gray-400 my-2",
  heading: {
    h1: "text-3xl font-bold my-3",
    h2: "text-2xl font-bold my-2",
    h3: "text-xl font-bold my-2",
    h4: "text-lg font-bold my-1",
    h5: "text-base font-bold my-1",
    h6: "text-sm font-bold my-1 uppercase tracking-wide",
  },
  list: {
    nested: {
      listitem: "list-none",
    },
    ol: "list-decimal list-inside ml-4 my-2",
    ul: "list-disc list-inside ml-4 my-2",
    listitem: "my-0.5",
    listitemChecked:
      "relative pl-6 my-0.5 line-through text-gray-500 dark:text-gray-400 before:content-[''] before:absolute before:left-0 before:top-1 before:w-4 before:h-4 before:border before:border-gray-400 before:rounded before:bg-blue-500 after:content-[''] after:absolute after:left-[6px] after:top-[3px] after:w-[4px] after:h-[8px] after:border-r-2 after:border-b-2 after:border-white after:rotate-45",
    listitemUnchecked:
      "relative pl-6 my-0.5 before:content-[''] before:absolute before:left-0 before:top-1 before:w-4 before:h-4 before:border before:border-gray-400 before:rounded",
  },
  hashtag: "bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-300 rounded px-1",
  image: "inline-block max-w-full",
  link: "text-blue-600 dark:text-blue-400 underline hover:text-blue-800 dark:hover:text-blue-300 cursor-pointer",
  text: {
    bold: "font-bold",
    code: "bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded font-mono text-sm",
    italic: "italic",
    strikethrough: "line-through",
    subscript: "align-sub text-xs",
    superscript: "align-super text-xs",
    underline: "underline",
    underlineStrikethrough: "[text-decoration:underline_line-through]",
    lowercase: "lowercase",
    uppercase: "uppercase",
    capitalize: "capitalize",
    highlight: "bg-yellow-200 dark:bg-yellow-700/50 px-0.5 rounded",
  },
  code: "block bg-gray-100 dark:bg-gray-800 p-3 rounded font-mono text-sm my-2 overflow-x-auto leading-relaxed",
  codeHighlight: {
    atrule: "text-purple-600 dark:text-purple-400",
    attr: "text-purple-600 dark:text-purple-400",
    boolean: "text-red-600 dark:text-red-400",
    builtin: "text-amber-600 dark:text-amber-400",
    cdata: "text-gray-500 dark:text-gray-400",
    char: "text-amber-600 dark:text-amber-400",
    class: "text-yellow-600 dark:text-yellow-400",
    "class-name": "text-yellow-600 dark:text-yellow-400",
    comment: "text-gray-500 dark:text-gray-400 italic",
    constant: "text-red-600 dark:text-red-400",
    deleted: "text-red-600 dark:text-red-400",
    doctype: "text-gray-500 dark:text-gray-400",
    entity: "text-orange-600 dark:text-orange-400",
    function: "text-blue-600 dark:text-blue-400",
    important: "text-red-600 dark:text-red-400 font-bold",
    inserted: "text-green-600 dark:text-green-400",
    keyword: "text-purple-600 dark:text-purple-400",
    namespace: "text-orange-600 dark:text-orange-400",
    number: "text-red-600 dark:text-red-400",
    operator: "text-gray-700 dark:text-gray-300",
    prolog: "text-gray-500 dark:text-gray-400",
    property: "text-blue-600 dark:text-blue-400",
    punctuation: "text-gray-700 dark:text-gray-300",
    regex: "text-green-600 dark:text-green-400",
    selector: "text-amber-600 dark:text-amber-400",
    string: "text-amber-600 dark:text-amber-400",
    symbol: "text-red-600 dark:text-red-400",
    tag: "text-red-600 dark:text-red-400",
    url: "text-blue-600 dark:text-blue-400 underline",
    variable: "text-orange-600 dark:text-orange-400",
  },
  mark: "bg-yellow-100 dark:bg-yellow-900/30 border-b border-yellow-400 dark:border-yellow-600",
  markOverlap:
    "bg-yellow-200 dark:bg-yellow-800/40 border-b border-yellow-500 dark:border-yellow-500",
  embedBlock: {
    base: "select-none",
    focus: "outline outline-2 outline-blue-500 rounded",
  },
  table:
    "border-collapse my-2 table-fixed w-full max-w-full",
  tableScrollableWrapper: "overflow-x-auto my-2",
  tableRow: "",
  tableCell:
    "border border-gray-300 dark:border-gray-700 align-top p-2 relative break-words",
  tableCellHeader:
    "bg-gray-100 dark:bg-gray-800 font-bold border border-gray-300 dark:border-gray-700 align-top p-2 text-start break-words",
  tableCellSelected: "bg-blue-100/40 dark:bg-blue-900/30",
  tableSelected: "outline outline-2 outline-blue-500",
  tableSelection: "[caret-color:transparent]",
  tableAddColumns:
    "absolute top-0 right-[-12px] h-full w-3 bg-gray-200 dark:bg-gray-700 hover:bg-blue-500 cursor-pointer",
  tableAddRows:
    "absolute bottom-[-12px] left-0 w-full h-3 bg-gray-200 dark:bg-gray-700 hover:bg-blue-500 cursor-pointer",
  tableCellActionButton:
    "absolute top-1 right-1 w-5 h-5 rounded bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 flex items-center justify-center",
  tableCellActionButtonContainer: "absolute top-1 right-1",
  tableCellResizer: "absolute right-[-2px] top-0 h-full w-1 cursor-col-resize",
  indent: "[--lexical-indent:40px]",
  // Tailwind utility hooks for ElementFormatPlugin alignment classes.
  // Lexical writes these as theme classes on the block element.
  // (These keys aren't part of the official type, but Lexical accepts
  // arbitrary string keys at runtime.)
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ...({
    textLeft: "text-left",
    textCenter: "text-center",
    textRight: "text-right",
    textJustify: "text-justify",
    textStart: "text-start",
    textEnd: "text-end",
  } as any),
}
