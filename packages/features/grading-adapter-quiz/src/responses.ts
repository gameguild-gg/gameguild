import {
  ASSESSMENT_RESPONSE_SCHEMA_VERSION,
  type AssessmentResponseEnvelopeV1,
} from "@game-guild/grading";
import { parseQuizAnswer, QuizEntryType, type QuizAnswer, type QuizEntry } from "@game-guild/quiz";
import {
  QUIZ_ANSWER_PAYLOAD_SCHEMA,
  QUIZ_CONTENT_TYPE,
  type QuizAnswerEnvelopeV1,
  type QuizAnswerPayloadV1,
  type QuizItemProjectionV1,
} from "./contracts";

const ENVELOPE_KEYS = new Set(["schemaVersion", "contentType", "payloadSchema", "payload"]);
const PAYLOAD_KEYS = new Set(["answers"]);

export function createQuizAnswerEnvelope(
  answers: Readonly<Record<string, QuizAnswer>>,
): QuizAnswerEnvelopeV1 {
  return parseQuizAnswerEnvelope({
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: QUIZ_CONTENT_TYPE,
    payloadSchema: QUIZ_ANSWER_PAYLOAD_SCHEMA,
    payload: { answers },
  });
}

export function parseQuizAnswerEnvelope(value: unknown): QuizAnswerEnvelopeV1 {
  const envelope = asRecord(value);
  if (!envelope) throw new TypeError("Quiz answer envelope must be an object.");
  assertOnlyKeys(envelope, ENVELOPE_KEYS, "Quiz answer envelope");
  if (envelope.schemaVersion !== ASSESSMENT_RESPONSE_SCHEMA_VERSION) {
    throw new TypeError(`Quiz answer envelope schemaVersion must be ${ASSESSMENT_RESPONSE_SCHEMA_VERSION}.`);
  }
  if (envelope.contentType !== QUIZ_CONTENT_TYPE) {
    throw new TypeError(`Quiz answer envelope contentType must be ${QUIZ_CONTENT_TYPE}.`);
  }
  if (envelope.payloadSchema !== QUIZ_ANSWER_PAYLOAD_SCHEMA) {
    throw new TypeError(`Unsupported quiz answer payload schema: ${String(envelope.payloadSchema)}.`);
  }

  const payload = asRecord(envelope.payload);
  if (!payload) throw new TypeError("Quiz answer payload must be an object.");
  assertOnlyKeys(payload, PAYLOAD_KEYS, "Quiz answer payload");
  const sourceAnswers = asRecord(payload.answers);
  if (!sourceAnswers) throw new TypeError("Quiz answer payload answers must be an object.");

  const answers: Record<string, QuizAnswer> = {};
  for (const [itemId, answer] of Object.entries(sourceAnswers)) {
    if (!itemId.trim()) throw new TypeError("Quiz answer item IDs must be non-empty.");
    answers[itemId] = parseQuizAnswer(answer);
  }

  return {
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: QUIZ_CONTENT_TYPE,
    payloadSchema: QUIZ_ANSWER_PAYLOAD_SCHEMA,
    payload: { answers },
  };
}

export function decodeQuizAnswerEnvelope(
  envelope: AssessmentResponseEnvelopeV1,
  items: readonly QuizItemProjectionV1[],
): QuizAnswerPayloadV1 {
  const parsed = parseQuizAnswerEnvelope(envelope);
  const expected = new Map(items.map(({ itemId, itemType }) => [itemId, itemType]));
  for (const [itemId, answer] of Object.entries(parsed.payload.answers)) {
    const expectedType = expected.get(itemId);
    if (!expectedType) throw new TypeError(`Quiz answer references unknown item ${itemId}.`);
    if (answer.type !== expectedType) {
      throw new TypeError(`Quiz answer type for ${itemId} does not match its question type.`);
    }
    const projection = items.find((item) => item.itemId === itemId)!;
    validateAnswerDomain(answer, projection.authoringEntry);
  }
  return parsed.payload;
}

