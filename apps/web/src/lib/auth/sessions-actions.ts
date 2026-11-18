'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type IdentityAuthenticationSessionOutput,
  type IdentityAuthenticationSessionSecurityAnalysis,
  type IdentityAuthenticationTrustedDeviceOutput,
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

/**
 * Server actions for the session management surface (settings/security, issue #278).
 *
 * Mirrors password-change-action.ts: bearer via createServerClient + getToken. All
 * mutations revalidate the security page so the session and trusted-device lists
 * reflect server state after every action.
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

export interface SessionsPageData {
  sessions: IdentityAuthenticationSessionOutput[];
  analysis: IdentityAuthenticationSessionSecurityAnalysis | null;
  trustedDevices: IdentityAuthenticationTrustedDeviceOutput[];
}

/**
 * Outcome of loading the security page's server data (issue #1298 J4): every
 * failure mode the old `safeLoad` swallowed into an empty render is surfaced so
 * the page can show an explicit error state with a retry instead of a silently
 * blank page. `'ok'` means every read succeeded.
 */
export type SecurityLoadStatus = 'ok' | 'unauthorized' | 'notFound' | 'serverError' | 'contractError';

/** {@link SessionsPageData} plus the aggregate load outcome of its three reads. */
export interface SessionsLoadResult extends SessionsPageData {
  loadStatus: SecurityLoadStatus;
}

export type SessionActionStatus = 'success' | 'notFound' | 'unauthorized' | 'error';

export interface SessionActionResult {
  success: boolean;
  status: SessionActionStatus;
}

function statusFromError(error: { status?: number } | undefined): SessionActionStatus {
  switch (error?.status) {
    case 401:
      return 'unauthorized';
    case 404:
      return 'notFound';
    default:
      return 'error';
  }
}

function resultFrom(success: boolean, error?: { status?: number }): SessionActionResult {
  return success
    ? { success: true, status: 'success' }
    : { success: false, status: statusFromError(error) };
}

function loadStatusFromError(error: { status?: number } | undefined): SecurityLoadStatus {
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
 * (issue #1298 J4). The generated client's safeParse THROWS on zod contract
 * mismatch (issue #864) and the transport throws when the API is unreachable;
 * a non-ok result carries the HTTP status. Both become a load status the page
 * renders as an explicit error state — never as an empty session/device list
 * that looks like "nothing to review".
 */
async function attemptLoad<T>(call: () => Promise<{ ok: boolean; data?: T; error?: { status?: number } }>): Promise<{
  data: T | null;
  status: SecurityLoadStatus;
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
function worstStatus(statuses: SecurityLoadStatus[]): SecurityLoadStatus {
  const priority: SecurityLoadStatus[] = ['unauthorized', 'notFound', 'serverError', 'contractError'];
  for (const candidate of priority) {
    if (statuses.includes(candidate)) {
      return candidate;
    }
  }
  return 'ok';
}

/**
 * Loads the session list, the current-session security analysis, and the trusted
 * device list in one pass for the settings/security page, reporting the aggregate
 * load outcome so the page can render an explicit error state with retry.
 */
export async function loadSessionsData(): Promise<SessionsLoadResult> {
  const client = getApiClient();
  const sessionsModule = new GeneratedApi.AuthSessionsModule(client);
  const trustedDevicesModule = new GeneratedApi.AuthTrustedDevicesModule(client);

  const [sessions, analysis, trustedDevices] = await Promise.all([
    attemptLoad(() => sessionsModule.getAuthSessions()),
    attemptLoad(() => sessionsModule.getAuthSessionsAnalyzeSecurity()),
    attemptLoad(() => trustedDevicesModule.getAuthTrustedDevices()),
  ]);

  return {
    sessions: sessions.data ?? [],
    analysis: analysis.data,
    trustedDevices: trustedDevices.data ?? [],
    loadStatus: worstStatus([sessions.status, analysis.status, trustedDevices.status]),
  };
}

/** Names or renames one of the current user's own sessions (1–64 characters). */
export async function renameSessionAction(sessionId: string, name: string): Promise<SessionActionResult> {
  const sessionsModule = new GeneratedApi.AuthSessionsModule(getApiClient());
  const result = await sessionsModule.patchAuthSessionsName(sessionId, { name });
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}

/** Terminates one specific session owned by the current user. */
export async function terminateSessionAction(sessionId: string): Promise<SessionActionResult> {
  const sessionsModule = new GeneratedApi.AuthSessionsModule(getApiClient());
  const result = await sessionsModule.deleteAuthSessions(sessionId);
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}

/** Terminates every session except the current one. */
export async function terminateOtherSessionsAction(): Promise<SessionActionResult> {
  const sessionsModule = new GeneratedApi.AuthSessionsModule(getApiClient());
  const result = await sessionsModule.postAuthSessionsTerminateOthers();
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}

/** Terminates every session including the current one (signs the user out everywhere). */
export async function terminateAllSessionsAction(): Promise<SessionActionResult> {
  const sessionsModule = new GeneratedApi.AuthSessionsModule(getApiClient());
  const result = await sessionsModule.postAuthSessionsTerminateAll();
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}

/** Marks the device behind the current session as trusted. */
export async function trustCurrentDeviceAction(deviceName?: string): Promise<SessionActionResult> {
  const trustedDevicesModule = new GeneratedApi.AuthTrustedDevicesModule(getApiClient());
  const result = await trustedDevicesModule.postAuthTrustedDevices({
    deviceName: deviceName ?? '',
  });
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}

/** Removes one device from the trusted device list. */
export async function revokeTrustedDeviceAction(deviceId: string): Promise<SessionActionResult> {
  const trustedDevicesModule = new GeneratedApi.AuthTrustedDevicesModule(getApiClient());
  const result = await trustedDevicesModule.deleteAuthTrustedDevices(deviceId);
  revalidatePath(SETTINGS_SECURITY_PATH);
  return resultFrom(result.ok, result.ok ? undefined : result.error);
}
