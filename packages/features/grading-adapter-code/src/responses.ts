import { ASSESSMENT_RESPONSE_SCHEMA_VERSION, type AssessmentResponseEnvelopeV1 } from '@game-guild/grading';
import { parseCodingSubmission, validateCodingSubmissionAgainstDefinition, type CodingSubmissionV1 } from '@game-guild/coding';
import {
  CODE_CONTENT_TYPE,
  CODE_RESPONSE_PAYLOAD_SCHEMA,
  type CodeItemProjectionV1,
  type CodeSubmissionEnvelopeV1,
  type CodeSubmissionPayloadV1,
} from './contracts';
import { requireSingleItem } from './delivery';

const ENVELOPE_KEYS = new Set(['schemaVersion', 'contentType', 'payloadSchema', 'payload']);
const PAYLOAD_KEYS = new Set(['submission']);

export function createCodeSubmissionEnvelope(submission: CodingSubmissionV1): CodeSubmissionEnvelopeV1 {
  return parseCodeSubmissionEnvelope({
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: CODE_CONTENT_TYPE,
    payloadSchema: CODE_RESPONSE_PAYLOAD_SCHEMA,
    payload: { submission },
  });
}

export function parseCodeSubmissionEnvelope(value: unknown): CodeSubmissionEnvelopeV1 {
  const envelope = asRecord(value);
  if (!envelope) throw new TypeError('Code response envelope must be an object.');
  assertOnlyKeys(envelope, ENVELOPE_KEYS, 'Code response envelope');
  if (
    envelope.schemaVersion !== ASSESSMENT_RESPONSE_SCHEMA_VERSION ||
    envelope.contentType !== CODE_CONTENT_TYPE ||
    envelope.payloadSchema !== CODE_RESPONSE_PAYLOAD_SCHEMA
  ) {
    throw new TypeError('Code response envelope does not match its versioned contract.');
  }
  const payload = asRecord(envelope.payload);
  if (!payload) throw new TypeError('Code response payload must be an object.');
  assertOnlyKeys(payload, PAYLOAD_KEYS, 'Code response payload');
  return {
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: CODE_CONTENT_TYPE,
    payloadSchema: CODE_RESPONSE_PAYLOAD_SCHEMA,
    payload: { submission: parseCodingSubmission(payload.submission) },
  };
}

export function decodeCodeSubmissionEnvelope(envelope: AssessmentResponseEnvelopeV1, items: readonly CodeItemProjectionV1[]): CodeSubmissionPayloadV1 {
  const parsed = parseCodeSubmissionEnvelope(envelope);
  const item = requireSingleItem(items);
  return {
    submission: validateCodingSubmissionAgainstDefinition(parsed.payload.submission, item.definition),
  };
}

function assertOnlyKeys(value: Record<string, unknown>, allowed: Set<string>, label: string): void {
  const unknown = Object.keys(value).filter((key) => !allowed.has(key));
  if (unknown.length > 0) throw new TypeError(`${label} contains unknown fields: ${unknown.join(', ')}.`);
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}
