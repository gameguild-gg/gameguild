// @vitest-environment node
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  verifyEmailAction: vi.fn(),
  resendVerificationEmailAction: vi.fn(),
}));

vi.mock('./password-actions', () => ({
  verifyEmailAction: (...args: unknown[]) => mocks.verifyEmailAction(...args),
  resendVerificationEmailAction: (...args: unknown[]) => mocks.resendVerificationEmailAction(...args),
}));

import {
  emailVerificationPath,
  MAX_VERIFICATION_TOKEN_LENGTH,
  requestVerificationEmail,
  verifyEmailToken,
  VERIFICATION_RESEND_COOLDOWN_MS,
} from './email-verification';

// A stand-in with the real token's length (32), deliberately not hex: a hex
// literal of that length is what secret scanners look for.
const TOKEN = 'token-'.padEnd(32, '0');

describe('email verification helpers (#1298 J1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('verifyEmailToken', () => {
    it('hands the trimmed token to the verify action', async () => {
      mocks.verifyEmailAction.mockResolvedValue({ success: true, data: undefined });

      await expect(verifyEmailToken(`  ${TOKEN}\n`)).resolves.toEqual({ ok: true });
      expect(mocks.verifyEmailAction).toHaveBeenCalledTimes(1);
      expect(mocks.verifyEmailAction).toHaveBeenCalledWith(TOKEN);
    });

    it('carries the action\'s reason back for a refused token', async () => {
      mocks.verifyEmailAction.mockResolvedValue({
        success: false,
        error: 'Invalid or expired verification token',
      });

      await expect(verifyEmailToken(TOKEN)).resolves.toEqual({
        ok: false,
        message: 'Invalid or expired verification token',
      });
    });

    it.each([
      ['empty', ''],
      ['blank', '   '],
      ['not a string', undefined],
      ['longer than any real token', 'x'.repeat(MAX_VERIFICATION_TOKEN_LENGTH + 1)],
    ])('refuses a token that is %s without calling the API', async (_label, token) => {
      await expect(verifyEmailToken(token as string)).resolves.toEqual({
        ok: false,
        message: 'Invalid or expired verification token',
      });
      expect(mocks.verifyEmailAction).not.toHaveBeenCalled();
    });
  });

  describe('requestVerificationEmail', () => {
    it('hands the address to the resend action, which normalizes it before calling the API', async () => {
      mocks.resendVerificationEmailAction.mockResolvedValue({ success: true, data: undefined });

      await expect(requestVerificationEmail('  Member@Example.com ')).resolves.toEqual({ ok: true });
      expect(mocks.resendVerificationEmailAction).toHaveBeenCalledWith('  Member@Example.com ');
    });

    it('carries the action\'s reason back on failure', async () => {
      mocks.resendVerificationEmailAction.mockResolvedValue({ success: false, error: 'Email is required.' });

      await expect(requestVerificationEmail('')).resolves.toEqual({ ok: false, message: 'Email is required.' });
    });
  });

  describe('emailVerificationPath', () => {
    it('carries the address, the fresh flag and where the visitor was heading', () => {
      expect(emailVerificationPath('jo@e.co', '/workspace/learn?tab=courses')).toBe(
        '/verify?email=jo%40e.co&new=1&redirectTo=%2Fworkspace%2Flearn%3Ftab%3Dcourses',
      );
    });

    it('keeps the default destination explicit', () => {
      expect(emailVerificationPath('jo@e.co', '/')).toBe('/verify?email=jo%40e.co&new=1&redirectTo=%2F');
    });
  });

  it('mirrors the API\'s two-minute resend window', () => {
    expect(VERIFICATION_RESEND_COOLDOWN_MS).toBe(120_000);
  });
});
