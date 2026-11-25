import type { AuthoringHostEnvironment } from './coding-authoring-browser-contracts.mjs';
import { resolveCodingHostEnvironment } from './coding-host-environment.mjs';

export const LEARNER_HOST_CASE_IDS = [
  'UX05-A3-standard-io-solve',
  'UX05-A3-function-solve',
  'UX05-A3-preview-isolation',
  'UX05-A3-scratch-isolation',
  'UX05-A3-stale-cancel',
  'UX05-A3-context-isolation',
  'UX05-A3-layouts',
  'UX05-A3-runtime-cleanup',
] as const;

export type LearnerHostCaseId = (typeof LEARNER_HOST_CASE_IDS)[number];
export type LearnerHostEnvironment = AuthoringHostEnvironment;

export interface LearnerHostCaseEvidence {
  readonly id: LearnerHostCaseId;
  readonly passed: boolean;
}

/** Evidence of local solving, not submission, recovery or released grading. */
export interface LearnerHostEvidence {
  readonly kind: 'actual-host-coding-solving';
  readonly nextHost: true;
  readonly realApi: true;
  readonly authentication: 'real-browser-credentials-route';
  readonly personas: readonly ['non-admin-course-owner', 'enrolled-member', 'independent-enrolled-member'];
  readonly officialGrading: false;
  readonly cases: readonly LearnerHostCaseEvidence[];
  readonly academicMutationsDuringPractice: number;
  readonly authoringMutationsDuringPractice: number;
  readonly privateLeaks: number;
  readonly activeRuntimeWorkers: number;
  readonly errors: readonly string[];
  readonly cleanupErrors: readonly string[];
}

export type LearnerHostVerifier = (evidence: LearnerHostEvidence) => void;

export function resolveLearnerHostEnvironment(environment: Readonly<Record<string, string | undefined>>): LearnerHostEnvironment {
  return resolveCodingHostEnvironment(environment);
}

export function assertLearnerHostEvidence(evidence: LearnerHostEvidence): void {
  if (
    evidence.kind !== 'actual-host-coding-solving' ||
    evidence.nextHost !== true ||
    evidence.realApi !== true ||
    evidence.officialGrading !== false ||
    evidence.authentication !== 'real-browser-credentials-route'
  )
    throw new TypeError('The solving proof has an invalid acceptance scope.');
  const personas = ['non-admin-course-owner', 'enrolled-member', 'independent-enrolled-member'];
  if (evidence.personas.length !== personas.length || personas.some((persona, index) => evidence.personas[index] !== persona))
    throw new TypeError('The solving proof requires independent non-admin personas.');
  if (
    evidence.cases.length !== LEARNER_HOST_CASE_IDS.length ||
    LEARNER_HOST_CASE_IDS.some((id) => evidence.cases.filter((entry) => entry.id === id && entry.passed === true).length !== 1)
  )
    throw new TypeError('Every required solving host case must pass exactly once.');
  if (evidence.academicMutationsDuringPractice !== 0 || evidence.authoringMutationsDuringPractice !== 0)
    throw new TypeError('Local practice caused an academic or authoring mutation.');
  if (evidence.privateLeaks !== 0) throw new TypeError('The solving proof recorded a private leak.');
  if (evidence.activeRuntimeWorkers !== 0) throw new TypeError('The solving proof left an active runtime worker.');
  if (evidence.errors.length) throw new TypeError('The solving proof recorded an error.');
  if (evidence.cleanupErrors.length) throw new TypeError('Owned solving fixture cleanup failed.');
}
