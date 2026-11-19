'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type IdentityAuthenticationBackupCodesStatusOutput,
  type IdentityAuthenticationMfaConfigurationOutput,
  type IdentityAuthenticationMfaMethodsOutput,
  type IdentityAuthenticationMfaSetupOutput,
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server actions for the MFA method-management surface (settings/security, issue #244).
 *
 * Mirrors sessions-actions.ts: bearer via createServerClient + getToken; every mutation
 * revalidates the security page so the method list, recovery-code status and profile state
 * reflect server state after each action.
 */

const SETTINGS_SECURITY_PATH = '/workspace/settings/security';

function getApiClient() {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

export interface MfaPageData {
  configuration: IdentityAuthenticationMfaConfigurationOutput | null;
  methods: IdentityAuthenticationMfaMethodsOutput | null;
  backupCodes: IdentityAuthenticationBackupCodesStatusOutput | null;
}

/**
 * Aggregate load outcome for the MFA half of the security page, mirroring
 * `SessionsLoadResult` (issue #1298 J4): failures surface as an explicit error
 * state instead of the silently blank method/recovery panels the old `safeLoad`
 * produced. `'ok'` means every read succeeded.
 */
export type MfaLoadStatus = 'ok' | 'unauthorized' | 'notFound' | 'serverError' | 'contractError';

/** {@link MfaPageData} plus the aggregate load outcome of its three reads. */
export interface MfaLoadResult extends MfaPageData {
  loadStatus: MfaLoadStatus;
}

export type MfaActionStatus = 'success' | 'notFound' | 'unauthorized' | 'error';

export interface MfaActionResult {
  success: boolean;
  status: MfaActionStatus;
  message?: string;
}

/** Result of starting an enrollment: provisioning data (TOTP) or delivery metadata (SMS). */
export interface MfaEnrollmentStartResult extends MfaActionResult {
  setup?: IdentityAuthenticationMfaSetupOutput;
  smsSetup?: {
    message?: string | null;
    phoneNumberMasked?: string | null;
    expiresInSeconds?: number;
  };
}

/** Result of regenerating recovery codes: the fresh codes, shown exactly once. */
export interface MfaBackupCodesResult extends MfaActionResult {
  codes?: string[];
}

function statusFromError(error: { status?: number } | undefined): MfaActionStatus {
  switch (error?.status) {
    case 401:
      return 'unauthorized';
    case 404:
      return 'notFound';
    default:
      return 'error';
  }
}

function resultFrom(success: boolean, error?: { status?: number }, message?: string): MfaActionResult {
  return success
    ? { success: true, status: 'success', message }
    : { success: false, status: statusFromError(error) };
}

function errorMessage(error?: { message?: string; detail?: string }): string | undefined {
  const message = error?.detail ?? error?.message;
  return message && message.length > 0 ? message : undefined;
}

function loadStatusFromError(error: { status?: number } | undefined): MfaLoadStatus {
  switch (error?.status) {
    case 401:
      return 'unauthorized';
    case 404:
      return 'notFound';
    default:
      return 'serverError';
  }
}

/**
 * Load wrapper that surfaces failures instead of degrading to a fallback
 * (issue #1298 J4): a thrown safeParse/transport error is contract drift or an
 * unreachable API, a non-ok result carries the HTTP status — both become a load
 * status the page renders as an explicit error state with retry, never as empty
 * MFA panels that look like "MFA not configured".
 */
async function attemptLoad<T>(call: () => Promise<{ ok: boolean; data?: T; error?: { status?: number } }>): Promise<{
  data: T | null;
  status: MfaLoadStatus;
}> {
  try {
    const result = await call();
    if (!result.ok) {
      return { data: null, status: loadStatusFromError(result.error) };
    }
    return { data: result.data ?? null, status: 'ok' };
  } catch {
    // Contract drift between the deployed API and the generated client (issue
    // #864), or an unreachable API: report it instead of blanking the page.
    return { data: null, status: 'contractError' };
  }
}

/** Most user-actionable status wins, so the retry state explains the failure. */
function worstStatus(statuses: MfaLoadStatus[]): MfaLoadStatus {
  const priority: MfaLoadStatus[] = ['unauthorized', 'notFound', 'serverError', 'contractError'];
  for (const candidate of priority) {
    if (statuses.includes(candidate)) {
      return candidate;
    }
  }
  return 'ok';
}

/** Loads the MFA configuration, method catalog and recovery-code status in one
 * pass, reporting the aggregate load outcome for the security page's error state. */
export async function loadMfaData(): Promise<MfaLoadResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());

  const [configuration, methods, backupCodes] = await Promise.all([
    attemptLoad(() => mfaModule.getAuthMfa()),
    attemptLoad(() => mfaModule.getAuthMfaMethods()),
    attemptLoad(() => mfaModule.getAuthMfaBackupCodes()),
  ]);

  return {
    configuration: configuration.data,
    methods: methods.data,
    backupCodes: backupCodes.data,
    loadStatus: worstStatus([configuration.status, methods.status, backupCodes.status]),
  };
}

