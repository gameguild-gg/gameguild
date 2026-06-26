"use client"

/**
 * Shared Markdown renderer.
 *
 * Single source of truth for how a markdown block is rendered, used by:
 *   - `preview-markdown.tsx` (read-only viewers / `block-array-viewer`)
 *   - `markdown-editor.tsx` (the live preview pane inside the editor)
 *   - `block-embed-registry.tsx` (when markdown is embedded in Lexical)
 *
 * Beyond GFM + raw HTML, this renderer understands one custom token:
 *
 *   <block-embed id="..." />
 *
 * which is resolved against the `embeds` map and rendered via the
 * embed registry — the same path Lexical uses for `BlockEmbedNode`.
 */

import { useMemo, useRef } from "react"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import rehypeRaw from "rehype-raw"

import { useMarkdownComponents } from "./markdown-components"
import { EMBEDDABLE_BLOCK_CONFIG } from "../../embed/block-embed-registry"
import { BlockEmbedView } from "../../embed/block-embed-view"
import type {
  EmbeddableBlock,
  EmbeddableBlockData,
  EmbeddableBlockType,
} from "../../embed/types"

// ---------------------------------------------------------------------------
// Embed token helpers (shared with the editor)
// ---------------------------------------------------------------------------

/**
 * Matches `<block-embed id="..." />` or `<block-embed id="..."></block-embed>`
 * with single or double quotes. Anchored to capture the id.
 */
export const BLOCK_EMBED_TOKEN_RE = /<block-embed\b[^>]*\bid=["']([^"']+)["'][^>]*\/?>(?:\s*<\/block-embed>)?/gi

export function extractEmbedIds(content: string): string[] {
  const ids: string[] = []
  for (const match of content.matchAll(BLOCK_EMBED_TOKEN_RE)) {
    if (match[1]) ids.push(match[1])
  }
  return ids
}

export function pruneUnusedEmbeds(
  content: string,
  embeds: Record<string, EmbeddableBlock> | undefined,
): Record<string, EmbeddableBlock> {
  if (!embeds) return {}
  const used = new Set(extractEmbedIds(content))
  const next: Record<string, EmbeddableBlock> = {}
  for (const [id, block] of Object.entries(embeds)) {
    if (used.has(id)) next[id] = block
  }
  return next
}

export function buildEmbedToken(id: string): string {
  return `<block-embed id="${id}"></block-embed>`
}

// ---------------------------------------------------------------------------
// Rehype plugin: lift <block-embed> out of any wrapping <p>
// ---------------------------------------------------------------------------

/**
 * Markdown wraps any inline-looking HTML in `<p>`. Our `<block-embed>` token
 * therefore lands inside a paragraph by default, and the registered Preview
 * component renders block-level content (divs, sections, iframes…), which
 * produces invalid HTML and React hydration errors.
 *
 * This rehype plugin walks the tree and splits any `<p>` that contains one
 * or more `<block-embed>` children into adjacent `<p>` segments around the
 * embeds, leaving each `<block-embed>` as a sibling at the block level.
 */
type HastNode = {
  type: string
  tagName?: string
  children?: HastNode[]
  [key: string]: unknown
}

function rehypeUnwrapBlockEmbed() {
  return (tree: HastNode) => {
    const walk = (node: HastNode) => {
      if (!Array.isArray(node.children)) return
      const next: HastNode[] = []
      for (const child of node.children) {
        if (
          child.type === "element" &&
          child.tagName === "p" &&
          Array.isArray(child.children) &&
          child.children.some(
            (c) => c.type === "element" && c.tagName === "block-embed",
          )
        ) {
          // Split: emit alternating <p> segments and bare <block-embed>s.
          let buffer: HastNode[] = []
          const flushParagraph = () => {
            if (buffer.length === 0) return
            next.push({ ...child, children: buffer })
            buffer = []
          }
          for (const c of child.children) {
            if (c.type === "element" && c.tagName === "block-embed") {
              flushParagraph()
              next.push(c)
            } else {
              buffer.push(c)
            }
          }
          flushParagraph()
        } else {
          walk(child)
          next.push(child)
        }
      }
      node.children = next
    }
    walk(tree)
  }
}

