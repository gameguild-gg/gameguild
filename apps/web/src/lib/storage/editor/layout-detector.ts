/**
 * Layout Detection System
 * 
 * Detecta automaticamente o tipo de layout baseado na estrutura dos dados do projeto.
 * - Single Panel: dados diretamente na raiz
 * - Dual Panel: dados com estrutura {left, right}
 */

export type LayoutType = "single" | "dual"

export interface LayoutDetectionResult {
  layoutType: LayoutType
  hasLeft: boolean
  hasRight: boolean
  isSinglePanel: boolean
  isDualPanel: boolean
}

export interface EditorStates {
  single: any | null
  left: any | null
  right: any | null
}

/**
 * Analisa os dados do projeto e determina qual layout usar
 * @param data - String JSON com os dados do projeto
 * @returns Informações sobre o layout detectado
 */
export function detectProjectLayout(data: string): LayoutDetectionResult {
  try {
    const parsed = JSON.parse(data)
    
    // Verifica se tem estrutura de dual panel (left e right)
    const hasLeft = parsed.left !== undefined
    const hasRight = parsed.right !== undefined
    const isDualPanel = hasLeft && hasRight
    
    return {
      layoutType: isDualPanel ? "dual" : "single",
      hasLeft,
      hasRight,
      isSinglePanel: !isDualPanel,
      isDualPanel,
    }
  } catch (error) {
    console.error("Failed to parse project data for layout detection:", error)
    // Default to single panel se não conseguir parsear
    return {
      layoutType: "single",
      hasLeft: false,
      hasRight: false,
      isSinglePanel: true,
      isDualPanel: false,
    }
  }
}

/**
 * Extrai os estados dos editores baseado no layout detectado
 * @param data - String JSON com os dados do projeto
 * @param layoutType - Tipo de layout (single ou dual)
 * @returns Objetos com os estados dos editores
 */
export function extractEditorStates(data: string, layoutType: LayoutType): EditorStates {
  try {
    const parsed = JSON.parse(data)
    
    if (layoutType === "dual") {
      // Dual panel: extrair left e right
      const leftData = typeof parsed.left === 'string' ? JSON.parse(parsed.left) : parsed.left
      const rightData = typeof parsed.right === 'string' ? JSON.parse(parsed.right) : parsed.right
      
      return {
        single: null,
        left: leftData,
        right: rightData,
      }
    } else {
      // Single panel: dados diretos
      return {
        single: parsed,
        left: null,
        right: null,
      }
    }
  } catch (error) {
    console.error("Failed to extract editor states:", error)
    return {
      single: null,
      left: null,
      right: null,
    }
  }
}

/**
 * Cria a estrutura de dados correta baseado no layout type
 * @param layoutType - Tipo de layout
 * @param states - Estados dos editores
 * @returns String JSON formatada corretamente
 */
export function createProjectData(layoutType: LayoutType, states: Partial<EditorStates>): string {
  if (layoutType === "dual") {
    // Dual panel: criar estrutura {left, right}
    return JSON.stringify({
      left: states.left || createEmptyEditorState(),
      right: states.right || createEmptyEditorState(),
    })
  } else {
    // Single panel: dados diretos
    return JSON.stringify(states.single || createEmptyEditorState())
  }
}

/**
 * Cria um estado vazio do editor Lexical
 */
function createEmptyEditorState() {
  return {
    root: {
      children: [
        {
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1,
        },
      ],
      direction: null,
      format: "",
      indent: 0,
      type: "root",
      version: 1,
    },
  }
}
