'use client';

import { LexicalComposer } from '@lexical/react/LexicalComposer';
import { ContentEditable } from '@lexical/react/LexicalContentEditable';
import { HistoryPlugin } from '@lexical/react/LexicalHistoryPlugin';
import { RichTextPlugin } from '@lexical/react/LexicalRichTextPlugin';
import { LexicalErrorBoundary } from '@lexical/react/LexicalErrorBoundary';
import { ListPlugin } from '@lexical/react/LexicalListPlugin';
import { MarkdownShortcutPlugin } from '@lexical/react/LexicalMarkdownShortcutPlugin';
import { TRANSFORMERS } from '@lexical/markdown';
import { HeadingNode, QuoteNode } from '@lexical/rich-text';
import { ListItemNode, ListNode } from '@lexical/list';
import { CodeNode } from '@lexical/code';
import { TableCellNode, TableNode, TableRowNode } from '@lexical/table';
import { AutoLinkNode, LinkNode } from '@lexical/link';
import { HTMLNode } from './nodes/html-node';

import { cn } from '@/lib/utils';
import { ImageNode } from './nodes/image-node';
import { QuizNode } from './nodes/quiz-node';
import { MarkdownNode } from './nodes/markdown-node';
import { VideoNode } from './nodes/video-node';
import { FloatingContentInsertPlugin } from './plugins/floating-content-insert-plugin';
import { FloatingTextFormatToolbarPlugin } from './plugins/floating-text-format-toolbar-plugin';
import { ImagePlugin } from './plugins/image-plugin';
import { QuizPlugin } from './plugins/quiz-plugin';
import { MarkdownPlugin } from './plugins/markdown-plugin';
import { HTMLPlugin } from './plugins/html-plugin';
import { VideoPlugin } from './plugins/video-plugin';
import { EditorToolbar } from './editor-toolbar';
import { AudioNode } from './nodes/audio-node';
import { AudioPlugin } from './plugins/audio-plugin';
import { YouTubeAudioStyle } from './youtube-audio-style';
// Adicione o import para o HeaderNode
import { HeaderNode } from './nodes/header-node';

// Adicione o import para o HeaderPlugin
import { HeaderPlugin } from './plugins/header-plugin';

import { DividerNode } from './nodes/divider-node';
import { DividerPlugin } from './plugins/divider-plugin';
import { CodePlugin } from './plugins/code-plugin';

// Add these imports
import { ButtonNode } from './nodes/button-node';
import { ButtonPlugin } from './plugins/button-plugin';

// Add these imports
import { CalloutNode } from './nodes/callout-node';
import { CalloutPlugin } from './plugins/callout-plugin';

// Add these imports
import { GalleryNode } from './nodes/gallery-node';
import { GalleryPlugin } from './plugins/gallery-plugin';

// Add the import for the PresentationNode:
import { PresentationNode } from './nodes/presentation-node';

// Add the import for the PresentationPlugin:
import { PresentationPlugin } from './plugins/presentation-plugin';

// Add the import for the SourceNode and SourcePlugin:
import { SourceNode } from './nodes/source-node';
import { SourcePlugin } from './plugins/source-plugin';

// Add the import for the YouTubeNode and YouTubePlugin:
import { YouTubeNode } from './nodes/youtube-node';
import { YouTubePlugin } from './plugins/youtube-plugin';

// Add these imports
import { SpotifyNode } from './nodes/spotify-node';
import { SpotifyPlugin } from './plugins/spotify-plugin';

// Add the import for the SourceCodeNode and SourceCodePlugin:
import { SourceCodeNode } from './nodes/source-code-node';
import { SourceCodePlugin } from './plugins/source-code-plugin';

import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext';
import type { LexicalEditor } from 'lexical';
import { $getSelection, $isRangeSelection, COMMAND_PRIORITY_HIGH, KEY_BACKSPACE_COMMAND, KEY_DELETE_COMMAND } from 'lexical';
import type React from 'react';
import { createContext, useEffect, useState } from 'react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { OnChangePlugin } from '@lexical/react/LexicalOnChangePlugin';

// Create and export the EditorLoadingContext
export const EditorLoadingContext = createContext<boolean>(false);

