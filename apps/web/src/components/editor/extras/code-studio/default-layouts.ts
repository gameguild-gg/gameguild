import type { LayoutConfig } from "./types"

export function createDefaultLayout(): LayoutConfig {
  return {
    displays: [
      {
        id: "display-1",
        name: "Base",
        aspectRatio: "1:1",
        panels: [
          { id: "explorer-1", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
          { id: "editor-1", type: "editor", row: 0, col: 3, rowSpan: 8, colSpan: 9, editorInstance: "unique" },
          { id: "output-1", type: "output", row: 8, col: 3, rowSpan: 4, colSpan: 9 },
        ],
      },
      {
        id: "display-2",
        name: "Display 2",
        aspectRatio: "2:1",
        panels: [
          { id: "explorer-2", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 6 },
          { id: "editor-2", type: "editor", row: 0, col: 6, rowSpan: 12, colSpan: 18, editorInstance: "multiple" },
        ],
      },
    ],
    activeDisplayId: "display-1",
    editMode: false,
  }
}
