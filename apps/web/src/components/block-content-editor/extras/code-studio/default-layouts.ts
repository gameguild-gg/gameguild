import type { LayoutConfig, EditorMode, DisplayConfig } from "./types"
import { buildTemplateTree } from "./templates/templates"

/**
 * Default layout for a freshly created code-studio block.
 *
 * Seeds two displays: "Base" (editor + output) and "Mirror" (full IDE).
 * Users can apply other templates, rename, add or remove displays freely.
 *
 * Test mode adds a third "Test" display.
 */
export function createDefaultLayout(mode: EditorMode = "execution"): LayoutConfig {
  const displays: DisplayConfig[] = [
    {
      id: "display-1",
      name: "Base",
      templateId: "editor-output",
      root: buildTemplateTree("editor-output"),
    },
    {
      id: "display-2",
      name: "Mirror",
      templateId: "ide-classic",
      root: buildTemplateTree("ide-classic"),
    },
  ]

  if (mode === "test") {
    displays.push({
      id: "display-3",
      name: "Test",
      templateId: "ide-classic",
      root: buildTemplateTree("ide-classic"),
    })
  }

  return {
    displays,
    activeDisplayId: "display-1",
    editMode: false,
  }
}
