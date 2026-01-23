import type { SerializedEditorState, SerializedLexicalNode } from "lexical"
import type { QuizData } from "@/components/editor/nodes/quiz-node"
import type { CodeStudioData } from "@/components/editor/extras/code-studio/types"
import type { ImageData } from "@/components/editor/nodes/image-node"
import type { VideoData } from "@/components/editor/nodes/video-node"
import type { AudioData } from "@/components/editor/nodes/audio-node"
import type { GalleryData } from "@/components/editor/nodes/gallery-node"
import type { YouTubeData } from "@/components/editor/nodes/youtube-node"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import type { VegaLiteData } from "@/components/editor/nodes/vega-lite-node"
import type { PresentationData } from "@/components/editor/nodes/presentation/types"
import type { SourceData } from "@/components/editor/nodes/source-node"
import type { SpotifyData } from "@/components/editor/nodes/spotify-node"
import type { ButtonData } from "@/components/editor/nodes/button-node"
import type { HeaderData } from "@/components/editor/nodes/header-node"
import type { DividerData } from "@/components/editor/nodes/divider-node"
import type { AdmonitionData } from "@/components/editor/nodes/admonition-node"
import type { MarkdownData } from "@/components/editor/nodes/markdown-node"
import type { HTMLData } from "@/components/editor/nodes/html-node"
import type { TableData } from "@/components/editor/nodes/table-node"
import type { ProjectData as ProjectNodeData_Internal } from "@/components/editor/nodes/project-node"

export type CellType = 
  | "text"
  | "heading"
  | "paragraph"
  | "quote"
  | "list"
  | "quiz"
  | "code-studio"
  | "image"
  | "video"
  | "audio"
  | "gallery"
  | "youtube"
  | "spotify"
  | "mermaid"
  | "vega-lite"
  | "presentation"
  | "source"
  | "markdown"
  | "html"
  | "header"
  | "divider"
  | "button"
  | "admonition"
  | "table"
  | "project"

interface BaseCell {
  id: string
  type: CellType
}

export interface TextCell extends BaseCell {
  type: "text" | "heading" | "paragraph" | "quote" | "list"
  content: SerializedLexicalNode[]
}

export interface QuizCell extends BaseCell {
  type: "quiz"
  data: QuizData
}

export interface CodeStudioCell extends BaseCell {
  type: "code-studio"
  data: CodeStudioData
}

export interface ImageCell extends BaseCell {
  type: "image"
  data: ImageData
}

export interface VideoCell extends BaseCell {
  type: "video"
  data: VideoData
}

export interface AudioCell extends BaseCell {
  type: "audio"
  data: AudioData
}

export interface GalleryCell extends BaseCell {
  type: "gallery"
  data: GalleryData
}

export interface YouTubeCell extends BaseCell {
  type: "youtube"
  data: YouTubeData
}

export interface SpotifyCell extends BaseCell {
  type: "spotify"
  data: SpotifyData
}

export interface MermaidCell extends BaseCell {
  type: "mermaid"
  data: MermaidData
}

export interface VegaLiteCell extends BaseCell {
  type: "vega-lite"
  data: VegaLiteData
}

export interface PresentationCell extends BaseCell {
  type: "presentation"
  data: PresentationData
}

export interface SourceCell extends BaseCell {
  type: "source"
  data: SourceData
}

export interface MarkdownCell extends BaseCell {
  type: "markdown"
  data: MarkdownData
}

export interface HTMLCell extends BaseCell {
  type: "html"
  data: HTMLData
}

export interface HeaderCell extends BaseCell {
  type: "header"
  data: HeaderData
}

export interface DividerCell extends BaseCell {
  type: "divider"
  data: DividerData
}

export interface ButtonCell extends BaseCell {
  type: "button"
  data: ButtonData
}

export interface AdmonitionCell extends BaseCell {
  type: "admonition"
  data: AdmonitionData
}

export interface TableCell extends BaseCell {
  type: "table"
  data: TableData
}

export interface ProjectCell extends BaseCell {
  type: "project"
  data: ProjectNodeData_Internal
}

export type Cell =
  | TextCell
  | QuizCell
  | CodeStudioCell
  | ImageCell
  | VideoCell
  | AudioCell
  | GalleryCell
  | YouTubeCell
  | SpotifyCell
  | MermaidCell
  | VegaLiteCell
  | PresentationCell
  | SourceCell
  | MarkdownCell
  | HTMLCell
  | HeaderCell
  | DividerCell
  | ButtonCell
  | AdmonitionCell
  | TableCell
  | ProjectCell

export interface CellularContent {
  cells: Cell[]
}

