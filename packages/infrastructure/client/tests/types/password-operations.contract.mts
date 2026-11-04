import { changePassword, confirmPasswordReset, requestPasswordReset, type PasswordChangeInput, type PasswordResetConfirmInput } from '../../dist/index.js';

export function legacyReset(apiUrl: string, token: string, newPassword: string): Promise<void> {
  return confirmPasswordReset(apiUrl, { token, newPassword });
}

export async function scopedReset(apiUrl: string, email: string, tenantId: string, token: string, newPassword: string): Promise<void> {
  await requestPasswordReset(apiUrl, { email, tenantId });
  const input: PasswordResetConfirmInput = { token, newPassword, confirmPassword: newPassword, tenantId };
  await confirmPasswordReset(apiUrl, input);
}

export function authenticatedChange(apiUrl: string, accessToken: string, newPassword: string): Promise<void> {
  const input: PasswordChangeInput = { currentPassword: '', newPassword, confirmPassword: newPassword, revokeOtherSessions: false };
  return changePassword(apiUrl, input, accessToken);
}
