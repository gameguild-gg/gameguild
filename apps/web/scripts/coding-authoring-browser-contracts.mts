import { resolveCodingHostEnvironment } from './coding-host-environment.mjs';

/** Local-host authoring acceptance, not an academic grading verdict. */
export const AUTHORING_HOST_CASE_IDS = [
  'UX04-C-function-draft',
  'UX04-C-standard-io-draft',
  'UX04-C-function-publish',
  'UX04-C-standard-io-publish',
  'UX04-C-conflict-recovery',
  'UX04-C-failed-save',
  'UX04-C-context',
  'UX04-C-layout',
] as const;

export type AuthoringHostCaseId = (typeof AUTHORING_HOST_CASE_IDS)[number];

export interface AuthoringHostEnvironment {
  readonly webBaseUrl: string;
  readonly apiBaseUrl: string;
}

export interface AuthoringHostCaseEvidence {
  readonly id: AuthoringHostCaseId;
  readonly passed: boolean;
}

export interface AuthoringHostEvidence {
  readonly kind: 'actual-host-coding-authoring';
  readonly nextHost: true;
  readonly realApi: true;
  readonly persona: 'non-admin-course-owner';
  readonly officialGrading: false;
  readonly cases: readonly AuthoringHostCaseEvidence[];
  readonly compilerRequests: number;
  readonly academicBrowserRequests: number;
  readonly cleanupErrors: readonly string[];
  readonly errors: readonly string[];
}

export type AuthoringHostVerifier = (evidence: AuthoringHostEvidence) => void;

export function resolveAuthoringHostEnvironment(environment: Readonly<Record<string, string | undefined>>): AuthoringHostEnvironment {
  return resolveCodingHostEnvironment(environment);
}

export function assertAuthoringHostEvidence(evidence: AuthoringHostEvidence): void {
  if (
    evidence.cases.length !== AUTHORING_HOST_CASE_IDS.length ||
    AUTHORING_HOST_CASE_IDS.some((id) => evidence.cases.filter((entry) => entry.id === id && entry.passed).length !== 1)
  ) {
    throw new TypeError('Every required authoring case must pass exactly once.');
  }
  if (evidence.compilerRequests !== 0) throw new TypeError('Authoring form editing started a compiler.');
  if (evidence.academicBrowserRequests !== 0) throw new TypeError('Authoring issued an academic browser command.');
  if (evidence.errors.length) throw new TypeError('The authoring proof recorded an error.');
  if (evidence.cleanupErrors.length) throw new TypeError('Owned fixture cleanup failed.');
}
