'use client';

import { lazy, Suspense, useCallback, useMemo, useState } from 'react';
import { Label } from '@game-guild/ui/components/label';
import { Skeleton } from '@/components/ui/skeleton';
import { blocksToStorage, storageToBlocks } from '@/components/block-content-editor/lib/storage/editor/block-storage';
import type { BlockArray, BlockStorage } from '@/components/block-content-editor/lib/storage/editor/block-structure';
import { coerceBlockStorageDefinition } from '@/components/block-content-editor/lib/assessment/assessment-contracts';

const BlockArrayEditor = lazy(async () => {
  const mod = await import('@/components/block-content-editor/engines/blocks/block-array-editor');
  return { default: mod.BlockArrayEditor };
});

function EditorLoadingState() {
  return (
    <div className="space-y-3 rounded-lg border border-gray-200 p-4 dark:border-gray-700">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-[300px] w-full" />
    </div>
  );
}

interface QuizAssessmentEditorProps {
  initialDefinition: unknown;
  onChange: (definition: BlockStorage) => void;
}

export function QuizAssessmentEditor({ initialDefinition, onChange }: QuizAssessmentEditorProps) {
  const initialBlocks = useMemo(
    () => storageToBlocks(coerceBlockStorageDefinition(initialDefinition)),
    [initialDefinition],
  );
  const [blocks, setBlocks] = useState<BlockArray>(initialBlocks);

  const handleBlocksChange = useCallback(
    (nextBlocks: BlockArray) => {
      setBlocks(nextBlocks);
      onChange(blocksToStorage(nextBlocks));
    },
    [onChange],
  );

  return (
    <div className="space-y-2">
      <Label>Quiz assessment</Label>
      <div className="min-h-[400px] overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="max-h-[600px] overflow-y-auto">
          <div className="w-full bg-white p-4 dark:bg-gray-900">
            <Suspense fallback={<EditorLoadingState />}>
              <BlockArrayEditor
                blocks={blocks}
                onChange={handleBlocksChange}
                projectType="quiz"
              />
            </Suspense>
          </div>
        </div>
      </div>
    </div>
  );
}
