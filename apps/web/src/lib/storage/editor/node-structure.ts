import type { SerializedEditorState } from "lexical"
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

export type NodeType = 
  | "lexical"
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

export interface BaseNodeData {
  id: string
  type: NodeType
}

export interface LexicalNodeData extends BaseNodeData {
  type: "lexical"
  content: SerializedEditorState
}

export interface QuizNodeData extends BaseNodeData {
  type: "quiz"
  data: QuizData
}

export interface CodeStudioNodeData extends BaseNodeData {
  type: "code-studio"
  data: CodeStudioData
}

export interface ImageNodeData extends BaseNodeData {
  type: "image"
  data: ImageData
}

export interface VideoNodeData extends BaseNodeData {
  type: "video"
  data: VideoData
}

export interface AudioNodeData extends BaseNodeData {
  type: "audio"
  data: AudioData
}

export interface GalleryNodeData extends BaseNodeData {
  type: "gallery"
  data: GalleryData
}

export interface YouTubeNodeData extends BaseNodeData {
  type: "youtube"
  data: YouTubeData
}

export interface SpotifyNodeData extends BaseNodeData {
  type: "spotify"
  data: SpotifyData
}

export interface MermaidNodeData extends BaseNodeData {
  type: "mermaid"
  data: MermaidData
}

export interface VegaLiteNodeData extends BaseNodeData {
  type: "vega-lite"
  data: VegaLiteData
}

export interface PresentationNodeData extends BaseNodeData {
  type: "presentation"
  data: PresentationData
}

export interface SourceNodeData extends BaseNodeData {
  type: "source"
  data: SourceData
}

export interface MarkdownNodeData extends BaseNodeData {
  type: "markdown"
  data: MarkdownData
}

export interface HTMLNodeData extends BaseNodeData {
  type: "html"
  data: HTMLData
}

export interface HeaderNodeData extends BaseNodeData {
  type: "header"
  data: HeaderData
}

export interface DividerNodeData extends BaseNodeData {
  type: "divider"
  data: DividerData
}

export interface ButtonNodeData extends BaseNodeData {
  type: "button"
  data: ButtonData
}

export interface AdmonitionNodeData extends BaseNodeData {
  type: "admonition"
  data: AdmonitionData
}

export interface TableNodeData extends BaseNodeData {
  type: "table"
  data: TableData
}

export interface ProjectNodeData extends BaseNodeData {
  type: "project"
  data: ProjectNodeData_Internal
}

export type NodeData =
  | LexicalNodeData
  | QuizNodeData
  | CodeStudioNodeData
  | ImageNodeData
  | VideoNodeData
  | AudioNodeData
  | GalleryNodeData
  | YouTubeNodeData
  | SpotifyNodeData
  | MermaidNodeData
  | VegaLiteNodeData
  | PresentationNodeData
  | SourceNodeData
  | MarkdownNodeData
  | HTMLNodeData
  | HeaderNodeData
  | DividerNodeData
  | ButtonNodeData
  | AdmonitionNodeData
  | TableNodeData
  | ProjectNodeData

export interface NodeOrderItem {
  id: string
  children?: NodeOrderItem[]
}

export interface ProjectStructure {
  order: NodeOrderItem[]
  nodes: Record<string, NodeData>
}

