/**
 * Slideshow Structure System (type3)
 * 
 * Estrutura para projetos tipo Slideshow
 * Cada slide pode conter painéis (panels) com blocos (blocks) e células (cells)
 * 
 * Hierarquia: slides[] → panels[] → blocks[] → cells[]
 */

import type { SerializedEditorState } from "lexical"

export type SlideLayoutType = "single" | "multiple"

export interface SlideData {
  id: string // formato: s_<timestamp>_<random>
  type: SlideLayoutType
  name?: string // Nome opcional do slide (ex: "Introdução", "Capítulo 1")
  order: number // Ordem do slide na sequência
  
  // Blocos do slide (b1 para single, b1+b2+b3... para multiple)
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
            id: generateSlideId(),
            type: Object.keys(blocks).length > 1 ? "multiple" : "single",
            order: 0,
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
          id: generateSlideId(),
          type: "single",
          order: 0,
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
        id: generateSlideId(),
        type: "single",
        order: 0,
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
 */
export function addSlide(
  structure: SlideshowStructure, 
  type: SlideLayoutType,
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
    id: generateSlideId(),
    type,
    order: position !== undefined ? position : structure.slides.length,
    name: `Slide ${structure.slides.length + 1}`,
    blocks: type === "single" ? {
      b1: JSON.stringify(emptyState)
    } : {
      b1: JSON.stringify(emptyState),
      b2: JSON.stringify(emptyState)
    }
  }
  
  const newSlides = [...structure.slides]
  
  if (position !== undefined) {
    // Inserir na posição específica
    newSlides.splice(position, 0, newSlide)
    // Reordenar todos os slides
    newSlides.forEach((slide, index) => {
      slide.order = index
    })
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
  const newSlides = structure.slides
    .filter(s => s.id !== slideId)
    .map((slide, index) => ({
      ...slide,
      order: index
    }))
  
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
  
  // Atualizar ordem
  newSlides.forEach((slide, index) => {
    slide.order = index
  })
  
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
 * Gera um ID único para slide
 */
export function generateSlideId(): string {
  return `s_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
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
