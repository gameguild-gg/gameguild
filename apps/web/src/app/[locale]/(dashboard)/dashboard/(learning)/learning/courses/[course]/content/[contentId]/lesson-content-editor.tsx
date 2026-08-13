"use client";

import { lazy, Suspense, useCallback } from "react";
import { Label } from "@game-guild/ui/components/label";
import { Skeleton } from "@/components/ui/skeleton";
import { type LexicalSurfaceFeatures } from "@game-guild/lexical-surface";
import { lexicalSurfaceAdapters } from "@/components/block-content-editor/lexical-surface-adapters";
import type { SerializedEditorState } from "lexical";

const LexicalSurface = lazy(async () => {
  const mod = await import("@game-guild/lexical-surface");
  return { default: mod.LexicalSurface };
});

const LESSON_EDITOR_FEATURES = {
  toolbar: true,
  insertMenu: true,
  floatingTextFormat: true,
  floatingLinkEditor: true,
  draggable: true,
  picker: true,
  pageLayout: false,
  shortcuts: true,
  equation: true,
  excalidraw: true,
  emoji: true,
  autoEmbed: true,
  contextMenu: true,
  codeAction: true,
  table: true,
  layout: true,
  collapsible: true,
  sticky: true,
  admonition: true,
  button: true,
  divider: true,
  mermaid: true,
  vegaLite: true,
  media: true,
  history: true,
  list: true,
  link: true,
  checkList: true,
  tabIndentation: true,
} satisfies LexicalSurfaceFeatures;

interface LessonContentEditorProps {
  itemId: string;
  initialState: SerializedEditorState | null;
  onChange: (state: SerializedEditorState) => void;
}

function EditorLoadingState() {
  return (
    <div className="space-y-3 rounded-lg border border-gray-200 p-4 dark:border-gray-700">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-[300px] w-full" />
    </div>
  );
}

export function LessonContentEditor({
  itemId,
  initialState,
  onChange,
}: LessonContentEditorProps) {
  const handleEditorChange = useCallback(
    (state: SerializedEditorState) => {
      onChange(state);
    },
    [onChange],
  );

  return (
    <div className="space-y-2">
      <Label>Body</Label>
      <p className="text-muted-foreground text-xs">
        Use the rich-text editor to create your lesson content.
      </p>
      <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <Suspense fallback={<EditorLoadingState />}>
          <LexicalSurface
            namespace="LessonEditor"
            mountKey={itemId}
            initialState={initialState}
            onChange={handleEditorChange}
            accessibleLabel="Body"
            placeholder="Start writing your lesson content..."
            contentStyle={{ minHeight: "400px" }}
            contentClassName="max-w-none"
            adapters={lexicalSurfaceAdapters}
            features={LESSON_EDITOR_FEATURES}
          />
        </Suspense>
      </div>
    </div>
  );
}
