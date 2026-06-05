/**
 * Shared Lexical Configuration
 *
 * Single source of truth for nodes and theme used by every inline Lexical
 * editor in the block content editor (rich-text block, essay quiz answer,
 * etc.). Centralising it guarantees that "our Lexical" looks and behaves
 * the same everywhere \u2014 same headings, lists, quotes, code, links \u2014 and
 * eliminates drift between near-duplicate copies.
 */

import { HeadingNode, QuoteNode } from "@lexical/rich-text"
import { ListNode, ListItemNode } from "@lexical/list"
import { LinkNode, AutoLinkNode } from "@lexical/link"
import { CodeNode, CodeHighlightNode } from "@lexical/code"
import { HorizontalRuleNode } from "@lexical/react/LexicalHorizontalRuleNode"
import { CustomListNode } from "../../nodes/custom-list-node"
import { BlockEmbedNode } from "../../nodes/block-embed-node"
import { EquationNode } from "../../lexical-surface/equation"
import { ExcalidrawNode } from "../../lexical-surface/excalidraw"
import { YouTubeNode, TweetNode, FigmaNode } from "../../lexical-surface/embeds"
import { TableNode, TableRowNode, TableCellNode } from "@lexical/table"
import { LayoutContainerNode, LayoutItemNode } from "../../lexical-surface/layout"
import {
  CollapsibleContainerNode,
  CollapsibleTitleNode,
  CollapsibleContentNode,
} from "../../lexical-surface/collapsible"
import { StickyNode } from "../../lexical-surface/sticky"

/**
 * Full node set supported by our Lexical instances. Adding a node here
 * makes it available across every editor that imports this list.
 *
 * `CustomListNode` is a subclass of `ListNode` with its own type
 * (`"custom-list"`), so both can be registered side-by-side without
 * triggering Lexical's `errorOnTypeKlassMismatch`. The floating-toolbar
 * list menus instantiate `CustomListNode` directly to carry custom
 * list-style-type and marker-color metadata; `ListNode` stays available
 * for the stock `ListPlugin` (INSERT_*_LIST commands, etc.).
 */
export const SHARED_LEXICAL_NODES = [
  HeadingNode,
  QuoteNode,
  // ListNode mantido para o ListPlugin padrão (comandos INSERT_*_LIST).
  // CustomListNode tem `getType() === "custom-list"` (tipo próprio), então
  // os dois coexistem no registry sem conflito de klass.
  ListNode,
  ListItemNode,
  CustomListNode,
  CodeNode,
  CodeHighlightNode,
  HorizontalRuleNode,
  LinkNode,
  AutoLinkNode,
  BlockEmbedNode,
  EquationNode,
  ExcalidrawNode,
  YouTubeNode,
  TweetNode,
  FigmaNode,
  TableNode,
  TableRowNode,
  TableCellNode,
  LayoutContainerNode,
  LayoutItemNode,
  CollapsibleContainerNode,
  CollapsibleTitleNode,
  CollapsibleContentNode,
  StickyNode,
]

/**
 * Theme classes shared across every inline Lexical editor. Mirrors the
 * styling originally used by the rich-text block so authors get a
 * consistent experience whether they are writing a rich-text section or
 * answering an essay quiz question.
 */
export const SHARED_LEXICAL_THEME = {
  text: {
    bold: "font-bold",
    italic: "italic",
    underline: "underline",
    strikethrough: "line-through",
    code: "bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded font-mono text-sm",
  },
  paragraph: "my-2",
  heading: {
    h1: "text-3xl font-bold my-3",
    h2: "text-2xl font-bold my-2",
    h3: "text-xl font-bold my-2",
    h4: "text-lg font-bold my-1",
    h5: "text-base font-bold my-1",
  },
  list: {
    ul: "list-disc list-inside ml-4 my-2",
    ol: "list-decimal list-inside ml-4 my-2",
    listitem: "my-0.5",
    nested: {
      listitem: "ml-4",
    },
  },
  quote:
    "border-l-4 border-gray-300 dark:border-gray-600 pl-4 italic text-gray-600 dark:text-gray-400 my-2",
  code: "bg-gray-100 dark:bg-gray-800 p-3 rounded font-mono text-sm my-2",
  link: "text-blue-600 dark:text-blue-400 underline hover:text-blue-800 dark:hover:text-blue-300 cursor-pointer",
}