function validateAnswerDomain(answer: QuizAnswer, entry: QuizEntry): void {
  switch (entry.type) {
    case QuizEntryType.SingleChoice: {
      const allowed = new Set(entry.options.map(({ id }) => id));
      if (answer.type === entry.type && answer.optionId !== null && !allowed.has(answer.optionId)) {
        throw new TypeError("optionId contains an unknown value.");
      }
      return;
    }
    case QuizEntryType.MultipleChoice: {
      if (answer.type !== entry.type) return;
      assertUnique(answer.optionIds, "optionIds");
      assertKnown(answer.optionIds, new Set(entry.options.map(({ id }) => id)), "optionIds");
      if (answer.optionIds.length > (entry.selectionLimit ?? entry.options.length)) {
        throw new TypeError("optionIds exceeds the configured selection limit.");
      }
      return;
    }
    case QuizEntryType.FillInTheBlank:
      if (answer.type === entry.type) {
        assertKnown(Object.keys(answer.values), new Set(entry.blanks.map(({ id }) => id)), "blank IDs");
      }
      return;
    case QuizEntryType.Matching: {
      if (answer.type !== entry.type) return;
      const pairIds = new Set(entry.pairs.map(({ id }) => id));
      assertKnown(Object.keys(answer.matches), pairIds, "matching pair IDs");
      if (Object.keys(answer.matches).length > pairIds.size) {
        throw new TypeError("matches exceeds the number of matching pairs.");
      }
      const allowedValues = new Set([
        ...entry.pairs.map(({ right }) => right),
        ...(entry.rightOptions ?? []),
        ...(entry.distractors ?? []),
      ]);
      const selected = Object.values(answer.matches);
      assertUnique(selected, "matching values");
      assertKnown(selected, allowedValues, "matching values");
      return;
    }
    case QuizEntryType.Ordering:
      if (answer.type === entry.type) {
        assertUnique(answer.itemIds, "itemIds");
        assertKnown(answer.itemIds, new Set(entry.items.map(({ id }) => id)), "itemIds");
        if (answer.itemIds.length > entry.items.length) {
          throw new TypeError("itemIds exceeds the number of ordering items.");
        }
      }
      return;
    case QuizEntryType.Categorization: {
      if (answer.type !== entry.type) return;
      const itemIds = new Set(entry.items.map(({ id }) => id));
      const categoryIds = new Set(entry.categories.map(({ id }) => id));
      assertKnown(Object.keys(answer.categoryIdsByItem), itemIds, "categorization item IDs");
      for (const values of Object.values(answer.categoryIdsByItem)) {
        assertUnique(values, "category IDs");
        assertKnown(values, categoryIds, "category IDs");
      }
      return;
    }
    case QuizEntryType.Rating:
      if (answer.type === entry.type && answer.value !== null) {
        const { min, max, step } = entry.scale;
        const offset = (answer.value - min) / step;
        if (step <= 0 || answer.value < min || answer.value > max || !Number.isInteger(offset)) {
          throw new TypeError("Rating value is outside its configured scale.");
        }
      }
      return;
    case QuizEntryType.Hotspot:
      if (answer.type === entry.type && answer.point &&
          (answer.point.x < 0 || answer.point.x > 100 || answer.point.y < 0 || answer.point.y > 100)) {
        throw new TypeError("Hotspot coordinates must be between 0 and 100.");
      }
      return;
    case QuizEntryType.Highlight:
      if (answer.type === entry.type) {
        const ranges = [...answer.spans].sort((left, right) => left.start - right.start || left.end - right.end);
        if (ranges.some(({ end }) => end > entry.plainText.length)) {
          throw new TypeError("Highlight span exceeds the source text.");
        }
        if (ranges.some((range, index) => index > 0 && range.start < ranges[index - 1]!.end)) {
          throw new TypeError("Highlight spans cannot overlap.");
        }
      }
      return;
    default:
      return;
  }
}

function assertUnique(values: readonly string[], label: string): void {
  if (new Set(values).size !== values.length) throw new TypeError(`${label} contains duplicate values.`);
}

function assertKnown(values: readonly string[], allowed: ReadonlySet<string>, label: string): void {
  const unknown = values.find((value) => !allowed.has(value));
  if (unknown !== undefined) throw new TypeError(`${label} contains unknown value ${unknown}.`);
}

function assertOnlyKeys(value: Record<string, unknown>, allowed: Set<string>, label: string): void {
  const unknown = Object.keys(value).filter((key) => !allowed.has(key));
  if (unknown.length > 0) throw new TypeError(`${label} contains unknown fields: ${unknown.join(", ")}.`);
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null;
}