// Export the provider component for convenience
export function EditorLoadingProvider({ children, value }: { children: React.ReactNode; value: boolean }) {
  return <EditorLoadingContext.Provider value={value}>{children}</EditorLoadingContext.Provider>;
}

function StructureDeleteConfirmPlugin() {
  const [editor] = useLexicalComposerContext();
  const [showConfirm, setShowConfirm] = useState(false);
  const [pendingDelete, setPendingDelete] = useState<() => void>(() => () => {});

  useEffect(() => {
    const handleDelete = (event: KeyboardEvent) => {
      const selection = $getSelection();
      if (!$isRangeSelection(selection)) return false;

      const nodes = selection.getNodes();
      const hasStructuralNodes = nodes.some(
        (node) =>
          node.getType() === 'image' ||
          node.getType() === 'video' ||
          node.getType() === 'audio' ||
          node.getType() === 'quiz' ||
          node.getType() === 'gallery' ||
          node.getType() === 'presentation' ||
          node.getType() === 'source' ||
          node.getType() === 'youtube' ||
          node.getType() === 'spotify' ||
          node.getType() === 'source-code' ||
          node.getType() === 'button' ||
          node.getType() === 'callout' ||
          node.getType() === 'divider' ||
          node.getType() === 'header',
      );

      if (hasStructuralNodes) {
        event.preventDefault();
        setPendingDelete(() => () => {
          editor.update(() => {
            nodes.forEach((node) => {
              if (
                node.getType() === 'image' ||
                node.getType() === 'video' ||
                node.getType() === 'audio' ||
                node.getType() === 'quiz' ||
                node.getType() === 'gallery' ||
                node.getType() === 'presentation' ||
                node.getType() === 'source' ||
                node.getType() === 'youtube' ||
                node.getType() === 'spotify' ||
                node.getType() === 'source-code' ||
                node.getType() === 'button' ||
                node.getType() === 'callout' ||
                node.getType() === 'divider' ||
                node.getType() === 'header'
              ) {
                node.remove();
              }
            });
          });
        });
        setShowConfirm(true);
        return true;
      }
      return false;
    };

    return editor.registerCommand(KEY_BACKSPACE_COMMAND, handleDelete, COMMAND_PRIORITY_HIGH);
  }, [editor]);

  useEffect(() => {
    return editor.registerCommand(
      KEY_DELETE_COMMAND,
      (event: KeyboardEvent) => {
        const selection = $getSelection();
        if (!$isRangeSelection(selection)) return false;

        const nodes = selection.getNodes();
        const hasStructuralNodes = nodes.some(
          (node) =>
            node.getType() === 'image' ||
            node.getType() === 'video' ||
            node.getType() === 'audio' ||
            node.getType() === 'quiz' ||
            node.getType() === 'gallery' ||
            node.getType() === 'presentation' ||
            node.getType() === 'source' ||
            node.getType() === 'youtube' ||
            node.getType() === 'spotify' ||
            node.getType() === 'source-code' ||
            node.getType() === 'button' ||
            node.getType() === 'callout' ||
            node.getType() === 'divider' ||
            node.getType() === 'header',
        );

        if (hasStructuralNodes) {
          event.preventDefault();
          setPendingDelete(() => () => {
            editor.update(() => {
              nodes.forEach((node) => {
                if (
                  node.getType() === 'image' ||
                  node.getType() === 'video' ||
                  node.getType() === 'audio' ||
                  node.getType() === 'quiz' ||
                  node.getType() === 'gallery' ||
                  node.getType() === 'presentation' ||
                  node.getType() === 'source' ||
                  node.getType() === 'youtube' ||
                  node.getType() === 'spotify' ||
                  node.getType() === 'source-code' ||
                  node.getType() === 'button' ||
                  node.getType() === 'callout' ||
                  node.getType() === 'divider' ||
                  node.getType() === 'header'
                ) {
                  node.remove();
                }
              });
            });
          });
          setShowConfirm(true);
          return true;
        }
        return false;
      },
      COMMAND_PRIORITY_HIGH,
    );
  }, [editor]);

  return (
    <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Confirm Deletion</AlertDialogTitle>
          <AlertDialogDescription>Are you sure you want to delete this structural element? This action cannot be undone.</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={() => setShowConfirm(false)}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={() => {
              pendingDelete();
              setShowConfirm(false);
            }}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

// Update the initialConfig nodes array to include ButtonNode
const initialConfig = {
  namespace: 'GameGuildEditor',
  editorState: undefined,
  nodes: [
    HeadingNode,
    QuoteNode,
    ListItemNode,
    ListNode,
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
    VideoNode,
    AudioNode,
    HeaderNode,
    DividerNode,
    ButtonNode, // Add ButtonNode here
    CalloutNode, // Add CalloutNode here
    GalleryNode,
    PresentationNode,
    SourceNode, // Add SourceNode here
    YouTubeNode, // Add YouTubeNode here
    SpotifyNode, // Add SpotifyNode here
    SourceCodeNode, // Add SourceCodeNode here
  ],
  theme: {
    text: {
      bold: 'font-bold',
      italic: 'italic',
      underline: 'underline',
    },
    paragraph: 'my-2',
    heading: {
      h1: 'text-3xl font-bold',
      h2: 'text-2xl font-bold',
      h3: 'text-xl font-bold',
      h4: 'text-lg font-bold',
      h5: 'text-base font-bold',
    },
    list: {
      ul: 'list-disc list-inside',
      ol: 'list-decimal list-inside',
    },
    quote: 'border-l-4 border-muted pl-4 italic',
    code: 'bg-muted p-1 rounded font-mono text-sm',
  },
  onError: (error: Error) => {
    console.error(error);
  },
};

// Add the ButtonPlugin to the Editor component
interface EditorProps {
  className?: string;
  initialState?: string;
  onChange?: (state: string) => void;
  editorRef?: React.MutableRefObject<LexicalEditor | null>;
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void;
}

// Criar um plugin para gerenciar a referência do editor:
function EditorRefPlugin({ editorRef }: { editorRef?: React.MutableRefObject<LexicalEditor | null> }) {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (editorRef) {
      editorRef.current = editor;
    }
    return () => {
      if (editorRef) {
        editorRef.current = null;
      }
    };
  }, [editor, editorRef]);

  return null;
}

