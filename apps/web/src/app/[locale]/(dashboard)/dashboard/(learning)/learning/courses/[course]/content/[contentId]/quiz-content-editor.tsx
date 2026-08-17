"use client";

import { useCallback, useMemo, useState } from "react";
import {
  QuizCollectionEditor,
  type QuizCollectionItem,
} from "@game-guild/quiz-surface/editor";
import type {
  ContentGradingDefinition,
  GradingResultUse,
} from "@game-guild/grading";
import {
  createDisabledGradingDefinition,
  createQuizGradingDefinition,
  readContentGradingDefinition,
  sumGradedItemPoints,
  syncQuizGradingDefinition,
  writeContentGradingDefinition,
} from "@game-guild/grading";
import { Badge } from "@game-guild/ui/components/badge";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Switch } from "@game-guild/ui/components/switch";
import {
  blocksToStorage,
  storageToBlocks,
} from "@/components/block-content-editor/lib/storage/editor/block-storage";
import type {
  BlockArray,
  BlockStorage,
} from "@/components/block-content-editor/lib/storage/editor/block-structure";
import { nextBlockId } from "@/components/block-content-editor/lib/storage/editor/block-structure";

type QuizEditorMode = "edit" | "preview";

interface ParsedQuizContent {
  blocks: BlockArray;
  grading: ContentGradingDefinition;
}

function parseQuizContent(
  raw: Record<string, unknown> | null | undefined,
): ParsedQuizContent {
  const fallback = {
    blocks: [],
    grading: createDisabledGradingDefinition(),
  } satisfies ParsedQuizContent;

  if (!isBlockStorage(raw)) return fallback;

  return {
    blocks: storageToBlocks(raw),
    grading:
      readContentGradingDefinition(raw) ?? createDisabledGradingDefinition(),
  };
}

function isBlockStorage(value: unknown): value is BlockStorage {
  const candidate = value as { order?: unknown; blocks?: unknown } | null;

  return Boolean(
    candidate &&
    Array.isArray(candidate.order) &&
    candidate.blocks &&
    typeof candidate.blocks === "object" &&
    !Array.isArray(candidate.blocks),
  );
}

function serializeQuizContent(
  blocks: BlockArray,
  grading: ContentGradingDefinition,
): Record<string, unknown> {
  const storage = blocksToStorage(blocks);
  const syncedGrading = grading.enabled
    ? syncQuizGradingDefinition(blocks, grading)
    : grading;
  const body = writeContentGradingDefinition(
    storage as unknown as Record<string, unknown>,
    syncedGrading,
  );
  return body;
}

function ensureQuizGradingDefinition(
  blocks: BlockArray,
  current: ContentGradingDefinition,
): ContentGradingDefinition {
  return syncQuizGradingDefinition(
    blocks,
    current.enabled ? current : createQuizGradingDefinition(blocks),
  );
}

interface QuizContentEditorProps {
  initialContent: Record<string, unknown> | null | undefined;
  onChange: (content: Record<string, unknown>) => void;
  mode?: QuizEditorMode;
}

