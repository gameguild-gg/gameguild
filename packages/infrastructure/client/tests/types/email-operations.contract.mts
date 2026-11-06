import { sendVerificationEmail, resendVerificationEmail, verifyEmail, type EmailVerificationInput } from '../../dist/index.js';

export function authenticatedSend(apiUrl: string, accessToken: string): Promise<void> {
  return sendVerificationEmail(apiUrl, accessToken);
}

export async function anonymousVerification(apiUrl: string, email: string, token: string, tenantId: string): Promise<void> {
  await resendVerificationEmail(apiUrl, { email });
  const input: EmailVerificationInput = { token, tenantId };
  await verifyEmail(apiUrl, input);
}
