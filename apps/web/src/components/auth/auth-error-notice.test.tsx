import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));

import { AuthErrorNotice, resolveAuthErrorMessageKey } from './auth-error-notice';

describe('resolveAuthErrorMessageKey', () => {
  it.each([
    ['state_mismatch', 'stateMismatch'],
    ['state-mismatch', 'stateMismatch'],
    ['StateMismatch', 'stateMismatch'],
    ['missing_code', 'missingCode'],
    ['callback_failed', 'callbackFailed'],
    ['configuration', 'callbackFailed'],
    ['verification', 'stateMismatch'],
    ['access_denied', 'accessDenied'],
  ])('maps %s to %s', (code, expected) => {
    expect(resolveAuthErrorMessageKey(code)).toBe(expected);
  });

  it('falls back to the generic message for unknown codes', () => {
    expect(resolveAuthErrorMessageKey('something_new')).toBe('generic');
  });

  it.each([
    [undefined],
    [null],
    [''],
    ['   '],
    ['!!!'],
  ])('returns null for %j', (code) => {
    expect(resolveAuthErrorMessageKey(code)).toBeNull();
  });
});

describe('AuthErrorNotice', () => {
  afterEach(cleanup);

  it('renders the translated message for a known error code', async () => {
    const ui = await AuthErrorNotice({ errorCode: 'access_denied' });
    render(ui);

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('authError.accessDenied');
  });

  it('renders the generic message for an unknown error code', async () => {
    const ui = await AuthErrorNotice({ errorCode: 'mystery_failure' });
    render(ui);

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('authError.generic');
  });

  it('renders nothing without an error code', async () => {
    const ui = await AuthErrorNotice({ errorCode: undefined });
    expect(ui).toBeNull();
  });
});