export function QuizContentEditor({
  initialContent,
  onChange,
  mode = "edit",
}: QuizContentEditorProps) {
  const initialQuizContent = useMemo(
    () => parseQuizContent(initialContent),
    [initialContent],
  );
  const [blocks, setBlocks] = useState<BlockArray>(initialQuizContent.blocks);
  const [gradingConfig, setGradingConfig] = useState<ContentGradingDefinition>(
    initialQuizContent.grading,
  );

  const syncedGradingConfig = useMemo(
    () =>
      gradingConfig.enabled
        ? syncQuizGradingDefinition(blocks, gradingConfig)
        : gradingConfig,
    [blocks, gradingConfig],
  );
  const gradedItemCount = Object.keys(syncedGradingConfig.items).length;
  const gradedPoints = sumGradedItemPoints(syncedGradingConfig);
  const resultUse: GradingResultUse = syncedGradingConfig.outcome.uses.includes(
    "gradebook",
  )
    ? "gradebook"
    : "feedback";

  const emitChange = useCallback(
    (nextBlocks: BlockArray, nextGrading: ContentGradingDefinition) => {
      onChange(serializeQuizContent(nextBlocks, nextGrading));
    },
    [onChange],
  );

  const handleBlocksChange = useCallback(
    (nextBlocks: BlockArray) => {
      const nextGrading = gradingConfig.enabled
        ? syncQuizGradingDefinition(nextBlocks, gradingConfig)
        : gradingConfig;
      setBlocks(nextBlocks);
      setGradingConfig(nextGrading);
      emitChange(nextBlocks, nextGrading);
    },
    [emitChange, gradingConfig],
  );

  const updateGrading = useCallback(
    (
      updater: (current: ContentGradingDefinition) => ContentGradingDefinition,
    ) => {
      setGradingConfig((current) => {
        const next = updater(current);
        emitChange(blocks, next);
        return next;
      });
    },
    [blocks, emitChange],
  );

  const handleGradingEnabledChange = useCallback(
    (enabled: boolean) => {
      updateGrading(() =>
        enabled
          ? createQuizGradingDefinition(blocks)
          : createDisabledGradingDefinition(),
      );
    },
    [blocks, updateGrading],
  );

  const handleResultUseChange = useCallback(
    (nextUse: GradingResultUse) => {
      updateGrading((current) => {
        const synced = ensureQuizGradingDefinition(blocks, current);
        const uses: GradingResultUse[] =
          nextUse === "gradebook" ? ["feedback", "gradebook"] : ["feedback"];

        return {
          ...synced,
          outcome: {
            uses,
            gradebook:
              nextUse === "gradebook"
                ? {
                    groupId: synced.outcome.gradebook?.groupId ?? null,
                    weight: synced.outcome.gradebook?.weight,
                    required: synced.outcome.gradebook?.required ?? true,
                    includeInFinalGrade:
                      synced.outcome.gradebook?.includeInFinalGrade ?? true,
                  }
                : null,
          },
          score: {
            ...synced.score,
            maxScore: Math.max(1, synced.score.maxScore || gradedPoints || 1),
          },
        };
      });
    },
    [blocks, gradedPoints, updateGrading],
  );

  const handleMaxScoreChange = useCallback(
    (value: string) => {
      const maxScore = Math.max(1, Number(value) || 1);
      updateGrading((current) => {
        const synced = ensureQuizGradingDefinition(blocks, current);
        return {
          ...synced,
          score: {
            ...synced.score,
            maxScore,
            passingScore:
              synced.score.passingScore === undefined
                ? undefined
                : Math.min(synced.score.passingScore, maxScore),
          },
        };
      });
    },
    [blocks, updateGrading],
  );

  const handlePassingScoreChange = useCallback(
    (value: string) => {
      const passingScore = value.trim()
        ? Math.max(0, Number(value) || 0)
        : undefined;
      updateGrading((current) => {
        const synced = ensureQuizGradingDefinition(blocks, current);
        return {
          ...synced,
          score: {
            ...synced.score,
            passingScore:
              passingScore === undefined
                ? undefined
                : Math.min(passingScore, synced.score.maxScore),
          },
        };
      });
    },
    [blocks, updateGrading],
  );

  const quizItems = useMemo<QuizCollectionItem[]>(
    () =>
      blocks.flatMap((block) =>
        block.type === "quiz" ? [{ id: block.id, entry: block.data }] : [],
      ),
    [blocks],
  );

  const handleQuizItemsChange = useCallback(
    (items: QuizCollectionItem[]) => {
      handleBlocksChange(
        items.map((item) => ({
          id: item.id,
          type: "quiz" as const,
          data: item.entry,
        })),
      );
    },
    [handleBlocksChange],
  );

  if (mode === "preview") {
    return (
      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        createItemId={() => nextBlockId(blocks)}
        submissionMode={
          syncedGradingConfig.enabled ? "server-graded" : "local-practice"
        }
        readOnly={true}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Quiz editor</Label>
        <p className="text-muted-foreground text-xs">
          Use the quiz block editor to build your questions.
        </p>
      </div>

      <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-700">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="space-y-1">
            <Label htmlFor="quiz-grading-enabled">Grading</Label>
            <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
              <Badge
                variant={syncedGradingConfig.enabled ? "secondary" : "outline"}
              >
                {syncedGradingConfig.enabled
                  ? `${gradedItemCount} items`
                  : "Off"}
              </Badge>
              {syncedGradingConfig.enabled && (
                <span>{gradedPoints} configured pts</span>
              )}
            </div>
          </div>
          <Switch
            id="quiz-grading-enabled"
            checked={syncedGradingConfig.enabled}
            onCheckedChange={handleGradingEnabledChange}
          />
        </div>

        {syncedGradingConfig.enabled && (
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="quiz-result-use">Result destination</Label>
              <Select
                value={resultUse}
                onValueChange={(value) =>
                  handleResultUseChange(value as GradingResultUse)
                }
              >
                <SelectTrigger id="quiz-result-use">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="feedback">Feedback only</SelectItem>
                  <SelectItem value="gradebook">Gradebook</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="quiz-max-score">Max score</Label>
              <Input
                id="quiz-max-score"
                type="number"
                min={1}
                value={syncedGradingConfig.score.maxScore}
                onChange={(event) =>
                  handleMaxScoreChange(event.currentTarget.value)
                }
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="quiz-passing-score">Passing score</Label>
              <Input
                id="quiz-passing-score"
                type="number"
                min={0}
                value={syncedGradingConfig.score.passingScore ?? ""}
                onChange={(event) =>
                  handlePassingScoreChange(event.currentTarget.value)
                }
              />
            </div>
          </div>
        )}

        {syncedGradingConfig.enabled && (
          <div className="mt-3 flex flex-wrap gap-2">
            <Badge variant="outline">Assessment</Badge>
            <Badge variant="outline">
              {resultUse === "gradebook" ? "Gradebook" : "Feedback only"}
            </Badge>
          </div>
        )}
      </div>

      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        createItemId={() => nextBlockId(blocks)}
        submissionMode={
          syncedGradingConfig.enabled ? "server-graded" : "local-practice"
        }
      />
    </div>
  );
}
