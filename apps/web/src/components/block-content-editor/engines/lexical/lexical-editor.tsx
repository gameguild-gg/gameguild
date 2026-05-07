"use client"

import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import { ListPlugin } from "@lexical/react/LexicalListPlugin"
import { MarkdownShortcutPlugin } from "@lexical/react/LexicalMarkdownShortcutPlugin"
import { TRANSFORMERS } from "@lexical/markdown"
import { HeadingNode, QuoteNode } from "@lexical/rich-text"
import { ListItemNode } from "@lexical/list"
import { CodeNode } from "@lexical/code"
import { TableCellNode, TableNode, TableRowNode } from "@lexical/table"
import { AutoLinkNode, LinkNode } from "@lexical/link"
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { AutoLinkPlugin } from "@lexical/react/LexicalAutoLinkPlugin"
import type { LinkMatcher } from "@lexical/react/LexicalAutoLinkPlugin"
import { HTMLNode } from "@/components/block-content-editor/nodes/html-node"
import { RichTextNode } from "@/components/block-content-editor/nodes/rich-text-node"

import { cn } from "@/lib/utils"
import { ImageNode } from "@/components/block-content-editor/nodes/image-node"
import { QuizNode } from "@/components/block-content-editor/nodes/quiz-node"
import { MarkdownNode } from "@/components/block-content-editor/nodes/markdown-node"
import { VideoNode } from "@/components/block-content-editor/nodes/video-node"
import { FloatingContentInsertPlugin } from "@/components/block-content-editor/plugins/floating-content-insert-plugin"
import { FloatingTextFormatToolbarPlugin } from "@/components/block-content-editor/plugins/floating-text-format-toolbar-plugin"
import { ImagePlugin } from "@/components/block-content-editor/plugins/image-plugin"
import { QuizPlugin } from "@/components/block-content-editor/plugins/quiz-plugin"
import { MarkdownPlugin } from "@/components/block-content-editor/plugins/markdown-plugin"
import { HTMLPlugin } from "@/components/block-content-editor/plugins/html-plugin"
import { RichTextPlugin as RichTextNodePlugin } from "@/components/block-content-editor/plugins/rich-text-plugin"
import { VideoPlugin } from "@/components/block-content-editor/plugins/video-plugin"
import { EditorToolbar } from "@/components/block-content-editor/editor-toolbar"
import { AudioNode } from "@/components/block-content-editor/nodes/audio-node"
import { AudioPlugin } from "@/components/block-content-editor/plugins/audio-plugin"
import { YouTubeAudioStyle } from "@/components/block-content-editor/youtube-audio-style"
// Adicione o import para o HeaderNode
import { HeaderNode } from "@/components/block-content-editor/nodes/header-node"

// Adicione o import para o HeaderPlugin
import { HeaderPlugin } from "@/components/block-content-editor/plugins/header-plugin"

import { DividerNode } from "@/components/block-content-editor/nodes/divider-node"
import { DividerPlugin } from "@/components/block-content-editor/plugins/divider-plugin"
import { CodePlugin } from "@/components/block-content-editor/plugins/code-plugin"

// Add these imports
import { ButtonNode } from "@/components/block-content-editor/nodes/button-node"
import { ButtonPlugin } from "@/components/block-content-editor/plugins/button-plugin"

// Add these imports
import { AdmonitionNode } from "@/components/block-content-editor/nodes/admonition-node"
import { AdmonitionPlugin } from "@/components/block-content-editor/plugins/admonition-plugin"

// Add these imports
import { GalleryNode } from "@/components/block-content-editor/nodes/gallery-node"
import { GalleryPlugin } from "@/components/block-content-editor/plugins/gallery-plugin"

// Add the import for the SourceNode and SourcePlugin:
import { SourceNode } from "@/components/block-content-editor/nodes/source-node"
import { SourcePlugin } from "@/components/block-content-editor/plugins/source-plugin"

// Add the import for the YouTubeNode and YouTubePlugin:
import { YouTubeNode } from "@/components/block-content-editor/nodes/youtube-node"
import { YouTubePlugin } from "@/components/block-content-editor/plugins/youtube-plugin"

// Add these imports
import { SpotifyNode } from "@/components/block-content-editor/nodes/spotify-node"
import { SpotifyPlugin } from "@/components/block-content-editor/plugins/spotify-plugin"

// Add the import for the CodeStudioNode and CodeStudioPlugin:
import { CodeStudioNode } from "@/components/block-content-editor/nodes/code-studio-node"
import { CodeStudioPlugin } from "@/components/block-content-editor/plugins/code-studio-plugin"

