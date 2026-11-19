import type { AssessmentExecutionDeliveryV1, AssessmentItemManifestV1 } from '@game-guild/grading';
import { projectCodingDefinitionForLearner, projectCodingDefinitionForLearnerV2 } from '@game-guild/coding';
import { CODE_ASSESSMENT_TYPE_ADAPTER, type CodeItemProjectionV1, type CodeLearnerDeliveryItemV1 } from './contracts';

export function createCodeItemManifest(items: readonly CodeItemProjectionV1[]): AssessmentItemManifestV1[] {
  const item = requireSingleItem(items);
  return [
    {
      itemId: item.itemId,
      itemType: item.itemType,
      adapterKey: CODE_ASSESSMENT_TYPE_ADAPTER.key,
      adapterVersion: CODE_ASSESSMENT_TYPE_ADAPTER.version,
    },
  ];
}

export function createCodeExecutionDelivery(
  definitionRevisionId: string,
  executionSnapshotHash: string,
  items: readonly CodeItemProjectionV1[],
  itemOrder: readonly string[] = items.map(({ itemId }) => itemId),
): AssessmentExecutionDeliveryV1<CodeLearnerDeliveryItemV1> {
  const item = requireSingleItem(items);
  if (itemOrder.length !== 1 || itemOrder[0] !== item.itemId) {
    throw new TypeError('Code delivery itemOrder must contain its single item exactly once.');
  }
  return {
    schemaVersion: 1,
    definitionRevisionId,
    executionSnapshotHash,
    itemOrder: [item.itemId],
    items: {
      [item.itemId]: {
        adapterKey: CODE_ASSESSMENT_TYPE_ADAPTER.key,
        adapterVersion: CODE_ASSESSMENT_TYPE_ADAPTER.version,
        learnerPayload: {
          itemId: item.itemId,
          definition: item.definition.schemaVersion === 2
            ? projectCodingDefinitionForLearnerV2(item.definition)
            : projectCodingDefinitionForLearner(item.definition),
        },
      },
    },
  };
}

export function requireSingleItem(items: readonly CodeItemProjectionV1[]): CodeItemProjectionV1 {
  if (items.length !== 1 || !items[0]) {
    throw new TypeError('A coding assessment requires exactly one projected item.');
  }
  return items[0];
}
