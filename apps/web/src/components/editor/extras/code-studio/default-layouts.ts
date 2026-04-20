import type { LayoutConfig, EditorMode } from "./types"

export function createDefaultLayout(mode: EditorMode = "execution"): LayoutConfig {
  if (mode === "test") {
    return createTestModeLayout()
  }
  return createExecutionModeLayout()
}

function createExecutionModeLayout(): LayoutConfig {
  return {
    displays: [
      {
        id: "display-1",
        name: "Base",
        aspectRatio: "1:1",
        panels: [
          { id: "explorer-1", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
          { id: "editor-1", type: "full-editor", row: 0, col: 3, rowSpan: 8, colSpan: 9, editorInstance: "multiple" },
          { id: "output-1", type: "output", row: 8, col: 3, rowSpan: 4, colSpan: 9 },
        ],
      },
      {
        id: "display-2",
        name: "Mirror",
        aspectRatio: "2:1",
        panels: [
          { id: "explorer-2", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 6 },
          { id: "editor-2", type: "full-editor", row: 0, col: 6, rowSpan: 8, colSpan: 18, editorInstance: "multiple" },
          { id: "output-2", type: "output", row: 8, col: 6, rowSpan: 4, colSpan: 18 },
        ],
      },
    ],
    activeDisplayId: "display-1",
    editMode: false,
  }
}

function createTestModeLayout(): LayoutConfig {
  return {
    displays: [
      {
        id: "display-1",
        name: "Base",
        aspectRatio: "1:1",
        panels: [
          { id: "explorer-1", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
          { id: "editor-1", type: "full-editor", row: 0, col: 3, rowSpan: 8, colSpan: 9, editorInstance: "multiple" },
          { id: "output-1", type: "output", row: 8, col: 3, rowSpan: 4, colSpan: 9 },
        ],
      },
      {
        id: "display-2",
        name: "Mirror",
        aspectRatio: "2:1",
        panels: [
          { id: "explorer-2", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 6 },
          { id: "editor-2", type: "full-editor", row: 0, col: 6, rowSpan: 8, colSpan: 18, editorInstance: "multiple" },
          { id: "output-2", type: "output", row: 8, col: 6, rowSpan: 4, colSpan: 18 },
        ],
      },
      {
        id: "display-3",
        name: "Test",
        aspectRatio: "2:1",
        panels: [
          { id: "explorer-3", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 6 },
          { id: "editor-3", type: "full-editor", row: 0, col: 6, rowSpan: 8, colSpan: 18, editorInstance: "unique" },
          { id: "output-3", type: "output", row: 8, col: 6, rowSpan: 4, colSpan: 18 },
        ],
      },
    ],
    activeDisplayId: "display-1",
    editMode: false,
  }
}
