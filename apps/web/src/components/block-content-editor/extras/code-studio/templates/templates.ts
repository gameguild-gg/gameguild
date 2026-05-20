import type { LayoutNode, PanelType } from "../types"

/**
 * A layout template seeds a display's splitter tree. Users pick a template
 * from the gallery, then drag dividers / add or remove panels to customize.
 *
 * Each template returns a fresh tree (new ids per panel) so two displays seeded
 * from the same template don't share leaf ids.
 */
export interface LayoutTemplate {
  id: string
  name: string
  description: string
  /**
   * Where this template fits.
   * - "compact": designed for the embed-sized Base display (≈500px tall, narrow).
   * - "expanded": designed for the IDE-sized secondary displays (Mirror, etc.).
   */
  scope: "compact" | "expanded"
  /** Minimal SVG-ish silhouette description, for the gallery card preview. */
  preview: {
    /** "h" = horizontal split, "v" = vertical split, leaf = single PanelType */
    schema: TemplateSilhouette
  }
  /** Factory returning a fresh splitter tree. */
  build: () => LayoutNode
}

export type TemplateSilhouette =
  | { kind: "leaf"; type: PanelType }
  | { kind: "split"; direction: "horizontal" | "vertical"; sizes: number[]; children: TemplateSilhouette[] }

// ---------------------------------------------------------------------------
// Helpers

let leafCounter = 0
function leaf(type: PanelType, editorInstance?: "multiple" | "unique"): LayoutNode {
  leafCounter += 1
  const id = `${type}-${Date.now()}-${leafCounter}`
  if (type === "full-editor" || type === "focus-editor") {
    return { kind: "leaf", id, type, editorInstance: editorInstance ?? "multiple" }
  }
  return { kind: "leaf", id, type }
}

let splitCounter = 0
function split(direction: "horizontal" | "vertical", sizes: number[], children: LayoutNode[]): LayoutNode {
  splitCounter += 1
  return {
    kind: "split",
    id: `split-${Date.now()}-${splitCounter}`,
    direction,
    sizes,
    children,
  }
}

// ---------------------------------------------------------------------------
// Templates

export const LAYOUT_TEMPLATES: LayoutTemplate[] = [
  {
    id: "single-file",
    name: "Single File",
    description: "One editor, no tabs or explorer. Best for the minimal inline embed.",
    scope: "compact",
    preview: { schema: { kind: "leaf", type: "focus-editor" } },
    build: () => leaf("focus-editor"),
  },
  {
    id: "editor-output",
    name: "Editor + Output",
    description: "Editor on top, output console below. The classic Base layout.",
    scope: "compact",
    preview: {
      schema: {
        kind: "split",
        direction: "vertical",
        sizes: [70, 30],
        children: [
          { kind: "leaf", type: "full-editor" },
          { kind: "leaf", type: "output" },
        ],
      },
    },
    build: () =>
      split("vertical", [70, 30], [
        leaf("full-editor"),
        leaf("output"),
      ]),
  },
  {
    id: "editor-output-explorer",
    name: "Editor + Output + Files",
    description: "Compact IDE: file explorer on the left, editor + output stacked on the right.",
    scope: "compact",
    preview: {
      schema: {
        kind: "split",
        direction: "horizontal",
        sizes: [25, 75],
        children: [
          { kind: "leaf", type: "explorer" },
          {
            kind: "split",
            direction: "vertical",
            sizes: [70, 30],
            children: [
              { kind: "leaf", type: "full-editor" },
              { kind: "leaf", type: "output" },
            ],
          },
        ],
      },
    },
    build: () =>
      split("horizontal", [25, 75], [
        leaf("explorer"),
        split("vertical", [70, 30], [
          leaf("full-editor"),
          leaf("output"),
        ]),
      ]),
  },
  {
    id: "ide-classic",
    name: "IDE Classic",
    description: "Explorer sidebar, editor, and output. The full IDE experience.",
    scope: "expanded",
    preview: {
      schema: {
        kind: "split",
        direction: "horizontal",
        sizes: [20, 80],
        children: [
          { kind: "leaf", type: "explorer" },
          {
            kind: "split",
            direction: "vertical",
            sizes: [70, 30],
            children: [
              { kind: "leaf", type: "full-editor" },
              { kind: "leaf", type: "output" },
            ],
          },
        ],
      },
    },
    build: () =>
      split("horizontal", [20, 80], [
        leaf("explorer"),
        split("vertical", [70, 30], [
          leaf("full-editor"),
          leaf("output"),
        ]),
      ]),
  },
  {
    id: "terminal-bottom",
    name: "Terminal Bottom",
    description: "Explorer + editor side-by-side, output as a wide terminal below.",
    scope: "expanded",
    preview: {
      schema: {
        kind: "split",
        direction: "vertical",
        sizes: [70, 30],
        children: [
          {
            kind: "split",
            direction: "horizontal",
            sizes: [25, 75],
            children: [
              { kind: "leaf", type: "explorer" },
              { kind: "leaf", type: "full-editor" },
            ],
          },
          { kind: "leaf", type: "output" },
        ],
      },
    },
    build: () =>
      split("vertical", [70, 30], [
        split("horizontal", [25, 75], [
          leaf("explorer"),
          leaf("full-editor"),
        ]),
        leaf("output"),
      ]),
  },
  {
    id: "side-by-side",
    name: "Side-by-Side",
    description: "Explorer on the left, editor on the right. No output panel.",
    scope: "expanded",
    preview: {
      schema: {
        kind: "split",
        direction: "horizontal",
        sizes: [25, 75],
        children: [
          { kind: "leaf", type: "explorer" },
          { kind: "leaf", type: "full-editor" },
        ],
      },
    },
    build: () =>
      split("horizontal", [25, 75], [
        leaf("explorer"),
        leaf("full-editor"),
      ]),
  },
]

export function getTemplate(id: string): LayoutTemplate | undefined {
  return LAYOUT_TEMPLATES.find(t => t.id === id)
}

export function getTemplatesByScope(scope: "compact" | "expanded"): LayoutTemplate[] {
  return LAYOUT_TEMPLATES.filter(t => t.scope === scope)
}

export function buildTemplateTree(id: string): LayoutNode {
  const template = getTemplate(id)
  if (!template) {
    // Safe fallback so callers always get a renderable tree.
    return getTemplate("editor-output")!.build()
  }
  return template.build()
}
