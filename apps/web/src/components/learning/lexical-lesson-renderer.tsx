"use client";

import type { SerializedEditorState } from "lexical";
import { LexicalSurface } from "@game-guild/lexical-surface";
import { lexicalSurfaceViewerAdapters } from "@/components/block-content-editor/lexical-surface-viewer-adapters";

interface LexicalLessonRendererProps {
  content: unknown;
  itemId: string;
}

function parseEditorState(content: unknown): SerializedEditorState | null {
  if (typeof content === "string") {
    try {
      return parseEditorState(JSON.parse(content));
    } catch {
      return null;
    }
  }

  if (!content || typeof content !== "object" || Array.isArray(content)) {
    return null;
  }

  const state = content as Partial<SerializedEditorState>;
  return state.root && typeof state.root === "object"
    ? (state as SerializedEditorState)
    : null;
}

export function LexicalLessonRenderer({
  content,
  itemId,
}: LexicalLessonRendererProps) {
  const initialState = parseEditorState(content);

  if (!initialState) {
    return (
      <p className="text-sm text-muted-foreground">
        This Lexical lesson has no published content.
      </p>
    );
  }

  return (
    <LexicalSurface
      namespace="LearnerLesson"
      mountKey={`learner-lesson-${itemId}`}
      initialState={initialState}
      readOnly
      accessibleLabel="Lesson content"
      contentClassName="max-w-none"
      features={{ pageLayout: false }}
      adapters={lexicalSurfaceViewerAdapters}
    />
  );
}
