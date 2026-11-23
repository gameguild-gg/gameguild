import {
  CODING_SUBMISSION_SCHEMA_VERSION,
  normalizeCodingPath,
  parseCodingSubmission,
  type CodingAuthoringDefinition,
  type CodingLearnerDelivery,
  type CodingSubmission,
} from '@game-guild/coding';
import type { IdeController, WorkspaceFile } from '@gameguild/emception-ide';
import { computeScore, withWorkspaceOverlay } from 'emception/testing';
import { buildCodingExecutionPlan } from './execution-plan';
import type { CodingPreviewResult, CodingSurfaceMode, CodingSurfaceSession } from './types';

export interface CreateCodingSurfaceSessionOptions {
  readonly controller: Pick<IdeController, 'getFiles' | 'syncWorkspace'> & {
    readonly api: {
      readonly workspace: Pick<IdeController['api']['workspace'], 'readFile' | 'writeFile' | 'deleteFile'>;
      readonly runTests: IdeController['api']['runTests'];
    };
  };
  readonly definition: CodingAuthoringDefinition | CodingLearnerDelivery;
  readonly mode: CodingSurfaceMode;
  readonly maxScore?: number;
  readonly passingScore?: number;
  readonly onResult?: (result: CodingPreviewResult) => void;
}

export function createCodingSurfaceSession(options: CreateCodingSurfaceSessionOptions): CodingSurfaceSession {
  let status: CodingSurfaceSession['status'] = 'ready';
  let result: CodingPreviewResult | null = null;
  return {
    get status() {
      return status;
    },
    get result() {
      return result;
    },
    async run(runOptions = {}) {
      if (status === 'running') throw new Error('A coding preview is already running.');
      status = 'running';
      try {
        await options.controller.syncWorkspace();
        const scope = options.mode === 'learner' ? 'public' : 'full';
        const execution = buildCodingExecutionPlan(options.definition, scope);
        const report = await withWorkspaceOverlay(options.controller.api.workspace, execution.overlay, () =>
          options.controller.api.runTests(execution.plan, { signal: runOptions.signal }),
        );
        result = {
          scope,
          report,
          score: computeScore(report, execution.plan, options.maxScore ?? 100, options.passingScore ?? 60),
          academic: false,
        };
        status = 'ready';
        options.onResult?.(result);
        return result;
      } catch (error) {
        status = 'failed';
        throw error;
      }
    },
    async getSubmission() {
      return captureCodingSubmission(options.definition, await options.controller.getFiles());
    },
  };
}

export function captureCodingSubmission(definition: CodingAuthoringDefinition | CodingLearnerDelivery, files: readonly WorkspaceFile[]): CodingSubmission {
  const authoredByPath = new Map(definition.files.map((file) => [normalizeCodingPath(file.path), file]));
  const submissionFiles = files.filter(isTextFile).flatMap((file) => {
    const path = normalizeCodingPath(file.path);
    const authored = authoredByPath.get(path);
    if (authored) {
      if ('visibility' in authored && authored.visibility === 'private') return [];
      if (!authored.mutable || file.content === authored.content) return [];
      return [{ fileId: authored.id, path, encoding: 'text' as const, content: file.content }];
    }
    if (!definition.allowLearnerCreatedFiles) return [];
    return [{ fileId: stableFileId(path), path, encoding: 'text' as const, content: file.content }];
  });
  return parseCodingSubmission({ schemaVersion: CODING_SUBMISSION_SCHEMA_VERSION, files: submissionFiles });
}

function isTextFile(file: WorkspaceFile): file is WorkspaceFile & { type: 'text' } {
  return file.type === 'text';
}

function stableFileId(path: string): string {
  return `learner-${path.replace(/^\/home\/user\//, '').replace(/[^A-Za-z0-9._-]+/g, '-')}`.slice(0, 128);
}