// ---------------------------------------------------------------------------
// Renderer
// ---------------------------------------------------------------------------

interface MarkdownRendererProps {
  content: string
  embeds?: Record<string, EmbeddableBlock>
  title?: string
  caption?: string
  /** Tailwind class for the outer wrapper. */
  className?: string
  /** Shown when `content` is empty. Pass `null` to hide. */
  emptyFallback?: React.ReactNode
  /**
   * When true, each embed renders inside `BlockEmbedView` — clicking the
   * pencil affordance opens the block's own editor (same UX as Lexical).
   * Requires `onEmbedChange` to persist edits.
   */
  editable?: boolean
  /** Called with the new data after the inline editor saves. */
  onEmbedChange?: (id: string, data: EmbeddableBlockData) => void
  /** Called when the user removes the embed via the inline UI. */
  onEmbedRemove?: (id: string) => void
}

const DEFAULT_EMPTY = (
  <p className="text-gray-400 dark:text-gray-600 italic">No markdown content</p>
)

export function MarkdownRenderer({
  content,
  embeds,
  title,
  caption,
  className = "my-4",
  emptyFallback = DEFAULT_EMPTY,
  editable = false,
  onEmbedChange,
  onEmbedRemove,
}: MarkdownRendererProps) {
  const baseComponents = useMarkdownComponents()

  // Keep latest values reachable from a STABLE EmbedTag component below.
  // If `EmbedTag` itself were re-created on every render, react-markdown
  // would see a new component type for `<block-embed>` and unmount /
  // remount every embed on each keystroke — which would close any open
  // inline editor modal.
  const embedsRef = useRef(embeds)
  const editableRef = useRef(editable)
  const onChangeRef = useRef(onEmbedChange)
  const onRemoveRef = useRef(onEmbedRemove)
  embedsRef.current = embeds
  editableRef.current = editable
  onChangeRef.current = onEmbedChange
  onRemoveRef.current = onEmbedRemove

  // Defined once per MarkdownRenderer instance — identity is stable across
  // re-renders so React preserves the underlying BlockEmbedView subtree.
  const EmbedTag = useMemo(() => {
    function EmbedTagImpl(props: { id?: string }) {
      const id = props.id
      if (!id) return null
      const block = embedsRef.current?.[id]
      if (!block) {
        return (
          <div className="my-4 rounded-md border border-dashed border-amber-300 bg-amber-50 dark:bg-amber-900/10 dark:border-amber-700 p-3 text-xs text-amber-700 dark:text-amber-300">
            Missing embed <code>{id}</code>
          </div>
        )
      }
      const entry = EMBEDDABLE_BLOCK_CONFIG[block.type as EmbeddableBlockType]
      if (!entry) {
        return (
          <div className="my-4 rounded-md border border-dashed border-red-300 bg-red-50 dark:bg-red-900/10 dark:border-red-700 p-3 text-xs text-red-700 dark:text-red-300">
            Block type &ldquo;{block.type}&rdquo; is not embeddable
          </div>
        )
      }
      if (editableRef.current) {
        return (
          <BlockEmbedView
            block={block}
            editable
            onChange={(data) => onChangeRef.current?.(id, data)}
            onRemove={onRemoveRef.current ? () => onRemoveRef.current?.(id) : undefined}
          />
        )
      }
      const Preview = entry.Preview as React.ComponentType<{ block: EmbeddableBlock }>
      return <Preview block={block} />
    }
    return EmbedTagImpl
  }, [])

  const components = useMemo(
    () =>
      ({
        ...baseComponents,
        "block-embed": EmbedTag,
      }) as typeof baseComponents,
    [baseComponents, EmbedTag],
  )

  return (
    <div className={className}>
      {title && (
        <h1 className="text-3xl font-bold mb-2 text-gray-900 dark:text-gray-100">{title}</h1>
      )}
      {caption && (
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">{caption}</p>
      )}
      {content ? (
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={[rehypeRaw, rehypeUnwrapBlockEmbed]}
          components={components}
        >
          {content}
        </ReactMarkdown>
      ) : (
        emptyFallback
      )}
    </div>
  )
}
