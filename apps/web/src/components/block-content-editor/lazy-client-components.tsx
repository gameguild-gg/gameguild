"use client"

import { lazy, type ComponentProps } from "react"
import { Loader2 } from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import type { CodeStudioData } from "@/components/block-content-editor/extras/code-studio/types"
import { ClientOnlyLazy } from "@/components/block-content-editor/lib/client-only-lazy"
import type { ModeSelectionDialog as ModeSelectionDialogComponent } from "@/components/block-content-editor/extras/code-studio/mode-selection-dialog"
import type { HTMLEditor as HTMLEditorComponent } from "@/components/block-content-editor/extras/html/html-editor"
import type { MarkdownEditor as MarkdownEditorComponent } from "@/components/block-content-editor/extras/markdown/markdown-editor"
import type { QuizEditorDialog as QuizSettingsDialogComponent } from "@game-guild/quiz-surface/editor"
import type { RichTextEditor as RichTextEditorComponent } from "@/components/block-content-editor/extras/rich-text/rich-text-editor"

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

type QuizSettingsDialogProps = ComponentProps<typeof QuizSettingsDialogComponent>
type ModeSelectionDialogProps = ComponentProps<typeof ModeSelectionDialogComponent>
type MarkdownEditorProps = ComponentProps<typeof MarkdownEditorComponent>
type HTMLEditorProps = ComponentProps<typeof HTMLEditorComponent>
type RichTextEditorProps = ComponentProps<typeof RichTextEditorComponent>

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

const LazyCodeStudioEditor = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/code-studio/code-studio-editor")).CodeStudioEditor,
}))

const LazyCodeStudioPreview = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/code-studio/code-studio-editor")).CodeStudioEditor,
}))

const LazyQuizSettingsDialog = lazy(async () => ({
  default: (await import("@game-guild/quiz-surface/editor")).QuizEditorDialog,
}))

const LazyModeSelectionDialog = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/code-studio/mode-selection-dialog")).ModeSelectionDialog,
}))

const LazyMarkdownEditor = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/markdown/markdown-editor")).MarkdownEditor,
}))

const LazyHTMLEditor = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/html/html-editor")).HTMLEditor,
}))

const LazyRichTextEditor = lazy(async () => ({
  default: (await import("@/components/block-content-editor/extras/rich-text/rich-text-editor")).RichTextEditor,
}))

export function CodeStudioEditor(props: CodeStudioEditorProps) {
  if (props.isPreview) {
    return <ClientOnlyLazy component={LazyCodeStudioPreview} props={props} fallback={<CodeStudioPreviewSkeleton />} />
  }

  return <ClientOnlyLazy component={LazyCodeStudioEditor} props={props} fallback={<EditorOverlaySkeleton title="Loading code studio..." />} />
}

export function QuizSettingsDialog(props: QuizSettingsDialogProps) {
  return <ClientOnlyLazy component={LazyQuizSettingsDialog} props={props} fallback={<EditorOverlaySkeleton title="Loading quiz editor..." compact={true} />} />
}

export function ModeSelectionDialog(props: ModeSelectionDialogProps) {
  return <ClientOnlyLazy component={LazyModeSelectionDialog} props={props} fallback={<EditorOverlaySkeleton title="Loading mode selector..." compact={true} />} />
}

export function MarkdownEditor(props: MarkdownEditorProps) {
  return <ClientOnlyLazy component={LazyMarkdownEditor} props={props} fallback={<EditorOverlaySkeleton title="Loading markdown editor..." />} />
}

export function HTMLEditor(props: HTMLEditorProps) {
  return <ClientOnlyLazy component={LazyHTMLEditor} props={props} fallback={<EditorOverlaySkeleton title="Loading HTML editor..." />} />
}

export function RichTextEditor(props: RichTextEditorProps) {
  return <ClientOnlyLazy component={LazyRichTextEditor} props={props} fallback={<EditorOverlaySkeleton title="Loading rich text editor..." />} />
}