import { MermaidNode } from "@/components/block-content-editor/nodes/mermaid-node"
import { MermaidPlugin } from "@/components/block-content-editor/plugins/mermaid-plugin"
import { VegaLiteNode } from "@/components/block-content-editor/nodes/vega-lite-node"
import { VegaLitePlugin } from "@/components/block-content-editor/plugins/vega-lite-plugin"
import { CustomListNode } from "@/components/block-content-editor/nodes/custom-list-node"
import { TableNode as CustomTableNode } from "@/components/block-content-editor/nodes/table-node"
import { TablePlugin } from "@/components/block-content-editor/plugins/table-plugin"
import { ProjectNode } from "@/components/block-content-editor/nodes/project-node"
import { ProjectPlugin } from "@/components/block-content-editor/plugins/project-plugin"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $getSelection,
  $isRangeSelection,
  KEY_BACKSPACE_COMMAND,
  KEY_DELETE_COMMAND,
  COMMAND_PRIORITY_HIGH,
} from "lexical"
import { useEffect, createContext } from "react"
import { DeleteConfirmDialog } from "@/components/block-content-editor/extras/dialogs/delete-confirm-dialog"
import { useState } from "react"
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin"
import type { LexicalEditor } from "lexical"
import type React from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import { lexicalToCells, cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import type { CellularContent } from "@/lib/storage/editor/cell-structure"

// Create and export the EditorLoadingContext
export const EditorLoadingContext = createContext<boolean>(false)

// Create and export the ProjectIdContext
export const ProjectIdContext = createContext<string | null>(null)

// Create and export the StorageAdapterContext
export const StorageAdapterContext = createContext<any>(null)

// Export the provider component for convenience
export function EditorLoadingProvider({ children, value }: { children: React.ReactNode; value: boolean }) {
  return <EditorLoadingContext.Provider value={value}>{children}</EditorLoadingContext.Provider>
}

// Export the ProjectId provider component
export function ProjectIdProvider({ children, value }: { children: React.ReactNode; value: string | null }) {
  return <ProjectIdContext.Provider value={value}>{children}</ProjectIdContext.Provider>
}

// Export the StorageAdapter provider component
export function StorageAdapterProvider({ children, value }: { children: React.ReactNode; value: any }) {
  return <StorageAdapterContext.Provider value={value}>{children}</StorageAdapterContext.Provider>
}

function StructureDeleteConfirmPlugin() {
  const [editor] = useLexicalComposerContext()
  const [showConfirm, setShowConfirm] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<() => void>(() => () => {})

  useEffect(() => {
    const handleDelete = (event: KeyboardEvent) => {
      const selection = $getSelection()
      if (!$isRangeSelection(selection)) return false

      const nodes = selection.getNodes()
      const hasStructuralNodes = nodes.some(
        (node) =>
          node.getType() === "image" ||
          node.getType() === "video" ||
          node.getType() === "audio" ||
          node.getType() === "quiz" ||
          node.getType() === "gallery" ||
          node.getType() === "presentation" ||
          node.getType() === "source" ||
          node.getType() === "youtube" ||
          node.getType() === "spotify" ||
          node.getType() === "source-code" ||
          node.getType() === "code-studio" ||
          node.getType() === "button" ||
          node.getType() === "admonition" ||
          node.getType() === "divider" ||
          node.getType() === "header" ||
          node.getType() === "mermaid" ||
          node.getType() === "vega-lite" ||
          node.getType() === "table",
      )

      if (hasStructuralNodes) {
        event.preventDefault()
        setPendingDelete(() => () => {
          editor.update(() => {
            nodes.forEach((node) => {
              if (
                node.getType() === "image" ||
                node.getType() === "video" ||
                node.getType() === "audio" ||
                node.getType() === "quiz" ||
                node.getType() === "gallery" ||
                node.getType() === "presentation" ||
                node.getType() === "source" ||
                node.getType() === "youtube" ||
                node.getType() === "spotify" ||
                node.getType() === "source-code" ||
                node.getType() === "code-studio" ||
                node.getType() === "button" ||
                node.getType() === "admonition" ||
                node.getType() === "divider" ||
                node.getType() === "header" ||
                node.getType() === "mermaid" ||
                node.getType() === "vega-lite"
              ) {
                node.remove()
              }
            })
          })
        })
        setShowConfirm(true)
        return true
      }
      return false
    }

    return editor.registerCommand(KEY_BACKSPACE_COMMAND, handleDelete, COMMAND_PRIORITY_HIGH)
  }, [editor])

  useEffect(() => {
    return editor.registerCommand(
      KEY_DELETE_COMMAND,
      (event: KeyboardEvent) => {
        const selection = $getSelection()
        if (!$isRangeSelection(selection)) return false

        const nodes = selection.getNodes()
        const hasStructuralNodes = nodes.some(
          (node) =>
            node.getType() === "image" ||
            node.getType() === "video" ||
            node.getType() === "audio" ||
            node.getType() === "quiz" ||
            node.getType() === "gallery" ||
            node.getType() === "presentation" ||
            node.getType() === "source" ||
            node.getType() === "youtube" ||
            node.getType() === "spotify" ||
            node.getType() === "source-code" ||
            node.getType() === "code-studio" ||
            node.getType() === "button" ||
            node.getType() === "admonition" ||
            node.getType() === "divider" ||
            node.getType() === "header" ||
            node.getType() === "mermaid" ||
            node.getType() === "vega-lite"
        )

        if (hasStructuralNodes) {
          event.preventDefault()
          setPendingDelete(() => () => {
            editor.update(() => {
              nodes.forEach((node) => {
                if (
                  node.getType() === "image" ||
                  node.getType() === "video" ||
                  node.getType() === "audio" ||
                  node.getType() === "quiz" ||
                  node.getType() === "gallery" ||
                  node.getType() === "presentation" ||
                  node.getType() === "source" ||
                  node.getType() === "youtube" ||
                  node.getType() === "spotify" ||
                  node.getType() === "source-code" ||
                  node.getType() === "code-studio" ||
                  node.getType() === "button" ||
                  node.getType() === "admonition" ||
                  node.getType() === "divider" ||
                  node.getType() === "header" ||
                  node.getType() === "mermaid" ||
                  node.getType() === "vega-lite" ||
                  node.getType() === "table"
                ) {
                  node.remove()
                }
              })
            })
          })
          setShowConfirm(true)
          return true
        }
        return false
      },
      COMMAND_PRIORITY_HIGH,
    )
  }, [editor])

  return (
    <DeleteConfirmDialog
      open={showConfirm}
      onOpenChange={setShowConfirm}
      title="Confirm Deletion"
      itemName="structural element"
      itemType="element"
      onConfirm={() => {
        pendingDelete()
        setShowConfirm(false)
      }}
      description="Are you sure you want to delete this structural element? This action cannot be undone."
    />
  )
}

// Update the initialConfig nodes array to include ButtonNode
const initialConfig = {
  namespace: "GameGuildEditor",
  editorState: undefined,
  nodes: [
    HeadingNode,
    QuoteNode,
    ListItemNode,
    // ListNode,  // Substituído pelo CustomListNode
    CustomListNode,
    // Remove CodeHighlightNode to avoid the prismjs dependency
    CodeNode,
    TableCellNode,
    TableNode,
    TableRowNode,
    AutoLinkNode,
    LinkNode,
    ImageNode,
    QuizNode,
    MarkdownNode,
    HTMLNode,
    RichTextNode,
    VideoNode,
    AudioNode,
    HeaderNode,
    DividerNode,
    ButtonNode,
    AdmonitionNode,
    GalleryNode,
    SourceNode,
    YouTubeNode,
    SpotifyNode,
    CodeStudioNode,
    MermaidNode,
    VegaLiteNode,
    CustomTableNode,
    ProjectNode,
  ],
  theme: {
    text: {
      bold: "font-bold",
      italic: "italic",
      underline: "underline",
      strikethrough: "line-through",
    },
    paragraph: "my-2",
    heading: {
      h1: "text-3xl font-bold",
      h2: "text-2xl font-bold",
      h3: "text-xl font-bold",
      h4: "text-lg font-bold",
      h5: "text-base font-bold",
    },
    list: {
      ul: "list-disc list-inside",
      ol: "list-decimal list-inside",
      listitem: "my-1",
    },
    quote: "border-l-4 border-muted pl-4 italic",
    code: "bg-muted p-1 rounded font-mono text-sm",
    link: "text-blue-600 underline hover:text-blue-800 cursor-pointer",
  },
  onError: (error: Error) => {
    console.error(error)
  },
}

// Add the ButtonPlugin to the Editor component
interface EditorProps {
  className?: string
  initialState?: string
  onChange?: (state: string) => void
  editorRef?: React.MutableRefObject<LexicalEditor | null>
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId?: string | null
  mode?: ProjectMode
  blockId?: string
  panelId?: string
  customRestrictions?: any
  storageAdapter?: any
  currentStorageType?: "local" | "gameguild-cloud" | "google-drive"
  readOnly?: boolean
}

// Criar um plugin para gerenciar a referência do editor:
function EditorRefPlugin({ editorRef }: { editorRef?: React.MutableRefObject<LexicalEditor | null> }) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (editorRef) {
      editorRef.current = editor
    }
    return () => {
      if (editorRef) {
        editorRef.current = null
      }
    }
  }, [editor, editorRef])

  return null
}

