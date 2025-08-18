export type SlideLayout = 
  | "title"
  | "content" 
  | "title-content"
  | "image-text"
  | "text-image"
  | "full-image"
  | "two-column"
  | "blank"

export type SlideTheme = 
  | "light"
  | "dark" 
  | "standard"
  | "gradient"
  | "custom"
  | "image"

export type TransitionEffect = 
  | "none"
  | "fade"
  | "slide"
  | "zoom"
  | "flip"
  | "cube"

export interface SlideShape {
  id: string
  type: "rectangle" | "circle" | "ellipse" | "line"
  x?: number
  y?: number
  width?: number
  height?: number
  fill?: string
  stroke?: string
  strokeWidth?: number
  text?: string
}

export interface ImageFilters {
  brightness: number
  contrast: number
  saturation: number
  blur: number
  hueRotate: number
  opacity: number
}

export interface ImageSize {
  width: number
  height: number
  objectFit: "cover" | "contain" | "fill" | "scale-down"
}

export interface Slide {
  id: string
  title: string
  content: string
  layout: SlideLayout
  theme: SlideTheme
  backgroundImage?: string
  backgroundGradient?: string
  notes: string
  shapes?: SlideShape[]
  images?: string[]
  filters?: ImageFilters
  imageSize?: ImageSize
}

export interface PresentationData {
  slides: Slide[]
  title: string
  theme: SlideTheme
  transitionEffect: TransitionEffect
  autoAdvance: boolean
  autoAdvanceDelay: number
  autoAdvanceLoop: boolean
  showControls: boolean
  isNew?: boolean
  customThemeColor?: string | null
}

export interface ParseProgress {
  progress: number
  status: string
}