/** Starts authenticator-app (TOTP) enrollment, returning the secret and QR provisioning URI. */
export async function startTotpEnrollmentAction(): Promise<MfaEnrollmentStartResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  const result = await mfaModule.postAuthMfaTotpSetup();

  if (!result.ok) {
    return { ...resultFrom(false, result.error, errorMessage(result.error)), setup: undefined };
  }

  return { success: true, status: 'success', setup: result.data ?? undefined };
}

/** Completes authenticator-app (TOTP) enrollment by verifying a code from the app. */
export async function completeTotpEnrollmentAction(code: string, secretKey: string): Promise<MfaActionResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  // The generated contract carries the provisioning secret alongside the code; the server
  // verifies against the stored pending enrollment and ignores the echo for SMS.
  const result = await mfaModule.postAuthMfaTotpComplete({ code, secretKey });
  revalidatePath(SETTINGS_SECURITY_PATH);

  if (!result.ok) {
    return resultFrom(false, result.error, errorMessage(result.error));
  }

  return resultFrom(true);
}

/** Starts SMS enrollment after re-verifying the current account password. */
export async function startSmsEnrollmentAction(phoneNumber: string, password: string): Promise<MfaEnrollmentStartResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  const result = await mfaModule.postAuthMfaSmsSetup({ phoneNumber, password });

  if (!result.ok) {
    return { ...resultFrom(false, result.error, errorMessage(result.error)), smsSetup: undefined };
  }

  return { success: true, status: 'success', smsSetup: result.data ?? undefined };
}

/** Completes SMS enrollment by verifying the code sent to the pending phone number. */
export async function completeSmsEnrollmentAction(code: string): Promise<MfaActionResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  // The dedicated SMS contract carries only the delivered code.
  const result = await mfaModule.postAuthMfaSmsComplete({ code });
  revalidatePath(SETTINGS_SECURITY_PATH);

  if (!result.ok) {
    return resultFrom(false, result.error, errorMessage(result.error));
  }

  return resultFrom(true);
}

/**
 * Regenerates the recovery (backup) codes after re-verifying the account password
 * (issue #1153 — the server refuses to rotate recovery codes on a bearer token alone);
 * the fresh set is shown exactly once.
 */
export async function regenerateBackupCodesAction(password: string): Promise<MfaBackupCodesResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  const result = await mfaModule.postAuthMfaBackupCodesRegenerate({ password });
  revalidatePath(SETTINGS_SECURITY_PATH);

  if (!result.ok) {
    return { ...resultFrom(false, result.error, errorMessage(result.error)), codes: undefined };
  }

  return { success: true, status: 'success', codes: result.data?.codes ?? [] };
}

/** Disables MFA after re-verifying the account password (server enforces password proof). */
export async function disableMfaAction(password: string): Promise<MfaActionResult> {
  const mfaModule = new GeneratedApi.AuthMultiFactorModule(getApiClient());
  const result = await mfaModule.postAuthMfaDisable({ password });
  revalidatePath(SETTINGS_SECURITY_PATH);

  if (!result.ok) {
    return resultFrom(false, result.error, errorMessage(result.error));
  }

  return resultFrom(true);
}