// Configure URL matchers for AutoLinkPlugin
const URL_MATCHER =
  /((https?:\/\/(www\.)?)|(www\.))[-a-zA-Z0-9@:%._+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_+.~#?&//=]*)/

const EMAIL_MATCHER =
  /(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))/

const MATCHERS: LinkMatcher[] = [
  (text: string) => {
    const match = URL_MATCHER.exec(text)
    if (match === null) {
      return null
    }
    const fullMatch = match[0]
    return {
      index: match.index,
      length: fullMatch.length,
      text: fullMatch,
      url: fullMatch.startsWith('http') ? fullMatch : `https://${fullMatch}`,
    }
  },
  (text: string) => {
    const match = EMAIL_MATCHER.exec(text)
    if (match === null) {
      return null
    }
    const fullMatch = match[0]
    return {
      index: match.index,
      length: fullMatch.length,
      text: fullMatch,
      url: `mailto:${fullMatch}`,
    }
  },
]

// Atualizar a função Editor para incluir o EditorRefPlugin:
export function Editor({ 
  className, 
  initialState, 
  onChange, 
  editorRef, 
  onLoadingChange, 
  projectId, 
  mode, 
  blockId, 
  panelId, 
  customRestrictions, 
  storageAdapter, 
  currentStorageType,
  readOnly = false
}: EditorProps) {
  const [isLoadingProject, setIsLoadingProject] = useState(false)

  useEffect(() => {
    if (onLoadingChange) {
      onLoadingChange(setIsLoadingProject)
    }
  }, [onLoadingChange])

  // Cells as native format - convert to Lexical only for internal UI
  const lexicalInitialState = initialState
    ? (() => {
        try {
          const parsed = JSON.parse(initialState)
          // Always expect cells format
          return JSON.stringify(cellsToLexical(parsed))
        } catch {
          // If parsing fails, create empty cells structure
          return JSON.stringify(cellsToLexical([]))
        }
      })()
    : JSON.stringify(cellsToLexical([]))

  return (
    <LexicalComposer
      initialConfig={{
        ...initialConfig,
        editorState: lexicalInitialState,
        editable: !readOnly,
      }}
    >
      <StorageAdapterProvider value={storageAdapter}>
        <ProjectIdProvider value={projectId || null}>
          <EditorLoadingProvider value={isLoadingProject}>
        <div className={cn("rounded-lg border-2 border-gray-300 dark:border-gray-700", className)}>
          
          {/*<EditorToolbar />*/}
          <div className="relative">
            <RichTextPlugin
              contentEditable={<ContentEditable className="min-h-[450px] p-3 outline-none text-gray-900 dark:text-gray-100" />}
              placeholder={
                <div className="pointer-events-none absolute left-[13px] top-[13px] select-none text-gray-400 dark:text-gray-500">
                  {readOnly ? "Viewing historical version (read-only)" : "Start typing..."}
                </div>
              }
              ErrorBoundary={LexicalErrorBoundary}
            />
            {!readOnly && (
              <FloatingContentInsertPlugin 
                mode={mode} 
                blockId={blockId}
                panelId={panelId}
                customRestrictions={customRestrictions}
                currentProjectId={projectId || undefined}
                storageAdapter={storageAdapter}
                currentStorageType={currentStorageType}
              />
            )}
            {!readOnly && <FloatingTextFormatToolbarPlugin />}
            <LinkPlugin />
            <AutoLinkPlugin matchers={MATCHERS} />
            <ImagePlugin />
            <QuizPlugin />
            <MarkdownPlugin />
            <HTMLPlugin />
            <RichTextNodePlugin />
            <VideoPlugin />
            <AudioPlugin />
            <HeaderPlugin />
            <DividerPlugin />
            <ButtonPlugin />
            <AdmonitionPlugin />
            <GalleryPlugin />
            <SourcePlugin />
            <YouTubePlugin />
            <SpotifyPlugin />
            <CodePlugin />
            <CodeStudioPlugin />
            <MermaidPlugin />
            <VegaLitePlugin />
            <TablePlugin />
            <ProjectPlugin />
            <OnChangePlugin
              onChange={(editorState) => {
                if (onChange) {
                  const cellContent = lexicalToCells(editorState.toJSON())
                  onChange(JSON.stringify(cellContent))
                }
              }}
            />
            <EditorRefPlugin editorRef={editorRef} />
            <StructureDeleteConfirmPlugin />
            <ListPlugin />
            <MarkdownShortcutPlugin transformers={TRANSFORMERS} />
          </div>
        </div>
        </EditorLoadingProvider>
        </ProjectIdProvider>
      </StorageAdapterProvider>
    </LexicalComposer>
  )
}
