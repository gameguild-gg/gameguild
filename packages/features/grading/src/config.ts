import {
  CONTENT_GRADING_SCHEMA_VERSION,
  GradingContractValidationError,
  type ContentGradingDefinitionV2,
  type GradingItemAuthoringV2,
} from "./types";

const DEFINITION_KEYS = new Set(["schemaVersion", "items"]);
const ITEM_KEYS = new Set(["rubricRef"]);

export function createContentGradingDefinition(
  itemIds: readonly string[],
): ContentGradingDefinitionV2 {
  return {
    schemaVersion: CONTENT_GRADING_SCHEMA_VERSION,
    items: Object.fromEntries(uniqueItemIds(itemIds).map((itemId) => [itemId, {}])),
  };
}

export function syncContentGradingDefinition(
  itemIds: readonly string[],
  definition: ContentGradingDefinitionV2,
): ContentGradingDefinitionV2 {
  const validated = validateContentGradingDefinition(definition);
  return {
    schemaVersion: CONTENT_GRADING_SCHEMA_VERSION,
    items: Object.fromEntries(
      uniqueItemIds(itemIds).map((itemId) => [itemId, { ...(validated.items[itemId] ?? {}) }]),
    ),
  };
}

export function validateContentGradingDefinition(value: unknown): ContentGradingDefinitionV2 {
  const issues: string[] = [];
  const root = asRecord(value);
  if (!root) throw new GradingContractValidationError(["Grading definition must be an object."]);

  for (const key of Object.keys(root)) {
    if (!DEFINITION_KEYS.has(key)) issues.push(`${key} is not allowed in ContentGradingDefinitionV2.`);
  }
  if (root.schemaVersion !== CONTENT_GRADING_SCHEMA_VERSION) {
    issues.push(`schemaVersion must be ${CONTENT_GRADING_SCHEMA_VERSION}.`);
  }

  const sourceItems = asRecord(root.items);
  if (!sourceItems) issues.push("items must be an object.");
  const items: Record<string, GradingItemAuthoringV2> = {};
  for (const [itemId, rawItem] of Object.entries(sourceItems ?? {})) {
    if (!itemId.trim()) {
      issues.push("items cannot contain an empty item ID.");
      continue;
    }
    const item = asRecord(rawItem);
    if (!item) {
      issues.push(`items.${itemId} must be an object.`);
      continue;
    }
    for (const key of Object.keys(item)) {
      if (!ITEM_KEYS.has(key)) issues.push(`items.${itemId}.${key} is not allowed.`);
    }
    if (item.rubricRef !== undefined && (typeof item.rubricRef !== "string" || !item.rubricRef.trim())) {
      issues.push(`items.${itemId}.rubricRef must be a non-empty string.`);
    }
    items[itemId] = typeof item.rubricRef === "string" ? { rubricRef: item.rubricRef } : {};
  }

  if (issues.length > 0) throw new GradingContractValidationError(issues);
  return { schemaVersion: CONTENT_GRADING_SCHEMA_VERSION, items };
}

export function tryParseContentGradingDefinition(value: unknown): ContentGradingDefinitionV2 | null {
  try {
    return validateContentGradingDefinition(value);
  } catch {
    return null;
  }
}

function uniqueItemIds(itemIds: readonly string[]): string[] {
  const seen = new Set<string>();
  for (const itemId of itemIds) {
    if (!itemId.trim()) throw new GradingContractValidationError(["Item IDs must be non-empty strings."]);
    if (seen.has(itemId)) throw new GradingContractValidationError([`Duplicate item ID: ${itemId}.`]);
    seen.add(itemId);
  }
  return [...seen];
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null;
}
