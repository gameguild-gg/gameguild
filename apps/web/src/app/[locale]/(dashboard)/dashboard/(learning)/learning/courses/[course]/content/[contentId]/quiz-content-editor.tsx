'use client';

import { lazy, Suspense } from 'react';
import { Label } from '@game-guild/ui/components/label';
import { Skeleton } from '@/components/ui/skeleton';
import type { FieldConfig, ToolbarConfig } from '@/components/block-content-editor/engines/editor-config';

const QUIZ_FIELD_CONFIG = {
  allowedBlockTypes: [],
  projectType: 'quiz',
  allowedProjectTypes: ['quiz'],
} satisfies Partial<FieldConfig>;

const QUIZ_TOOLBAR_CONFIG = {} satisfies Partial<ToolbarConfig>;

const QuizEditorSurface = lazy(async () => {
  const [{ EditorProvider }, { EditorField }, { EditorDialogs }] = await Promise.all([
    import('@/components/block-content-editor/engines/editor-provider'),
    import('@/components/block-content-editor/engines/editor-field'),
    import('@/components/block-content-editor/engines/editor-dialogs'),
  ]);

  return {
    default: function QuizEditorSurfaceComponent() {
      return (
        <EditorProvider fieldConfig={QUIZ_FIELD_CONFIG} toolbarConfig={QUIZ_TOOLBAR_CONFIG}>
          <EditorField
            contentContainer={{
              className: 'flex-1 h-full max-h-[600px]',
              blocksClassName: 'w-full border-none rounded-none bg-white p-4 dark:bg-gray-900',
            }}
          />
          <EditorDialogs />
        </EditorProvider>
      );
    },
  };
});

function EditorLoadingState() {
  return (
    <div className="space-y-3 rounded-lg border border-gray-200 p-4 dark:border-gray-700">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-[300px] w-full" />
    </div>
  );
}

export function QuizContentEditor() {
  return (
    <div className="space-y-2">
      <Label>Quiz editor</Label>
      <p className="text-muted-foreground text-xs">
        Use the quiz block editor to build your questions.
      </p>
      <div className="min-h-[400px] overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <Suspense fallback={<EditorLoadingState />}>
          <QuizEditorSurface />
        </Suspense>
      </div>
    </div>
  );
}
