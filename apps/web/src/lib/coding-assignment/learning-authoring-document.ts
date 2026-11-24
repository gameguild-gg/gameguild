import {
  CODING_DRAFT_TYPE,
  CODING_TOOLCHAIN,
  codingDraftOperations,
  compileCodingAuthoringDraft,
  parseCodingAuthoringDraft,
  type CodingAuthoringDraft,
} from '@game-guild/coding';
import type { AuthoringContentPayload } from '@/lib/learning/authoring';
import { narrowCodingAssignmentContent, type CodingAssignmentContent, type GradingConfig } from './types';
import { codingAssignmentToAuthoringDefinition, codingAuthoringDefinitionToAssignmentContent } from './coding-surface-bridge';

export interface LearningCodingDocumentSource {
  readonly document: unknown;
  readonly publishedAssignment: CodingAssignmentContent | null;
}

export interface LearningCodingSaveExpectation {
  readonly programId: string;
  readonly contentId: string;
  readonly revision: number;
  readonly basePublishedVersion: number;
  readonly payload: AuthoringContentPayload;
}

/** Reads only; it never upgrades or writes a stored document on mount. */
export function readLearningCodingDocument(source: LearningCodingDocumentSource): CodingAuthoringDraft {
  const document = source.document ?? source.publishedAssignment;
  if (document == null)
    return parseCodingAuthoringDraft({
      type: CODING_DRAFT_TYPE,
      schemaVersion: 1,
      exerciseType: 'standard-io',
      problem: { statement: '', examples: [], constraints: [] },
      environment: { language: 'cpp', runtime: 'wasi' },
      toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
      files: [],
      tests: [],
      validationSolutions: [],
      entryPoint: { path: '' },
      functionSignature: null,
      limits: { timeLimitMs: '5000', memoryLimitBytes: '', outputLimitBytes: '1048576', maxFileCount: '100' },
      allowLearnerCreatedFiles: false,
    });
  if (record(document) && document.type === CODING_DRAFT_TYPE) return parseCodingAuthoringDraft(document);
  const assignment = narrowCodingAssignmentContent(document);
  if (!assignment?.Execution) throw new TypeError('Coding content does not match the current authoring contract.');
  const definition = codingAssignmentToAuthoringDefinition(assignment);
  if (!definition.environment) throw new TypeError('The stored coding environment must be explicit.');
  return codingDraftOperations.fromDefinition(definition, definition.environment);
}

/** Grading configuration comes from the host, never from the coding surface. */
export function prepareLearningCodingPublication(
  source: LearningCodingDocumentSource,
  payload: AuthoringContentPayload,
  grading: GradingConfig,
): AuthoringContentPayload {
  if (payload.type !== 'Code') throw new TypeError('A Code lesson is required for coding publication.');
  if (!sameJson(source.document ?? null, payload.jsonBody ?? null)) throw new TypeError('The coding publication candidate is stale.');
  const definition = compileCodingAuthoringDraft(readLearningCodingDocument(source));
  return { ...payload, jsonBody: { ...codingAuthoringDefinitionToAssignmentContent(definition, grading) } };
}

export function assertLearningCodingSaveAcknowledgement(value: unknown, expected: LearningCodingSaveExpectation): void {
  if (
    !expected.programId.trim() ||
    !expected.contentId.trim() ||
    !Number.isSafeInteger(expected.revision) ||
    expected.revision < 1 ||
    expected.revision >= Number.MAX_SAFE_INTEGER ||
    !Number.isSafeInteger(expected.basePublishedVersion) ||
    expected.basePublishedVersion < 0 ||
    !record(value) ||
    value.programId !== expected.programId ||
    value.contentId !== expected.contentId ||
    value.revision !== expected.revision + 1 ||
    value.basePublishedVersion !== expected.basePublishedVersion ||
    !sameJson(value.payload, JSON.parse(JSON.stringify(expected.payload)))
  ) {
    throw new TypeError('The coding draft save acknowledgement does not match the sent lesson and revision.');
  }
}

export function assertLearningCodingPublicationAcknowledgement(value: unknown, expected: LearningCodingSaveExpectation): void {
  if (
    !Number.isSafeInteger(expected.basePublishedVersion) ||
    expected.basePublishedVersion < 0 ||
    !record(value) ||
    !record(value.publishedContent) ||
    value.publishedContent.id !== expected.contentId ||
    value.publishedContent.programId !== expected.programId ||
    value.publishedContent.slug !== expected.payload.slug ||
    typeof value.publishedContent.version !== 'number' ||
    !Number.isSafeInteger(value.publishedContent.version) ||
    value.publishedContent.version <= expected.basePublishedVersion
  ) {
    throw new TypeError('The coding publication acknowledgement does not match the published lesson and version.');
  }
  assertLearningCodingSaveAcknowledgement(value.draft, {
    ...expected,
    basePublishedVersion: value.publishedContent.version,
  });
}

function record(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function sameJson(left: unknown, right: unknown): boolean {
  if (left === right) return true;
  if (Array.isArray(left) && Array.isArray(right)) {
    const items: readonly unknown[] = left;
    const expected: readonly unknown[] = right;
    return items.length === expected.length && items.every((item, index) => sameJson(item, expected[index]));
  }
  if (!record(left) || !record(right)) return false;
  const keys = Object.keys(left);
  return keys.length === Object.keys(right).length && keys.every((key) => Object.hasOwn(right, key) && sameJson(left[key], right[key]));
}
