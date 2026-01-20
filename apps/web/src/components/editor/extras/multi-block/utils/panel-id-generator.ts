import { PANEL_ID_PREFIX } from "../constants"
import type { PanelData } from "../types"

/**
 * Generates a new panel ID based on existing panels
 */
export function generatePanelId(panels: PanelData[]): string {
  const panelNumbers = panels.map(p => {
    const match = p.id.match(/^panel-(\d+)$/)
    return match && match[1] ? parseInt(match[1], 10) : 0
  })
  const maxNumber = panelNumbers.length > 0 ? Math.max(...panelNumbers) : 0
  return `${PANEL_ID_PREFIX}${maxNumber + 1}`
}
