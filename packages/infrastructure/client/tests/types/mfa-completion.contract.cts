import { verifyMfa, type MfaVerifyInput, type ProviderResult } from '../../dist/index.cjs';

export function completeWithChallenge(apiUrl: string, mfaToken: string): Promise<ProviderResult> {
  const input: MfaVerifyInput = { mfaToken, method: 'totp', code: '123456', deviceFingerprint: 'device' };
  return verifyMfa(apiUrl, input);
}

export function completeWithLegacyAlias(apiUrl: string, challenge: string): Promise<ProviderResult> {
  return verifyMfa(apiUrl, { mfaSessionId: challenge, method: 'backup_code', code: 'backup-code' }, 'ignored-legacy-bearer');
}
