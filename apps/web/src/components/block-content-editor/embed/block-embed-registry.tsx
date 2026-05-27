"use client"

/**
 * Surface-agnostic registry of preview renderers for embeddable blocks.
 *
 * Each entry takes a `Block` envelope and renders the matching `preview-*`
 * component already used elsewhere in the editor. Heavy previews are
 * loaded lazily so embedding a `divider` never pulls in mermaid/vega/code
 * bundles.
 *
 * MUST NOT import from `lexical`, `@lexical/*`, or `../plugins/` /
 * `../nodes/` (except `nodes/base/serialized-*` types).
 */

import dynamic from "next/dynamic"
import { Loader2 } from "lucide-react"

import { PreviewImage } from "../plugins/preview-components/preview-image"
import { PreviewVideo } from "../plugins/preview-components/preview-video"
import { PreviewAudio } from "../plugins/preview-components/preview-audio"
import { PreviewGallery } from "../plugins/preview-components/preview-gallery"
import { PreviewAdmonition } from "../plugins/preview-components/preview-admonition"
import { PreviewDivider } from "../plugins/preview-components/preview-divider"
import { PreviewButton } from "../plugins/preview-components/preview-button"
import { PreviewHTML } from "../plugins/preview-components/preview-html"

import type { Block } from "../lib/storage/editor/block-structure"
import type { CodeStudioData } from "../extras/code-studio/types"
import type { MermaidData } from "../nodes/mermaid-node"
import type { SerializedImageNode } from "../nodes/image-node"
import type { SerializedVideoNode } from "../nodes/video-node"
import type { SerializedAudioNode } from "../nodes/audio-node"
import type { SerializedGalleryNode } from "../nodes/gallery-node"
import type { SerializedAdmonitionNode } from "../nodes/admonition-node"
import type { SerializedDividerNode } from "../nodes/divider-node"
import type { SerializedButtonNode } from "../nodes/button-node"
import type { SerializedVegaLiteNode } from "../nodes/vega-lite-node"
import type { SerializedHTMLNode } from "../nodes/html-node"

import type {
  EmbedPreviewProps,
  EmbeddableBlockConfig,
} from "./types"

// ---------------------------------------------------------------------------
// Lazy previews for heavy bundles
// ---------------------------------------------------------------------------

function PreviewSkeleton({ label }: { label: string }) {
  return (
    <div className="my-4 flex items-center gap-2 rounded-lg border border-dashed bg-muted/30 p-4 text-sm text-muted-foreground">
      <Loader2 className="h-4 w-4 animate-spin" />
      <span>Carregando {label}…</span>
    </div>
  )
}

const LazyPreviewCodeStudio = dynamic<{ data: CodeStudioData; isPreview: true }>(
  () => import("../lazy-client-components").then((m) => ({ default: m.CodeStudioEditor })),
  { ssr: false, loading: () => <PreviewSkeleton label="code studio" /> },
)

const LazyPreviewMermaid = dynamic<{ data: MermaidData }>(
  () => import("../plugins/preview-components/preview-mermaid").then((m) => ({ default: m.PreviewMermaid })),
  { ssr: false, loading: () => <PreviewSkeleton label="diagrama" /> },
)

const LazyPreviewVegaLite = dynamic<{ node: SerializedVegaLiteNode }>(
  () => import("../plugins/preview-components/preview-vega-lite").then((m) => ({ default: m.PreviewVegaLite })),
  { ssr: false, loading: () => <PreviewSkeleton label="gráfico" /> },
)

// ---------------------------------------------------------------------------
// Adapters: Block envelope → preview component props
// ---------------------------------------------------------------------------

function toSerialized<T extends string, D>(block: Block & { type: T; data: D }) {
  return { type: block.type, version: 1, data: block.data }
}

function PreviewImageAdapter({ block }: EmbedPreviewProps<"image">) {
  return <PreviewImage node={toSerialized(block) as SerializedImageNode} />
}

function PreviewVideoAdapter({ block }: EmbedPreviewProps<"video">) {
  return <PreviewVideo node={toSerialized(block) as SerializedVideoNode} />
}

function PreviewAudioAdapter({ block }: EmbedPreviewProps<"audio">) {
  return <PreviewAudio node={toSerialized(block) as SerializedAudioNode} />
}

function PreviewGalleryAdapter({ block }: EmbedPreviewProps<"gallery">) {
  return <PreviewGallery node={toSerialized(block) as SerializedGalleryNode} />
}

function PreviewAdmonitionAdapter({ block }: EmbedPreviewProps<"admonition">) {
  return <PreviewAdmonition node={toSerialized(block) as SerializedAdmonitionNode} />
}

function PreviewDividerAdapter({ block }: EmbedPreviewProps<"divider">) {
  return <PreviewDivider node={toSerialized(block) as SerializedDividerNode} />
}

function PreviewButtonAdapter({ block }: EmbedPreviewProps<"button">) {
  return <PreviewButton node={toSerialized(block) as SerializedButtonNode} />
}

function PreviewCodeStudioAdapter({ block }: EmbedPreviewProps<"code-studio">) {
  return <LazyPreviewCodeStudio data={block.data} isPreview />
}

function PreviewMermaidAdapter({ block }: EmbedPreviewProps<"mermaid">) {
  return <LazyPreviewMermaid data={block.data} />
}

function PreviewVegaLiteAdapter({ block }: EmbedPreviewProps<"vega-lite">) {
  return <LazyPreviewVegaLite node={toSerialized(block) as SerializedVegaLiteNode} />
}

function PreviewHTMLAdapter({ block }: EmbedPreviewProps<"html">) {
  return <PreviewHTML node={toSerialized(block) as SerializedHTMLNode} />
}

// ---------------------------------------------------------------------------
// Public registry
// ---------------------------------------------------------------------------

export const EMBEDDABLE_BLOCK_CONFIG: EmbeddableBlockConfig = {
  "image": { Preview: PreviewImageAdapter },
  "video": { Preview: PreviewVideoAdapter },
  "audio": { Preview: PreviewAudioAdapter },
  "gallery": { Preview: PreviewGalleryAdapter },
  "code-studio": { Preview: PreviewCodeStudioAdapter },
  "mermaid": { Preview: PreviewMermaidAdapter },
  "vega-lite": { Preview: PreviewVegaLiteAdapter },
  "admonition": { Preview: PreviewAdmonitionAdapter },
  "divider": { Preview: PreviewDividerAdapter },
  "button": { Preview: PreviewButtonAdapter },
  "html": { Preview: PreviewHTMLAdapter },
}
