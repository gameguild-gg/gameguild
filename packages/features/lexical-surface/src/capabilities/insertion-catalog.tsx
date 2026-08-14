"use client";

import {
  AlertCircle,
  BarChart3,
  Columns3,
  Film,
  GitBranch,
  MousePointerClick,
  PanelTopOpen,
  Pencil,
  Sigma,
  Table,
} from "lucide-react";
import { HorizontalRuleIcon, StickyIcon } from "../icons";
import { INSERT_ADMONITION_LEXICAL_COMMAND } from "../features/admonition";
import { INSERT_BUTTON_LEXICAL_COMMAND } from "../features/button";
import { INSERT_COLLAPSIBLE_COMMAND } from "../features/collapsible";
import { INSERT_DIVIDER_LEXICAL_COMMAND } from "../features/divider";
import { InsertEquationDialog } from "../features/equation";
import { INSERT_EXCALIDRAW_COMMAND } from "../features/excalidraw";
import { InsertLayoutDialog } from "../features/layout";
import { INSERT_MEDIA_LEXICAL_COMMAND } from "../features/media";
import { INSERT_MERMAID_LEXICAL_COMMAND } from "../features/mermaid";
import { INSERT_STICKY_COMMAND } from "../features/sticky";
import { InsertTableDialog } from "../features/table";
import { INSERT_VEGA_LITE_LEXICAL_COMMAND } from "../features/vega-lite";
import type { LexicalSurfaceFeatures } from "./feature-flags";
import type { InsertionDefinition, InsertionSurface } from "./insertion-types";

const BOTH_SURFACES = ["toolbar", "picker"] as const;

export const INSERTION_CATALOG: readonly InsertionDefinition[] = [
  {
    id: "divider",
    feature: "divider",
    label: "Horizontal Rule",
    keywords: ["horizontal rule", "divider", "hr"],
    Icon: HorizontalRuleIcon,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_DIVIDER_LEXICAL_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "equation",
    feature: "equation",
    label: "Equation",
    keywords: ["equation", "katex", "latex", "math"],
    Icon: Sigma,
    dialog: {
      title: "Insert Equation",
      contentClassName: "sm:max-w-[720px]",
      render: ({ activeEditor, onClose }) => (
        <InsertEquationDialog activeEditor={activeEditor} onClose={onClose} />
      ),
    },
    surfaces: BOTH_SURFACES,
  },
  {
    id: "table",
    feature: "table",
    label: "Table",
    keywords: ["table", "grid", "rows", "columns"],
    Icon: Table,
    dialog: {
      title: "Insert Table",
      render: ({ activeEditor, onClose }) => (
        <InsertTableDialog activeEditor={activeEditor} onClose={onClose} />
      ),
    },
    surfaces: BOTH_SURFACES,
  },
  {
    id: "excalidraw",
    feature: "excalidraw",
    label: "Excalidraw",
    keywords: ["excalidraw", "diagram", "drawing", "sketch"],
    Icon: Pencil,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_EXCALIDRAW_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "layout",
    feature: "layout",
    label: "Columns Layout",
    keywords: ["columns", "layout", "grid"],
    Icon: Columns3,
    dialog: {
      title: "Insert Columns Layout",
      render: ({ activeEditor, onClose }) => (
        <InsertLayoutDialog activeEditor={activeEditor} onClose={onClose} />
      ),
    },
    surfaces: BOTH_SURFACES,
  },
  {
    id: "collapsible",
    feature: "collapsible",
    label: "Collapsible container",
    keywords: ["collapsible", "accordion", "details", "toggle"],
    Icon: PanelTopOpen,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_COLLAPSIBLE_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "sticky",
    feature: "sticky",
    label: "Sticky Note",
    keywords: ["sticky", "note", "postit", "memo"],
    Icon: StickyIcon,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_STICKY_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "admonition",
    feature: "admonition",
    label: "Admonition",
    keywords: [
      "admonition",
      "callout",
      "note",
      "warning",
      "info",
      "tip",
      "alert",
    ],
    Icon: AlertCircle,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_ADMONITION_LEXICAL_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "button",
    feature: "button",
    label: "Button",
    keywords: ["button", "link", "action", "cta", "download"],
    Icon: MousePointerClick,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_BUTTON_LEXICAL_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "mermaid",
    feature: "mermaid",
    label: "Mermaid Diagram",
    keywords: [
      "mermaid",
      "diagram",
      "flowchart",
      "chart",
      "graph",
      "sequence",
      "gantt",
      "class",
    ],
    Icon: GitBranch,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_MERMAID_LEXICAL_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "vega-lite",
    feature: "vegaLite",
    label: "Vega-Lite Chart",
    keywords: [
      "vega",
      "vega-lite",
      "chart",
      "graph",
      "plot",
      "visualization",
      "bar",
      "line",
      "scatter",
    ],
    Icon: BarChart3,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_VEGA_LITE_LEXICAL_COMMAND, undefined),
    surfaces: BOTH_SURFACES,
  },
  {
    id: "media",
    feature: "media",
    label: "Media Block",
    keywords: [
      "media",
      "image",
      "video",
      "audio",
      "gallery",
      "photo",
      "music",
      "mp4",
      "mp3",
    ],
    Icon: Film,
    execute: (editor) =>
      editor.dispatchCommand(INSERT_MEDIA_LEXICAL_COMMAND, {
        mediaType: "image",
      }),
    surfaces: BOTH_SURFACES,
  },
];

export function getEnabledInsertions(
  features: Required<LexicalSurfaceFeatures>,
  surface: InsertionSurface,
): readonly InsertionDefinition[] {
  return INSERTION_CATALOG.filter(
    (definition) =>
      definition.surfaces.includes(surface) && features[definition.feature],
  );
}
