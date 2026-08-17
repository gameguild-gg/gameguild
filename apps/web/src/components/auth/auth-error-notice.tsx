import { getTranslations } from 'next-intl/server';
import React from 'react';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { TriangleAlert } from 'lucide-react';

/**
 * Error codes emitted by the OAuth callback handler in @game-guild/client
 * (`integrations/next/handlers.ts`), normalized to alphanumerics.
 */
const ERROR_MESSAGE_KEYS: Record<string, string> = {
  statemismatch: 'stateMismatch',
  missingcode: 'missingCode',
  callbackfailed: 'callbackFailed',
  accessdenied: 'accessDenied',
  configuration: 'callbackFailed',
  verification: 'stateMismatch',
};

function normalizeErrorCode(errorCode: string): string {
  return errorCode.trim().toLowerCase().replace(/[^a-z0-9]/g, '');
}

/** Map an auth error code to an `authError` translation key; null when nothing to show. */
export function resolveAuthErrorMessageKey(errorCode: string | undefined | null): string | null {
  if (!errorCode) return null;
  const normalized = normalizeErrorCode(errorCode);
  if (!normalized) return null;
  return ERROR_MESSAGE_KEYS[normalized] ?? 'generic';
}

/** Inline error banner for (auth)/* routes — keeps form context instead of a dedicated error page. */
export async function AuthErrorNotice({ errorCode }: { errorCode?: string | null }): Promise<React.JSX.Element | null> {
  const messageKey = resolveAuthErrorMessageKey(errorCode);
  if (!messageKey) return null;

  const t = await getTranslations('authError');
  return (
    <Alert variant="destructive" data-testid="auth-error-notice" className="text-left">
      <TriangleAlert aria-hidden="true" />
      <AlertDescription>{t(messageKey)}</AlertDescription>
    </Alert>
  );
}
