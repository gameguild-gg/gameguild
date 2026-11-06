import { listSessions, terminateSession, terminateOtherSessions, terminateAllSessions, type SessionInfo } from '../../dist/index.cjs';

export async function manageSessions(apiUrl: string, accessToken: string): Promise<void> {
  const sessions: SessionInfo[] = await listSessions(apiUrl, accessToken);
  const other = sessions.find((session) => !session.isCurrent);
  if (other) await terminateSession(apiUrl, other.id, accessToken);
  await terminateOtherSessions(apiUrl, accessToken);
  await terminateAllSessions(apiUrl, accessToken);
}

export function activityLabel(session: SessionInfo): string {
  return `${session.lastActiveAt}: ${session.userAgent ?? session.ipAddress ?? 'Unknown device'}`;
}
