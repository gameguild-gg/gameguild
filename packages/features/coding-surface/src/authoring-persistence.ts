import { parseCodingAuthoringDraft } from '@game-guild/coding';
import type {
  CodingAuthoringScope,
  CodingDraftFailureReason,
  CodingDraftPersistencePort,
  CodingDraftSession,
  CodingDraftSessionState,
  CodingDraftSnapshot,
} from './authoring-persistence-types';

export function parseCodingAuthoringScope(value: unknown): CodingAuthoringScope {
  if (!record(value)) throw new TypeError('Coding authoring scope is required.');
  const { tenantId, actorId, courseId, contentId } = value;
  if (!identity(tenantId) || !identity(actorId) || !identity(courseId) || !identity(contentId)) {
    throw new TypeError('Coding authoring scope must be complete.');
  }
  return { tenantId, actorId, courseId, contentId };
}

export function parseCodingDraftSnapshot(value: unknown): CodingDraftSnapshot {
  if (!record(value) || !revision(value.revision, 1) || !revision(value.basePublishedVersion, 0)) {
    throw new TypeError('Coding draft revisions must be safe non-negative integers; draft revision starts at one.');
  }
  return { revision: value.revision, basePublishedVersion: value.basePublishedVersion, draft: parseCodingAuthoringDraft(value.draft) };
}

export function createCodingDraftSession(port: CodingDraftPersistencePort, initial: CodingDraftSnapshot): CodingDraftSession {
  const scope = parseCodingAuthoringScope(port.scope);
  const snapshot = parseCodingDraftSnapshot(initial);
  let state: CodingDraftSessionState | null = { snapshot, draft: structuredClone(snapshot.draft), status: 'saved', error: null, currentRevision: null };
  let pending: Promise<void> | null = null;
  let edits = 0;
  let blockedByConflict = false;

  function current(): CodingDraftSessionState {
    try {
      if (!sameScope(scope, parseCodingAuthoringScope(port.scope))) state = null;
    } catch {
      state = null;
    }
    if (!state) throw new TypeError('Coding authoring session is closed.');
    return state;
  }

  function failed(reason: CodingDraftFailureReason): void {
    if (state) state = { ...state, status: 'error', error: reason };
  }

  async function write(sent: CodingDraftSnapshot): Promise<void> {
    try {
      const result = await port.save({ expectedRevision: sent.revision, draft: structuredClone(sent.draft) });
      if (!state) return;
      current();
      if (result.status === 'error') {
        failed(result.reason);
        return;
      }
      if (result.status === 'conflict') {
        blockedByConflict = true;
        state = { ...state, status: 'conflict', error: null, currentRevision: result.currentRevision };
        return;
      }
      const ack = checkedSnapshot(result.snapshot);
      if (!ack || ack.revision !== sent.revision + 1 || ack.basePublishedVersion !== sent.basePublishedVersion || !sameDraft(ack.draft, sent.draft)) {
        failed('invalid-data');
        return;
      }
      state = { ...state, snapshot: ack, status: sameDraft(state.draft, ack.draft) ? 'saved' : 'unsaved', error: null, currentRevision: null };
    } catch {
      failed('unavailable');
    }
  }

  async function read(editVersion: number): Promise<void> {
    try {
      const result = await port.load();
      if (!state) return;
      current();
      if (result.status === 'error') {
        failed(result.reason);
        return;
      }
      const ack = checkedSnapshot(result.snapshot);
      if (
        !ack ||
        ack.revision < state.snapshot.revision ||
        ack.basePublishedVersion < state.snapshot.basePublishedVersion ||
        (ack.revision === state.snapshot.revision &&
          (ack.basePublishedVersion !== state.snapshot.basePublishedVersion || !sameDraft(ack.draft, state.snapshot.draft)))
      ) {
        failed('invalid-data');
      } else if (editVersion !== edits) {
        blockedByConflict = true;
        state = { ...state, status: 'conflict', error: null, currentRevision: ack.revision };
      } else {
        blockedByConflict = false;
        state = { snapshot: ack, draft: structuredClone(ack.draft), status: 'saved', error: null, currentRevision: null };
      }
    } catch {
      failed('unavailable');
    }
  }

  function edit(draft: CodingDraftSnapshot['draft']): void {
    const previous = current();
    const parsed = parseCodingAuthoringDraft(draft);
    edits += 1;
    const status = pending ? previous.status : blockedByConflict ? 'conflict' : sameDraft(previous.snapshot.draft, parsed) ? 'saved' : 'unsaved';
    state = { ...previous, draft: parsed, status, error: null };
  }

  return {
    getState() {
      return structuredClone(current());
    },
    edit,
    save() {
      const previous = current();
      if (pending) return pending;
      if (blockedByConflict || previous.status === 'saved') return Promise.resolve();
      const sent = parseCodingDraftSnapshot({ ...previous.snapshot, draft: previous.draft });
      state = { ...previous, status: 'saving', error: null };
      pending = write(sent).finally(() => {
        pending = null;
      });
      return pending;
    },
    reload(options) {
      const previous = current();
      if (options.discardLocalChanges !== true || pending) throw new TypeError('Explicit discard and no pending operation are required to reload.');
      state = { ...previous, status: 'loading', error: null };
      pending = read(edits).finally(() => {
        pending = null;
      });
      return pending;
    },
    checkpoint() {
      const previous = current();
      return {
        kind: 'coding-authoring-memory-recovery',
        scope: { ...scope },
        snapshot: parseCodingDraftSnapshot({ ...previous.snapshot, draft: previous.draft }),
      };
    },
    restore(recovery) {
      const previous = current();
      if (pending) throw new TypeError('Cannot restore during a pending operation.');
      if (recovery.kind !== 'coding-authoring-memory-recovery' || !sameScope(scope, parseCodingAuthoringScope(recovery.scope))) {
        throw new TypeError('Coding recovery scope does not match.');
      }
      const recovered = parseCodingDraftSnapshot(recovery.snapshot);
      if (recovered.revision !== previous.snapshot.revision || recovered.basePublishedVersion !== previous.snapshot.basePublishedVersion) {
        throw new TypeError('Coding recovery revisions do not match.');
      }
      edit(recovered.draft);
    },
    dispose() {
      state = null;
    },
  };
}

function sameDraft(left: CodingDraftSnapshot['draft'], right: CodingDraftSnapshot['draft']): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function checkedSnapshot(value: unknown): CodingDraftSnapshot | null {
  try {
    return parseCodingDraftSnapshot(value);
  } catch {
    return null;
  }
}

function sameScope(left: CodingAuthoringScope, right: CodingAuthoringScope): boolean {
  return left.tenantId === right.tenantId && left.actorId === right.actorId && left.courseId === right.courseId && left.contentId === right.contentId;
}

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function identity(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}
function revision(value: unknown, minimum: number): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= minimum;
}
