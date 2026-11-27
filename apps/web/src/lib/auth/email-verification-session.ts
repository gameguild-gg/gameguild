'use server';

import { auth, update } from '@/auth';

/**
 * Records a successful email verification on the signed-in session (#1298 J1).
 *
 * The session carries the backend's `emailVerified` verdict from sign-in. The
 * token is consumed by a public endpoint that answers without a session, so
 * the verdict is re-read only on the next sign-in or token refresh; until
 * then the reminder banner would keep showing. Marking it here clears the
 * banner for the current session. Anonymous visitors keep their session-less
 * page and simply sign in afterwards.
 */
export async function markSessionEmailVerified(): Promise<void> {
  const session = await auth();
  if (!session || session.user.emailVerified === true) return;
  await update({ user: { ...session.user, emailVerified: true } });
}
