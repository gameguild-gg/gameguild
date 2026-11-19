import type {
  CodingAuthoringDefinitionV2,
  CodingExerciseTypeV2,
  CodingLearnerDeliveryV2,
  CodingSubmissionV1,
} from '@game-guild/coding';
import type { IdeController, IdeExtension, WorkspaceConfig } from '@gameguild/emception-ide';
import type { TestReport } from 'emception';
import type { ScoreResult } from 'emception/testing';
import type { ComponentType, ReactNode } from 'react';

export type CodingSurfaceMode = 'author' | 'author-test' | 'learner' | 'grader';
export type CodingSurfaceStatus = 'ready' | 'loading' | 'empty' | 'error' | 'unavailable';
export type CodingSurfaceDefinition = CodingAuthoringDefinitionV2 | CodingLearnerDeliveryV2;

export interface CodingPreviewResult {
  readonly scope: 'public' | 'full';
  readonly report: TestReport;
  readonly score: ScoreResult;
  readonly academic: false;
}

export interface CodingSurfaceProfile {
  readonly type: CodingExerciseTypeV2;
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
  readonly definition: CodingAuthoringDefinitionV2;
}

export interface CodingLearnerProfileRendererProps {
  readonly definition: CodingLearnerDeliveryV2;
}

export interface CodingSurfaceProfileRenderers {
  readonly author: ComponentType<CodingFullProfileRendererProps>;
  readonly 'author-test': ComponentType<CodingFullProfileRendererProps>;
  readonly learner: ComponentType<CodingLearnerProfileRendererProps>;
  readonly grader: ComponentType<CodingFullProfileRendererProps>;
}

interface CodingSurfaceCommonProps {
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
  readonly extensions?: readonly IdeExtension[];
  readonly header?: ReactNode;
  readonly onReady?: (controller: IdeController) => void;
  readonly onSessionReady?: (session: CodingSurfaceSession) => void;
  readonly onPreviewRun?: (result: CodingPreviewResult) => void;
}

export interface CodingAuthorSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'author';
  readonly definition: CodingAuthoringDefinitionV2;
  readonly onDefinitionChange?: (definition: CodingAuthoringDefinitionV2) => void;
}

export interface CodingAuthorTestSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'author-test';
  readonly definition: CodingAuthoringDefinitionV2;
}

export interface CodingLearnerSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'learner';
  readonly definition: CodingLearnerDeliveryV2;
  readonly onSubmit?: (submission: CodingSubmissionV1) => void | Promise<void>;
}

export interface CodingGraderSurfaceProps extends CodingSurfaceCommonProps {
  readonly mode: 'grader';
  readonly definition: CodingAuthoringDefinitionV2;
  readonly onReviewDecision?: (decision: 'approve' | 'request-changes') => void;
}

export type CodingSurfaceProps =
  | CodingAuthorSurfaceProps
  | CodingAuthorTestSurfaceProps
  | CodingLearnerSurfaceProps
  | CodingGraderSurfaceProps;

export interface CodingSurfaceSession {
  readonly status: 'ready' | 'running' | 'failed';
  readonly result: CodingPreviewResult | null;
  run(options?: { readonly signal?: AbortSignal }): Promise<CodingPreviewResult>;
  getSubmission(): Promise<CodingSubmissionV1>;
}