export function lexicalToCells(editorState: SerializedEditorState): CellularContent {
  const cells: Cell[] = []
  let cellId = 0

  function processNode(node: SerializedLexicalNode) {
    const id = `cell-${cellId++}`

    switch (node.type) {
      case "quiz":
        cells.push({ id, type: "quiz", data: (node as any).data })
        break
      case "code-studio":
        cells.push({ id, type: "code-studio", data: (node as any).data })
        break
      case "image":
        cells.push({ id, type: "image", data: (node as any).data })
        break
      case "video":
        cells.push({ id, type: "video", data: (node as any).data })
        break
      case "audio":
        cells.push({ id, type: "audio", data: (node as any).data })
        break
      case "gallery":
        cells.push({ id, type: "gallery", data: (node as any).data })
        break
      case "youtube":
        cells.push({ id, type: "youtube", data: (node as any).data })
        break
      case "spotify":
        cells.push({ id, type: "spotify", data: (node as any).data })
        break
      case "mermaid":
        cells.push({ id, type: "mermaid", data: (node as any).data })
        break
      case "vega-lite":
        cells.push({ id, type: "vega-lite", data: (node as any).data })
        break
      case "presentation":
        cells.push({ id, type: "presentation", data: (node as any).data })
        break
      case "source":
        cells.push({ id, type: "source", data: (node as any).data })
        break
      case "markdown":
        cells.push({ id, type: "markdown", data: (node as any).data })
        break
      case "html":
        cells.push({ id, type: "html", data: (node as any).data })
        break
      case "header":
        cells.push({ id, type: "header", data: (node as any).data })
        break
      case "divider":
        cells.push({ id, type: "divider", data: (node as any).data })
        break
      case "button":
        cells.push({ id, type: "button", data: (node as any).data })
        break
      case "admonition":
        cells.push({ id, type: "admonition", data: (node as any).data })
        break
      case "table":
        cells.push({ id, type: "table", data: (node as any).data })
        break
      case "project":
        cells.push({ id, type: "project", data: (node as any).data })
        break
      case "paragraph":
      case "heading":
      case "quote":
      case "list":
        cells.push({ 
          id, 
          type: node.type as "paragraph" | "heading" | "quote" | "list", 
          content: (node as any).children || [] 
        })
        break
      default:
        if ((node as any).children) {
          (node as any).children.forEach(processNode)
        }
    }
  }

  if (editorState.root?.children) {
    editorState.root.children.forEach(processNode)
  }

  return { cells }
}

export function cellsToLexical(content: CellularContent | any): SerializedEditorState {
  
  // Handle cells format
  if (!content || !content.cells) {
    // Return empty but valid Lexical state
    return {
      root: {
        type: "root",
        format: "",
        indent: 0,
        version: 1,
        children: [{
          type: "paragraph",
          children: [],
          direction: null,
          format: "",
          indent: 0,
          version: 1,
        } as any],
        direction: "ltr",
      },
    }
  }
  
  const children: SerializedLexicalNode[] = []

  for (const cell of content.cells) {
    switch (cell.type) {
      case "quiz":
        children.push({ type: "quiz", data: cell.data, version: 1 } as any)
        break
      case "code-studio":
        children.push({ type: "code-studio", data: cell.data, version: 1 } as any)
        break
      case "image":
        children.push({ type: "image", data: cell.data, version: 1 } as any)
        break
      case "video":
        children.push({ type: "video", data: cell.data, version: 1 } as any)
        break
      case "audio":
        children.push({ type: "audio", data: cell.data, version: 1 } as any)
        break
      case "gallery":
        children.push({ type: "gallery", data: cell.data, version: 1 } as any)
        break
      case "youtube":
        children.push({ type: "youtube", data: cell.data, version: 1 } as any)
        break
      case "spotify":
        children.push({ type: "spotify", data: cell.data, version: 1 } as any)
        break
      case "mermaid":
        children.push({ type: "mermaid", data: cell.data, version: 1 } as any)
        break
      case "vega-lite":
        children.push({ type: "vega-lite", data: cell.data, version: 1 } as any)
        break
      case "presentation":
        children.push({ type: "presentation", data: cell.data, version: 1 } as any)
        break
      case "source":
        children.push({ type: "source", data: cell.data, version: 1 } as any)
        break
      case "markdown":
        children.push({ type: "markdown", data: cell.data, version: 1 } as any)
        break
      case "html":
        children.push({ type: "html", data: cell.data, version: 1 } as any)
        break
      case "header":
        children.push({ type: "header", data: cell.data, version: 1 } as any)
        break
      case "divider":
        children.push({ type: "divider", data: cell.data, version: 1 } as any)
        break
      case "button":
        children.push({ type: "button", data: cell.data, version: 1 } as any)
        break
      case "admonition":
        children.push({ type: "admonition", data: cell.data, version: 1 } as any)
        break
      case "table":
        children.push({ type: "table", data: cell.data, version: 1 } as any)
        break
      case "project":
        children.push({ type: "project", data: cell.data, version: 1 } as any)
        break
      case "paragraph":
      case "heading":
      case "quote":
      case "list":
        children.push({
          type: cell.type,
          children: cell.content,
          version: 1,
        } as any)
        break
    }
  }

  // Lexical requires at least one child node - add empty paragraph if needed
  if (children.length === 0) {
    children.push({
      type: "paragraph",
      children: [],
      direction: null,
      format: "",
      indent: 0,
      version: 1,
    } as any)
  }

  return {
    root: {
      type: "root",
      format: "",
      indent: 0,
      version: 1,
      children,
      direction: "ltr",
    },
  }
}
