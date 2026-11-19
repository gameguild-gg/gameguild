import { createContentGradingDefinition, parseScoreValue, type ContentGradingDefinitionV2 } from '@game-guild/grading';
import { parseCodingAuthoringDefinition } from '@game-guild/coding';
import { allocateCodeTestScores } from './allocation';
import { CODE_CONTENT_TYPE, CODE_ITEM_PROJECTION_SCHEMA_VERSION, type CodeGradingItemInput, type CodeItemProjection } from './contracts';

export function createCodeGradingDefinition(input: CodeGradingItemInput): ContentGradingDefinitionV2 {
  assertItemId(input.itemId);
  return createContentGradingDefinition([input.itemId]);
}

export function projectCodeGradingItems(input: CodeGradingItemInput): readonly [CodeItemProjection] {
  assertItemId(input.itemId);
  const definition = parseCodingAuthoringDefinition(input.definition);
  const maxScore = parseScoreValue(input.maxScore);
  return [
    {
      schemaVersion: CODE_ITEM_PROJECTION_SCHEMA_VERSION,
      itemId: input.itemId,
      itemType: 'code',
      maxScore,
      source: { contentType: CODE_CONTENT_TYPE, itemId: input.itemId },
      definition,
      testScoreAllocation: allocateCodeTestScores(definition.tests, maxScore),
    },
  ];
}

function assertItemId(itemId: string): void {
  if (!itemId.trim()) throw new TypeError('Code grading item ID must be non-empty.');
}
