import { createContentGradingDefinition, parseScoreValue, type ContentGradingDefinitionV2 } from '@game-guild/grading';
import { parseCodingAuthoringDefinition, parseCodingAuthoringDefinitionV2 } from '@game-guild/coding';
import { allocateCodeTestScores } from './allocation';
import { CODE_CONTENT_TYPE, CODE_ITEM_PROJECTION_SCHEMA_VERSION, type CodeGradingItemInputV1, type CodeItemProjectionV1 } from './contracts';

export function createCodeGradingDefinition(input: CodeGradingItemInputV1): ContentGradingDefinitionV2 {
  assertItemId(input.itemId);
  return createContentGradingDefinition([input.itemId]);
}

export function projectCodeGradingItems(input: CodeGradingItemInputV1): readonly [CodeItemProjectionV1] {
  assertItemId(input.itemId);
  const definition = input.definition.schemaVersion === 2
    ? parseCodingAuthoringDefinitionV2(input.definition)
    : parseCodingAuthoringDefinition(input.definition);
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
