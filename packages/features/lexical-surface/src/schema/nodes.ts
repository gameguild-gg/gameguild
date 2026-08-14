/**
 * Single source of truth for the node schema supported by LexicalSurface.
 * Nodes remain registered independently from editing feature flags so stored
 * documents can always be deserialized and rendered.
 */

import { HeadingNode, QuoteNode } from "@lexical/rich-text";
import { ListNode, ListItemNode } from "@lexical/list";
import { LinkNode, AutoLinkNode } from "@lexical/link";
import { CodeNode, CodeHighlightNode } from "@lexical/code";
import { CustomListNode } from "./custom-list-node";
import { EquationNode } from "../features/equation";
import { ExcalidrawNode } from "../features/excalidraw";
import { YouTubeNode, TweetNode, FigmaNode } from "../features/embeds";
import { TableNode, TableRowNode, TableCellNode } from "@lexical/table";
import { LayoutContainerNode, LayoutItemNode } from "../features/layout";
import {
  CollapsibleContainerNode,
  CollapsibleTitleNode,
  CollapsibleContentNode,
} from "../features/collapsible";
import { StickyNode } from "../features/sticky";
import { AdmonitionLexicalNode } from "../features/admonition";
import { ButtonLexicalNode } from "../features/button";
import { DividerLexicalNode } from "../features/divider";
import { MermaidLexicalNode } from "../features/mermaid";
import { VegaLiteLexicalNode } from "../features/vega-lite";
import { MediaLexicalNode } from "../features/media";
import { PageNode, PageContentNode } from "../features/page";

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
export const LEXICAL_SURFACE_NODES = [
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
  LinkNode,
  AutoLinkNode,
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
  AdmonitionLexicalNode,
  ButtonLexicalNode,
  DividerLexicalNode,
  MermaidLexicalNode,
  VegaLiteLexicalNode,
  MediaLexicalNode,
  PageNode,
  PageContentNode,
];
