export type SlideLayout =
  | "title-slide"
  | "title-content"
  | "two-column"
  | "content-only"
  | "image-only"
  | "full-image"
  | "blank"

export type SlideTheme = "light" | "dark" | "standard" | "gradient" | "custom" | "image"

export type TransitionEffect = "none" | "fade" | "slide" | "zoom" | "flip" | "cube"

export interface SlideShape {
  id: string
  type: "rectangle" | "circle" | "line" | "text"
  x: number
  y: number
  width: number
  height: number
  fill?: string
  stroke?: string
  strokeWidth?: number
  text?: string
  fontSize?: number
  fontWeight?: "normal" | "bold"
  fontStyle?: "normal" | "italic"
}

export interface SlideImage {
  id: string
  src: string
  alt?: string
  x: number
  y: number
  width: number
  height: number
}

export interface Slide {
  id: string
  title: string
  content: string
  layout: SlideLayout
  theme: SlideTheme
  backgroundImage?: string
  backgroundGradient?: string
  notes?: string
  images?: SlideImage[]
  shapes?: SlideShape[]
  customStyles?: {
    fontSize?: number
    fontFamily?: string
    textColor?: string
    backgroundColor?: string
  }
}

export interface PresentationData {
  slides: Slide[]
  title: string
  theme: SlideTheme
  transitionEffect: TransitionEffect
  autoAdvance: boolean
  autoAdvanceDelay: number
  autoAdvanceLoop?: boolean
  showControls: boolean
  customThemeColor?: string | null
  isNew?: boolean
}

export interface ParseProgress {
  progress: number
  status: string
}

export interface PresentationMetadata {
  title?: string
  author?: string
  subject?: string
  keywords?: string
  description?: string
  createdDate?: string
  modifiedDate?: string
  slideCount?: number
}
