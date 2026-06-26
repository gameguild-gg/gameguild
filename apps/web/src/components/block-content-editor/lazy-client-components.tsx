"use client"

import dynamic from "next/dynamic"
import { Loader2 } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import type { CodeStudioData } from "@/components/block-content-editor/extras/code-studio/types"

// Browser-only editor surfaces used by nodes and engine modals.
// Lexical node classes themselves must stay synchronously importable.

interface CodeStudioEditorProps {
  data: CodeStudioData
  isPreview?: boolean
  onUpdate?: (data: Partial<CodeStudioData>) => void
  onSave?: (data: CodeStudioData) => void
  onCancel?: () => void
  onEdit?: () => void
  projectId?: string
}

function EditorOverlaySkeleton({ title, compact = false }: { title: string; compact?: boolean }) {
  // z-[100] sits above Radix Dialog overlays (which default to z-50),
  // ensuring this loader is visible even if the block-type picker
  // dialog is mid-close while the lazy chunk downloads.
  return (
    <div className="fixed inset-0 z-100 flex items-center justify-center bg-background/80 p-4 backdrop-blur-sm">
      <div className={compact ? "w-full max-w-xl rounded-2xl border bg-background/95 shadow-2xl" : "w-full max-w-6xl rounded-2xl border bg-background/95 shadow-2xl"}>
        <div className="flex items-center gap-3 border-b p-5 sm:p-6">
          <Loader2 className="h-5 w-5 shrink-0 animate-spin text-primary" />
          <div className="flex-1 min-w-0">
            <p className="text-base font-medium text-foreground">{title}</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Carregando módulo do editor… (pode levar alguns segundos na primeira vez)
            </p>
          </div>
        </div>

        {compact ? (
          <div className="space-y-4 p-5 sm:p-6">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-32 w-full" />
            <div className="flex justify-end gap-3">
              <Skeleton className="h-10 w-24" />
              <Skeleton className="h-10 w-32" />
            </div>
          </div>
        ) : (
          <div className="grid min-h-[70vh] grid-cols-1 lg:grid-cols-[240px_1fr]">
            <div className="space-y-3 border-b p-5 lg:border-b-0 lg:border-r lg:p-6">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-8 w-3/4" />
              <Skeleton className="h-8 w-5/6" />
              <Skeleton className="h-8 w-2/3" />
            </div>
            <div className="space-y-4 p-5 lg:p-6">
              <div className="flex gap-2">
                <Skeleton className="h-9 w-28" />
                <Skeleton className="h-9 w-24" />
                <Skeleton className="h-9 w-20" />
              </div>
              <Skeleton className="h-[50vh] w-full" />
              <Skeleton className="h-24 w-full" />
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

function CodeStudioPreviewSkeleton() {
  return (
    <div className="my-4 rounded-xl border bg-card p-4 shadow-sm">
      <div className="flex items-center justify-between gap-3 border-b pb-3">
        <div className="space-y-2">
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-4 w-48 max-w-full" />
        </div>
        <Skeleton className="h-9 w-24" />
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-[220px_1fr]">
        <div className="space-y-2 rounded-lg border p-3">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-5/6" />
          <Skeleton className="h-8 w-3/4" />
        </div>

        <div className="space-y-3">
          <div className="flex gap-2">
            <Skeleton className="h-8 w-28" />
            <Skeleton className="h-8 w-24" />
          </div>
          <Skeleton className="h-[22rem] w-full" />
        </div>
      </div>
    </div>
  )
}

const LazyCodeStudioEditor = dynamic<CodeStudioEditorProps>(
  () => import("@/components/block-content-editor/extras/code-studio/code-studio-editor").then((mod) => mod.CodeStudioEditor),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading code studio..." />,
  },
)

const LazyCodeStudioPreview = dynamic<CodeStudioEditorProps>(
  () => import("@/components/block-content-editor/extras/code-studio/code-studio-editor").then((mod) => mod.CodeStudioEditor),
  {
    ssr: false,
    loading: () => <CodeStudioPreviewSkeleton />,
  },
)

export function CodeStudioEditor(props: CodeStudioEditorProps) {
  if (props.isPreview) {
    return <LazyCodeStudioPreview {...props} />
  }

  return <LazyCodeStudioEditor {...props} />
}

export const QuizSettingsDialog = dynamic(
  () => import("@/components/block-content-editor/extras/quiz/quiz-settings-dialog").then((mod) => ({ default: mod.QuizSettingsDialog })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading quiz editor..." compact={true} />,
  },
)

export const ModeSelectionDialog = dynamic(
  () => import("@/components/block-content-editor/extras/code-studio/mode-selection-dialog").then((mod) => ({ default: mod.ModeSelectionDialog })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading mode selector..." compact={true} />,
  },
)

export const MarkdownEditor = dynamic(
  () => import("@/components/block-content-editor/extras/markdown/markdown-editor").then((mod) => ({ default: mod.MarkdownEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading markdown editor..." />,
  },
)

export const MermaidEditor = dynamic(
  () => import("@/components/block-content-editor/extras/mermaid/mermaid-editor").then((mod) => ({ default: mod.MermaidEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading diagram editor..." />,
  },
)

export const VegaLiteEditor = dynamic(
  () => import("@/components/block-content-editor/extras/vega-lite/vega-lite-editor").then((mod) => ({ default: mod.VegaLiteEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading chart editor..." />,
  },
)

export const UnifiedMediaEditor = dynamic(
  () => import("@/components/block-content-editor/extras/media/unified-media-editor").then((mod) => ({ default: mod.UnifiedMediaEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading media editor..." />,
  },
)

export const HTMLEditor = dynamic(
  () => import("@/components/block-content-editor/extras/html/html-editor").then((mod) => ({ default: mod.HTMLEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading HTML editor..." />,
  },
)

export const RichTextEditor = dynamic(
  () => import("@/components/block-content-editor/extras/rich-text/rich-text-editor").then((mod) => ({ default: mod.RichTextEditor })),
  {
    ssr: false,
    loading: () => <EditorOverlaySkeleton title="Loading rich text editor..." />,
  },
)