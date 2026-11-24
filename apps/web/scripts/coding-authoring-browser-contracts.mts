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
  if (environment.E2E_RUN !== '1') throw new TypeError('E2E_RUN=1 is required for the authoring host proof.');
  return {
    webBaseUrl: localOrigin(environment.PLAYWRIGHT_WEB_BASE_URL ?? 'http://localhost:3000'),
    apiBaseUrl: localOrigin(environment.API_BASE_URL ?? 'http://localhost:8080'),
  };
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

function localOrigin(value: string): string {
  const url = new URL(value);
  if (
    !['http:', 'https:'].includes(url.protocol) ||
    !(url.hostname === 'localhost' || url.hostname.endsWith('.localhost') || url.hostname === '127.0.0.1' || url.hostname === '[::1]') ||
    url.username ||
    url.password ||
    url.pathname !== '/' ||
    url.search ||
    url.hash
  ) {
    throw new TypeError('The authoring proof requires an uncredentialed loopback origin.');
  }
  return url.origin;
}
