/**
 * Slideshow Structure System (type3)
 * 
 * Estrutura para projetos tipo Slideshow
 * Cada slide usa o mesmo sistema de multi-panel do type2
 * 
 * Hierarquia: slides[] → blocks[] → cells[]
 */

import type { SerializedEditorState } from "lexical"

export interface SlideData {
  id: string // formato: s1, s2, s3...
  name?: string // Nome opcional do slide (ex: "Introdução", "Capítulo 1")
  // Ordem é definida pela posição no array slides[]
  
  // Blocos do slide (b1, b2, b3... - mesmo sistema do type2)
  blocks: Record<string, SerializedEditorState | string>
}

export type PreviewMode = "continuous" | "slide"

export interface SlideshowStructure {
  version: "slideshow-v1"
  slides: SlideData[]
}

/**
 * Detecta se os dados são do formato slideshow
 */
export function isSlideshowStructure(data: string): boolean {
  try {
    const parsed = JSON.parse(data)
    return parsed.version === "slideshow-v1" && Array.isArray(parsed.slides)
  } catch {
    return false
  }
}

/**
 * Converte formato para estrutura slideshow
 * Espera dados já no formato blocks: {b1, b2, b3...}
 */
export function migrateToSlideshowStructure(data: string): SlideshowStructure {
  try {
    const parsed = JSON.parse(data)
    
    // Se já é formato slideshow, retorna
    if (isSlideshowStructure(data)) {
      return parsed as SlideshowStructure
    }
    
    // Se é formato com blocos
    if (parsed.blocks && typeof parsed.blocks === 'object') {
      const blocks = parsed.blocks
      return {
        version: "slideshow-v1",
        slides: [
          {
            id: "s1",
            blocks: Object.entries(blocks).reduce((acc, [key, value]: [string, any]) => {
              acc[key] = typeof value === 'string' ? value : JSON.stringify(value)
              return acc
            }, {} as Record<string, string>)
          }
        ]
      }
    }
    
    // Formato single (estado direto) - assume b1
    return {
      version: "slideshow-v1",
      slides: [
        {
          id: "s1",
          blocks: {
            b1: typeof parsed === 'string' ? parsed : JSON.stringify(parsed)
          }
        }
      ]
    }
  } catch (error) {
    console.error("Failed to migrate to slideshow structure:", error)
    // Retorna estrutura vazia em caso de erro
    return {
      version: "slideshow-v1",
      slides: []
    }
  }
}

/**
 * Cria uma nova estrutura slideshow vazia
 */
export function createEmptySlideshowStructure(): SlideshowStructure {
  const emptyState = {
    root: {
      children: [
        {
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1
        }
      ],
      direction: null,
      format: "",
      indent: 0,
      type: "root",
      version: 1
    }
  }
  
  return {
    version: "slideshow-v1",
    slides: [
      {
        id: "s1",
        name: "Slide 1",
        blocks: {
          b1: JSON.stringify(emptyState)
        }
      }
    ]
  }
}

/**
 * Adiciona um novo slide à estrutura
 * Cada slide usa o sistema multi-block (mesmo que type2)
 */
export function addSlide(
  structure: SlideshowStructure, 
  position?: number
): SlideshowStructure {
  const emptyState = {
    root: {
      children: [
        {
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1
        }
      ],
      direction: null,
      format: "",
      indent: 0,
      type: "root",
      version: 1
    }
  }
  
  const newSlide: SlideData = {
    id: generateSlideId(structure),
    name: `Slide ${structure.slides.length + 1}`,
    blocks: {
      b1: JSON.stringify(emptyState)
    }
  }
  
  const newSlides = [...structure.slides]
  
  if (position !== undefined) {
    // Inserir na posição específica
    newSlides.splice(position, 0, newSlide)
  } else {
    // Adicionar no final
    newSlides.push(newSlide)
  }
  
  return {
    ...structure,
    slides: newSlides
  }
}

/**
 * Remove um slide da estrutura
 */
export function removeSlide(
  structure: SlideshowStructure,
  slideId: string
): SlideshowStructure {
  const newSlides = structure.slides.filter(s => s.id !== slideId)
  
  return {
    ...structure,
    slides: newSlides
  }
}

/**
 * Reordena slides
 */
export function reorderSlides(
  structure: SlideshowStructure,
  fromIndex: number,
  toIndex: number
): SlideshowStructure {
  const newSlides = [...structure.slides]
  const [movedSlide] = newSlides.splice(fromIndex, 1)
  
  if (!movedSlide) {
    return structure // No slide to move
  }
  
  newSlides.splice(toIndex, 0, movedSlide)
  
  return {
    ...structure,
    slides: newSlides
  }
}

/**
 * Atualiza o nome de um slide
 */
export function updateSlideName(
  structure: SlideshowStructure,
  slideId: string,
  name: string
): SlideshowStructure {
  return {
    ...structure,
    slides: structure.slides.map(slide =>
      slide.id === slideId ? { ...slide, name } : slide
    )
  }
}

/**
 * Atualiza o estado de um slide
 */
export function updateSlideState(
  structure: SlideshowStructure,
  slideId: string,
  blocks: Record<string, SerializedEditorState | string>
): SlideshowStructure {
  return {
    ...structure,
    slides: structure.slides.map(slide =>
      slide.id === slideId ? { ...slide, blocks } : slide
    )
  }
}

/**
 * Gera um ID único para slide no formato s1, s2, s3...
 * Encontra o próximo número disponível baseado nos IDs existentes
 */
export function generateSlideId(structure?: SlideshowStructure): string {
  if (!structure || structure.slides.length === 0) {
    return "s1"
  }
  
  // Extrai os números dos IDs existentes (s1 -> 1, s2 -> 2, etc.)
  const existingNumbers = structure.slides
    .map(slide => {
      const match = slide.id.match(/^s(\d+)$/)
      return match && match[1] ? parseInt(match[1], 10) : 0
    })
    .filter(n => n > 0)
  
  // Encontra o próximo número disponível
  const maxNumber = existingNumbers.length > 0 ? Math.max(...existingNumbers) : 0
  return `s${maxNumber + 1}`
}

/**
 * Converte estrutura slideshow para string JSON
 */
export function serializeSlideshowStructure(structure: SlideshowStructure): string {
  return JSON.stringify(structure)
}

/**
 * Parse estrutura slideshow de string JSON
 */
export function parseSlideshowStructure(data: string): SlideshowStructure {
  return JSON.parse(data) as SlideshowStructure
}