// Atualizar a função Editor para incluir o EditorRefPlugin:
export function Editor({ className, initialState, onChange, editorRef, onLoadingChange }: EditorProps) {
  const [isLoadingProject, setIsLoadingProject] = useState(false);

  useEffect(() => {
    if (onLoadingChange) {
      onLoadingChange(setIsLoadingProject);
    }
  }, [onLoadingChange]);

  return (
    <LexicalComposer
      initialConfig={{
        ...initialConfig,
        editorState: initialState || undefined,
      }}
    >
      <EditorLoadingProvider value={isLoadingProject}>
        <div className={cn('rounded-lg border', className)}>
          <YouTubeAudioStyle />
          <EditorToolbar />
          <div className="relative">
            <RichTextPlugin
              contentEditable={<ContentEditable className="min-h-[150px] p-3 outline-none" />}
              placeholder={<div className="pointer-events-none absolute left-[13px] top-[13px] select-none text-muted-foreground">Start typing...</div>}
              ErrorBoundary={LexicalErrorBoundary}
            />
            <FloatingContentInsertPlugin />
            <FloatingTextFormatToolbarPlugin />
            <ImagePlugin />
            <QuizPlugin />
            <MarkdownPlugin />
            <HTMLPlugin />
            <VideoPlugin />
            <AudioPlugin />
            <HeaderPlugin />
            <DividerPlugin />
            <ButtonPlugin />
            <CalloutPlugin />
            <GalleryPlugin />
            <PresentationPlugin />
            <SourcePlugin />
            <YouTubePlugin />
            <SpotifyPlugin />
            <CodePlugin />
            <SourceCodePlugin />
            <OnChangePlugin
              onChange={(editorState) => {
                if (onChange) {
                  onChange(JSON.stringify(editorState.toJSON()));
                }
              }}
            />
            <EditorRefPlugin editorRef={editorRef} />
            <StructureDeleteConfirmPlugin />
            <HistoryPlugin />
            <ListPlugin />
            <MarkdownShortcutPlugin transformers={TRANSFORMERS} />
          </div>
        </div>
      </EditorLoadingProvider>
    </LexicalComposer>
  );
}
