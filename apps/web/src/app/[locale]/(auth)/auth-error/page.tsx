import { getTranslations } from 'next-intl/server';
import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import React from 'react';

const ERROR_MESSAGE_KEYS: Record<string, string> = {
  state_mismatch: 'stateMismatch',
  missing_code: 'missingCode',
  callback_failed: 'callbackFailed',
  access_denied: 'accessDenied',
};

export default async function AuthErrorPage({
  searchParams,
}: PageProps<'/[locale]/auth-error'>): Promise<React.JSX.Element> {
  const params = await searchParams;
  const t = await getTranslations('authError');
  const error = typeof params?.error === 'string' ? params.error : '';
  const messageKey = ERROR_MESSAGE_KEYS[error] ?? 'generic';

  return (
    <Card className="w-full">
      <CardHeader className="text-center">
        <CardTitle className="text-xl">{t('title')}</CardTitle>
        <CardDescription>{t(messageKey)}</CardDescription>
      </CardHeader>
      <CardContent>
        <Button asChild variant="outline" className="w-full">
          <Link href="/sign-in">{t('backToSignIn')}</Link>
        </Button>
      </CardContent>
    </Card>
  );
}
