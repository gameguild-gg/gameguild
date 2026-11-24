import type { CodingDraftScalar, CodingExerciseType, CodingFunctionSignature, CodingLearnerDelivery } from '@game-guild/coding';

interface CodingPracticeCaseBase {
  readonly id: string;
  readonly name: string;
}

export type CodingPracticeCase = CodingPracticeCaseBase &
  (
    | {
        readonly kind: 'stdio';
        readonly stdin: string;
        readonly expected: { readonly stdout: string | null; readonly stderr: string | null; readonly exitCode: string | null };
      }
    | {
        readonly kind: 'function';
        readonly inputs: readonly Readonly<CodingDraftScalar>[];
        readonly expected: Readonly<CodingDraftScalar> | null;
      }
  );

/** Personal buffers carry no grading weight, visibility or official identity. */
export type CodingPersonalCase = CodingPracticeCase & { readonly source: 'personal' };
export type CodingPublicPracticeCase = CodingPracticeCase & { readonly source: 'public' };

export type CodingPracticeSelection = { readonly source: 'public'; readonly id: string } | { readonly source: 'personal'; readonly id: string };

export interface CodingLearnerPracticeState {
  readonly contextKey: string;
  readonly exerciseType: CodingExerciseType;
  readonly signature:
    | (Readonly<Omit<CodingFunctionSignature, 'parameters'>> & {
        readonly parameters: readonly Readonly<CodingFunctionSignature['parameters'][number]>[];
      })
    | null;
  readonly publicCases: readonly CodingPublicPracticeCase[];
  readonly personalCases: readonly CodingPersonalCase[];
  readonly view: 'public' | 'personal';
  readonly selection: CodingPracticeSelection | null;
  readonly pendingDeletion: string | null;
}

export type CodingLearnerPracticeIntent =
  | { readonly kind: 'view'; readonly view: 'public' | 'personal' }
  | { readonly kind: 'select'; readonly selection: CodingPracticeSelection }
  | { readonly kind: 'add'; readonly id: string }
  | { readonly kind: 'duplicate'; readonly id: string; readonly selection: CodingPracticeSelection }
  | { readonly kind: 'replace'; readonly value: CodingPersonalCase }
  | { readonly kind: 'request-delete'; readonly id: string }
  | { readonly kind: 'cancel-delete' }
  | { readonly kind: 'confirm-delete' };

/** Editing is controlled and offline; execution and persistence are separate ports. */
export interface CodingLearnerCasesProps {
  readonly state: CodingLearnerPracticeState;
  readonly onChange: (state: CodingLearnerPracticeState) => void;
  readonly createId: () => string;
  readonly disabled?: boolean;
}

export interface CodingLearnerPracticeOperations {
  create(contextKey: string, delivery: CodingLearnerDelivery): CodingLearnerPracticeState;
  reduce(state: CodingLearnerPracticeState, intent: CodingLearnerPracticeIntent): CodingLearnerPracticeState;
}
