'use client';

import type { MarketplaceActionResult } from '@/lib/marketplace/actions';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { useTranslations } from 'next-intl';

export function MarketplaceActionNotice({ result }: { result: MarketplaceActionResult<unknown> | null }) {
  const t = useTranslations('marketplace');
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'}>
      <AlertTitle>{result.success ? t('completed') : t('notCompleted')}</AlertTitle>
      <AlertDescription>{result.message}</AlertDescription>
    </Alert>
  );
}
