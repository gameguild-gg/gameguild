import type { AuthoringActionResult, AuthoringContentPayload } from '@/lib/learning/authoring';
import { getAuthoringDraft, saveAuthoringDraft } from '@/lib/learning/authoring';
import { parseCodingAuthoringDraft } from '@game-guild/coding';
import type { CodingAuthoringDraft } from '@game-guild/coding';
import type { CodingAuthoringScope, CodingDraftLoadResult, CodingDraftPersistencePort } from '@game-guild/coding-surface';

export interface LearningCodingDraftActions {
  load(courseId: string, contentId: string): Promise<AuthoringActionResult<unknown>>;
  save(courseId: string, contentId: string, expectedRevision: number, payload: AuthoringContentPayload): Promise<AuthoringActionResult<unknown>>;
}

export interface LearningCodingDraftPersistenceOptions {
  readonly scope: CodingAuthoringScope;
  /** One revision-bound read from the lesson coordinator; never cached inside the surface. */
  readonly getPayload: () => { readonly revision: number; readonly payload: AuthoringContentPayload };
}

export interface LearningCodingDraftPersistenceFactory {
  (options: LearningCodingDraftPersistenceOptions, actions?: LearningCodingDraftActions): CodingDraftPersistencePort;
}

export const createLearningCodingDraftPersistence: LearningCodingDraftPersistenceFactory = (
  options,
  actions = { load: getAuthoringDraft, save: saveAuthoringDraft },
) => {
  const { tenantId, actorId, courseId, contentId } = options.scope;
  const scope = { tenantId, actorId, courseId, contentId };
  if ([tenantId, actorId, courseId, contentId].some((value) => typeof value !== 'string' || !value.trim()))
    throw new TypeError('Coding authoring scope must be complete.');

  return {
    scope: Object.freeze(scope),
    async load() {
      try {
        const response = await actions.load(scope.courseId, scope.contentId);
        if (!response.success) return failure(response.status);
        return decode(response.data, scope);
      } catch {
        return { status: 'error', reason: 'unavailable' };
      }
    },
    async save(request) {
      let candidate: CodingAuthoringDraft;
      let payload: AuthoringContentPayload;
      try {
        if (!validRevision(request.expectedRevision, 1) || request.expectedRevision === Number.MAX_SAFE_INTEGER)
          throw new TypeError('Invalid expected revision.');
        candidate = parseCodingAuthoringDraft(request.draft);
        const hostDraft = structuredClone(options.getPayload());
        if (hostDraft.revision !== request.expectedRevision) throw new TypeError('Host metadata and coding draft revisions must match.');
        const metadata = hostDraft.payload;
        if (metadata.type !== 'Code') throw new TypeError('A Code lesson payload is required.');
        payload = { ...metadata, jsonBody: { ...candidate } };
        JSON.stringify(payload);
      } catch {
        return { status: 'error', reason: 'invalid-data' };
      }
      try {
        const response = await actions.save(scope.courseId, scope.contentId, request.expectedRevision, payload);
        if (!response.success) {
          if (response.status === 409)
            return { status: 'conflict', currentRevision: validRevision(response.currentRevision, 1) ? response.currentRevision : null };
          return failure(response.status);
        }
        const decoded = decode(response.data, scope);
        if (decoded.status === 'error') return decoded;
        if (decoded.snapshot.revision !== request.expectedRevision + 1 || JSON.stringify(decoded.snapshot.draft) !== JSON.stringify(candidate)) {
          return { status: 'error', reason: 'invalid-data' };
        }
        return { status: 'saved', snapshot: decoded.snapshot };
      } catch {
        return { status: 'error', reason: 'unavailable' };
      }
    },
  };
};

function decode(value: unknown, scope: CodingAuthoringScope): CodingDraftLoadResult {
  if (
    !record(value) ||
    value.programId !== scope.courseId ||
    value.contentId !== scope.contentId ||
    !validRevision(value.revision, 1) ||
    !validRevision(value.basePublishedVersion, 0) ||
    !record(value.payload) ||
    value.payload.type !== 'Code'
  )
    return { status: 'error', reason: 'invalid-data' };
  try {
    return {
      status: 'loaded',
      snapshot: { revision: value.revision, basePublishedVersion: value.basePublishedVersion, draft: parseCodingAuthoringDraft(value.payload.jsonBody) },
    };
  } catch {
    return { status: 'error', reason: 'invalid-data' };
  }
}

function failure(status: number): { status: 'error'; reason: 'forbidden' | 'unavailable' } {
  return { status: 'error', reason: status === 401 || status === 403 ? 'forbidden' : 'unavailable' };
}

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function validRevision(value: unknown, minimum: number): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= minimum;
}
