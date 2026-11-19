import { ASSESSMENT_RESPONSE_SCHEMA_VERSION, type AssessmentResponseEnvelopeV1 } from '@game-guild/grading';
import {
  normalizeCodingPath,
  parseCodingSubmission,
  validateCodingSubmissionAgainstDefinition,
  type CodingSubmission,
} from '@game-guild/coding';
import {
  CODE_CONTENT_TYPE,
  CODE_RESPONSE_PAYLOAD_SCHEMA,
  type CodeItemProjection,
  type CodeResponseFile,
  type CodeSubmissionEnvelope,
  type CodeSubmissionPayload,
} from './contracts';
import { requireSingleItem } from './delivery';

const ENVELOPE_KEYS = new Set(['schemaVersion', 'contentType', 'payloadSchema', 'payload']);
const PAYLOAD_KEYS = new Set(['files']);
const FILE_KEYS = new Set(['content', 'encoding']);

export function createCodeSubmissionEnvelope(submission: CodingSubmission): CodeSubmissionEnvelope {
  const parsed = parseCodingSubmission(submission);
  return parseCodeSubmissionEnvelope({
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: CODE_CONTENT_TYPE,
    payloadSchema: CODE_RESPONSE_PAYLOAD_SCHEMA,
    payload: {
      files: Object.fromEntries(
        parsed.files.map(({ path, content }) => [path, { content, encoding: 'text' as const }]),
      ),
    },
  });
}

export function parseCodeSubmissionEnvelope(value: unknown): CodeSubmissionEnvelope {
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
  const files = asRecord(payload.files);
  if (!files) throw new TypeError('Code response files must be an object.');

  const parsedFiles: Record<string, CodeResponseFile> = {};
  for (const [path, fileValue] of Object.entries(files)) {
    const file = asRecord(fileValue);
    if (!file) throw new TypeError(`Code response file ${path} must be an object.`);
    assertOnlyKeys(file, FILE_KEYS, `Code response file ${path}`);
    if (file.encoding !== 'text' || typeof file.content !== 'string') {
      throw new TypeError(`Code response file ${path} must contain text content.`);
    }
    parsedFiles[path] = { content: file.content, encoding: 'text' };
  }

  return {
    schemaVersion: ASSESSMENT_RESPONSE_SCHEMA_VERSION,
    contentType: CODE_CONTENT_TYPE,
    payloadSchema: CODE_RESPONSE_PAYLOAD_SCHEMA,
    payload: { files: parsedFiles },
  };
}

export function decodeCodeSubmissionEnvelope(
  envelope: AssessmentResponseEnvelopeV1,
  items: readonly CodeItemProjection[],
): CodeSubmissionPayload {
  const parsed = parseCodeSubmissionEnvelope(envelope);
  const item = requireSingleItem(items);
  const authoredByPath = new Map(item.definition.files.map((file) => [file.path, file]));
  const submission = parseCodingSubmission({
    schemaVersion: 1,
    files: Object.entries(parsed.payload.files).map(([path, file]) => {
      const canonicalPath = normalizeCodingPath(path);
      return {
        fileId: authoredByPath.get(canonicalPath)?.id ?? stableIdentifier('learner-file', canonicalPath),
        path: canonicalPath,
        encoding: 'text',
        content: file.content,
      };
    }),
  });
  return {
    submission: validateCodingSubmissionAgainstDefinition(submission, item.definition),
  };
}

function stableIdentifier(prefix: string, source: string): string {
  let hash = 2_166_136_261;
  for (let index = 0; index < source.length; index += 1) {
    hash ^= source.charCodeAt(index);
    hash = Math.imul(hash, 16_777_619);
  }
  return `${prefix}-${(hash >>> 0).toString(16).padStart(8, '0')}`;
}

function assertOnlyKeys(value: Record<string, unknown>, allowed: Set<string>, label: string): void {
  const unknown = Object.keys(value).filter((key) => !allowed.has(key));
  if (unknown.length > 0) throw new TypeError(`${label} contains unknown fields: ${unknown.join(', ')}.`);
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}
