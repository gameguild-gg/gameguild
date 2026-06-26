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

import { lazy } from "react"
import { Loader2 } from "lucide-react"

import { PreviewImage } from "../plugins/preview-components/preview-image"
import { PreviewVideo } from "../plugins/preview-components/preview-video"
import { PreviewAudio } from "../plugins/preview-components/preview-audio"
import { PreviewGallery } from "../plugins/preview-components/preview-gallery"
import { PreviewHTML } from "../plugins/preview-components/preview-html"

import type { Block } from "../lib/storage/editor/block-structure"
import type { CodeStudioData } from "../extras/code-studio/types"
import type { MermaidData } from "../nodes/mermaid-node"
import type { SerializedImageNode } from "../nodes/image-node"
import type { SerializedVideoNode } from "../nodes/video-node"
import type { SerializedAudioNode } from "../nodes/audio-node"
import type { SerializedGalleryNode } from "../nodes/gallery-node"
import type { SerializedVegaLiteNode } from "../nodes/vega-lite-node"
import type { SerializedHTMLNode } from "../nodes/html-node"
import type { SerializedMarkdownNode } from "../nodes/markdown-node"

import type {
  EmbedPreviewProps,
  EmbeddableBlockConfig,
} from "./types"
import { ClientOnlyLazy } from "../lib/client-only-lazy"

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

const LazyPreviewCodeStudio = lazy(async () => ({
  default: (await import("../lazy-client-components")).CodeStudioEditor,
}))

const LazyPreviewMermaid = lazy(async () => ({
  default: (await import("../plugins/preview-components/preview-mermaid")).PreviewMermaid,
}))

const LazyPreviewVegaLite = lazy(async () => ({
  default: (await import("../plugins/preview-components/preview-vega-lite")).PreviewVegaLite,
}))

// Markdown is loaded lazily to avoid a static import cycle:
// preview-markdown → markdown-renderer → this registry.
const LazyPreviewMarkdown = lazy(async () => ({
  default: (await import("../plugins/preview-components/preview-markdown")).PreviewMarkdown,
}))

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

function PreviewCodeStudioAdapter({ block }: EmbedPreviewProps<"code-studio">) {
  return (
    <ClientOnlyLazy
      component={LazyPreviewCodeStudio}
      props={{ data: block.data, isPreview: true }}
      fallback={<PreviewSkeleton label="code studio" />}
    />
  )
}

function PreviewMermaidAdapter({ block }: EmbedPreviewProps<"mermaid">) {
  return (
    <ClientOnlyLazy
      component={LazyPreviewMermaid}
      props={{ data: block.data }}
      fallback={<PreviewSkeleton label="diagrama" />}
    />
  )
}

function PreviewVegaLiteAdapter({ block }: EmbedPreviewProps<"vega-lite">) {
  return (
    <ClientOnlyLazy
      component={LazyPreviewVegaLite}
      props={{ node: toSerialized(block) as SerializedVegaLiteNode }}
      fallback={<PreviewSkeleton label="gráfico" />}
    />
  )
}

function PreviewHTMLAdapter({ block }: EmbedPreviewProps<"html">) {
  return <PreviewHTML node={toSerialized(block) as SerializedHTMLNode} />
}

function PreviewMarkdownAdapter({ block }: EmbedPreviewProps<"markdown">) {
  return (
    <ClientOnlyLazy
      component={LazyPreviewMarkdown}
      props={{ node: toSerialized(block) as SerializedMarkdownNode }}
      fallback={<PreviewSkeleton label="markdown" />}
    />
  )
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
  "html": { Preview: PreviewHTMLAdapter },
  "markdown": { Preview: PreviewMarkdownAdapter },
}
