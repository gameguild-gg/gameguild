import React from 'react';
import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import { MfaRecoveryContent, type MfaRecoveryStage } from '@/components/auth/mfa-recovery-content';
import { confirmMfaRecoveryAction, continueMfaRecoveryAction } from '@/lib/auth/mfa-recovery-actions';

/** A recovery token is 32 bytes of hex; anything far longer is not one. */
const MAX_RECOVERY_TOKEN_LENGTH = 256;

function readToken(value: string | string[] | undefined): string {
  const token = typeof value === 'string' ? value.trim() : '';
  return token.length <= MAX_RECOVERY_TOKEN_LENGTH ? token : '';
}

/**
 * Lost-second-factor recovery (issue #1298 J2, part C) — the literal path the recovery
 * emails build. Each email lands here with its stage already chosen by its token, and the
 * token is used before anything renders, so opening the link is all the visitor has to do.
 */
export default async function Page({
  params,
  searchParams,
}: PageProps<'/[locale]/auth/mfa-recovery'>): Promise<React.JSX.Element> {
  const [, query] = await Promise.all([params, searchParams]);
  const requestToken = readToken(query?.token);
  const confirmationToken = readToken(query?.confirm);

  let stage: MfaRecoveryStage;
  if (requestToken) {
    const outcome = await continueMfaRecoveryAction(requestToken);
    if (outcome.status === 'sent') {
      // The request token did its job: land on an address that no longer carries it.
      redirect('/auth/mfa-recovery?sent=1');
    }
    stage =
      outcome.status === 'cooling-off'
        ? { kind: 'cooling-off', requestToken, retryAfterSeconds: outcome.retryAfterSeconds }
        : outcome.status === 'denied'
          ? { kind: 'denied' }
          : { kind: 'continue-failed', requestToken };
  } else if (confirmationToken) {
    const outcome = await confirmMfaRecoveryAction(confirmationToken);
    if (outcome.status === 'confirmed') {
      // The confirmation token is single-use and spent: a reload must not replay it.
      redirect('/auth/mfa-recovery?confirmed=1');
    }
    stage =
      outcome.status === 'denied'
        ? { kind: 'denied' }
        : { kind: 'confirm-failed', confirmationToken };
  } else if (query?.sent === '1') {
    stage = { kind: 'confirmation-sent' };
  } else if (query?.confirmed === '1') {
    stage = { kind: 'confirmed' };
  } else {
    stage = { kind: 'request' };
  }

  // The auth layout exempts this page, so a signed-in owner reaches it too: cancel is
  // the owner's way out of a recovery someone else started on their account.
  const session = await auth();
  return <MfaRecoveryContent stage={stage} signedIn={session !== null} />;
}
