import type { CodingAuthoringDefinition, CodingExerciseType, CodingLearnerDelivery, CodingSubmission } from '@game-guild/coding';
import type { WorkspaceConfig, WorkspaceFile } from '@gameguild/emception-ide';
import type { TestReport } from 'emception';
import type { ScoreResult } from 'emception/testing';
import type { ComponentType, ReactNode } from 'react';

export type CodingSurfaceMode = 'author' | 'author-test' | 'learner' | 'grader';
export type CodingSurfaceStatus = 'ready' | 'loading' | 'empty' | 'error' | 'unavailable';
export type CodingSurfaceDefinition = CodingAuthoringDefinition | CodingLearnerDelivery;

/** Editing/saving has no dependency on a compiler, worker or runtime API. */
export interface CodingEditorController {
  getFiles(): Promise<readonly WorkspaceFile[]>;
  replaceFiles(files: readonly WorkspaceFile[]): Promise<void>;
  setFilesReadOnly(paths: readonly string[], readOnly: boolean): void;
}

export interface CodingSurfaceExtension {
  readonly id: string;
  readonly toolbarEnd?: (editor: CodingEditorController) => ReactNode;
  readonly explorerFooter?: (editor: CodingEditorController) => ReactNode;
  readonly bottomPanel?: (editor: CodingEditorController) => ReactNode;
  readonly onReady?: (editor: CodingEditorController) => void | (() => void);
}

export interface CodingPreviewResult {
  readonly scope: 'public' | 'full';
  readonly report: TestReport;
  readonly score: ScoreResult;
  readonly academic: false;
}

export interface CodingSurfaceProfile {
  readonly type: CodingExerciseType;
  readonly label: string;
  readonly description: string;
  readonly capabilities: {
    readonly files: true;
    readonly terminal: boolean;
    readonly testResults: true;
    readonly preview: boolean;
  };
  readonly guidance: Readonly<Record<CodingSurfaceMode, string>>;
  readonly renderers: CodingSurfaceProfileRenderers;
}

export interface CodingFullProfileRendererProps {
  readonly definition: CodingAuthoringDefinition;
}

export interface CodingLearnerProfileRendererProps {
  readonly definition: CodingLearnerDelivery;
}

export interface CodingSurfaceProfileRenderers {
  readonly author: ComponentType<CodingFullProfileRendererProps>;
  readonly 'author-test': ComponentType<CodingFullProfileRendererProps>;
  readonly learner: ComponentType<CodingLearnerProfileRendererProps>;
  readonly grader: ComponentType<CodingFullProfileRendererProps>;
}

interface CodingSurfaceCommonProps {
  /** Change when the authenticated attempt/revision/inspection context changes. */
  readonly contextKey?: string;
  readonly title?: string;
  readonly manifestUrl?: string;
  readonly workspaceConfig?: WorkspaceConfig;
  readonly workspaceStorageKey?: string;
  readonly enableWorkspace?: boolean;
  readonly maxScore?: number;
  readonly passingScore?: number;
  readonly readOnly?: boolean;
  readonly status?: CodingSurfaceStatus;
  readonly statusMessage?: string;
  readonly extensions?: readonly CodingSurfaceExtension[];
  readonly header?: ReactNode;
  readonly onReady?: (controller: CodingEditorController) => void;
  readonly onSessionReady?: (session: CodingSurfaceSession) => void;
  readonly onPreviewRun?: (result: CodingPreviewResult) => void;
  readonly onPreviewInvalidated?: () => void;
}

export interface CodingAuthorSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'author';
  readonly definition: CodingAuthoringDefinition;
  readonly onDefinitionChange?: (definition: CodingAuthoringDefinition) => void;
}

export interface CodingAuthorTestSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'author-test';
  readonly definition: CodingAuthoringDefinition;
}

export interface CodingLearnerSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'learner';
  readonly definition: CodingLearnerDelivery;
  readonly onSubmit?: (submission: CodingSubmission) => void | Promise<void>;
}

export interface CodingGraderSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'grader';
  readonly definition: CodingAuthoringDefinition;
  readonly onReviewDecision?: (decision: 'approve' | 'request-changes') => void;
}

export type CodingSurfaceProps = CodingAuthorSurfaceProps | CodingAuthorTestSurfaceProps | CodingLearnerSurfaceProps | CodingGraderSurfaceProps;

export interface CodingSurfaceSession {
  readonly status: 'ready' | 'running' | 'failed';
  readonly result: CodingPreviewResult | null;
  run(options?: { readonly signal?: AbortSignal }): Promise<CodingPreviewResult>;
  getSubmission(): Promise<CodingSubmission>;
}
