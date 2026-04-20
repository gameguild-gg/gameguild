/**
 * Layout Detection System
 * 
 * Single-pane layout only. Detects and extracts editor state from project data.
 */

export interface EditorStates {
  blocks: Record<string, any> // {b1: state} - single block only
}

/**
 * Extrai os estados dos editores (always single block b1)
 * @param data - String JSON com os dados do projeto
 * @returns Objetos com os estados dos editores
 */
export function extractEditorStates(data: string): EditorStates {
  try {
    const parsed = JSON.parse(data)
    return {
      blocks: {
        b1: parsed,
      },
    }
  } catch (error) {
    console.error("Failed to extract editor states:", error)
    return {
      blocks: {},
    }
  }
}

/**
 * Cria a estrutura de dados para single-pane layout
 * @param states - Estados dos editores
 * @returns String JSON formatada corretamente
 */
export function createProjectData(states: Partial<EditorStates>): string {
  const editorStates = states as EditorStates
  const b1State = editorStates.blocks?.b1 || createEmptyEditorState()
  return JSON.stringify(b1State)
}

/**
 * Cria um estado vazio no formato cells (basilar)
 */
function createEmptyEditorState() {
  return []
}
