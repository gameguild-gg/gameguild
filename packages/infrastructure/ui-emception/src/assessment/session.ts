import type { IdeController, WorkspaceFile } from '@gameguild/emception-ide';
import type { TestReport } from 'emception';
import { computeScore, withWorkspaceOverlay, type ScoreResult } from 'emception/testing';

import { buildAssessmentExecutionPlan, type AssessmentTestScope } from './plan';
import type { CodingAssessmentDefinition } from './types';

export type AssessmentEditorMode = 'author' | 'learner' | 'grader';
export type AssessmentSessionStatus = 'booting' | 'ready' | 'running' | 'failed';

export interface AssessmentFile {
  readonly path: string;
  readonly content: string;
}

/** The controller subset required for assessment execution. */
export type AssessmentController = Pick<IdeController, 'getFiles'> & {
  readonly api: {
    readonly workspace: Pick<IdeController['api']['workspace'], 'readFile' | 'writeFile' | 'deleteFile'>;
    readonly runTests: IdeController['api']['runTests'];
  };
};

export interface AssessmentRunResult {
  readonly scope: AssessmentTestScope;
  readonly report: TestReport;
  readonly score: ScoreResult;
  /** Consumers can use this to avoid rendering instructor-only diagnostics. */
  readonly diagnosticVisibility: 'public' | 'full';
}

export interface AssessmentSessionOptions {
  readonly controller: AssessmentController;
  readonly definition: CodingAssessmentDefinition;
  readonly mode: AssessmentEditorMode;
  readonly maxScore?: number;
  readonly passingScore?: number;
  readonly onResult?: (result: AssessmentRunResult) => void;
}

export interface AssessmentSession {
  readonly status: AssessmentSessionStatus;
  readonly report: TestReport | null;
  readonly result: AssessmentRunResult | null;
  run(scope: AssessmentTestScope, options?: { readonly signal?: AbortSignal }): Promise<AssessmentRunResult>;
  getSubmissionDelta(): Promise<readonly AssessmentFile[]>;
}

function isTextFile(file: WorkspaceFile): boolean {
  return file.type === 'text';
}

/**
 * Creates the single canonical assessment executor.
 *
 * The function deliberately has no React or layout dependency: a visual host
 * supplies the public controller from the vanilla IDE, while this session owns
 * the data policy, transient VFS material and score computation.
 */
export function createAssessmentSession(options: AssessmentSessionOptions): AssessmentSession {
  let status: AssessmentSessionStatus = 'ready';
  let report: TestReport | null = null;
  let result: AssessmentRunResult | null = null;

  return {
    get status() {
      return status;
    },
    get report() {
      return report;
    },
    get result() {
      return result;
    },
    async run(scope, runOptions = {}) {
      if (options.mode === 'learner' && scope !== 'public') {
        throw new Error('Learner sessions may only run public tests');
      }
      if (status === 'running') {
        throw new Error('An assessment run is already in progress');
      }

      status = 'running';
      try {
        const execution = buildAssessmentExecutionPlan(options.definition, scope);
        const nextReport = await withWorkspaceOverlay(
          options.controller.api.workspace,
          execution.overlay,
          () => options.controller.api.runTests(execution.plan, { signal: runOptions.signal }),
        );
        const score = computeScore(
          nextReport,
          execution.plan,
          options.maxScore ?? 100,
          options.passingScore ?? 60,
        );
        const nextResult: AssessmentRunResult = {
          scope,
          report: nextReport,
          score,
          diagnosticVisibility: scope === 'public' ? 'public' : 'full',
        };

        report = nextReport;
        result = nextResult;
        status = 'ready';
        options.onResult?.(nextResult);
        return nextResult;
      } catch (error) {
        status = 'failed';
        throw error;
      }
    },
    async getSubmissionDelta() {
      const visibleFiles = await options.controller.getFiles();
      const definitionFiles = options.definition.Data.Files;
      const allowNewFiles = options.definition.Environment.AllowStudentCreateFiles !== false;
      const delta: AssessmentFile[] = [];

      for (const file of visibleFiles) {
        if (!isTextFile(file)) continue;
        const original = definitionFiles[file.path];
        if (!original) {
          if (allowNewFiles) delta.push({ path: file.path, content: file.content });
          continue;
        }
        if (original.Visibility === 'Private' || original.Modifiable === false) continue;
        if (file.content !== original.Content) {
          delta.push({ path: file.path, content: file.content });
        }
      }

      return delta;
    },
  };
}
