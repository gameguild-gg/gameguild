'use client';

import { createTopUpAction, type EconomyActionResult } from '@/lib/economy/actions';
import type { EconomyTopUpsData } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useState, useTransition } from 'react';
import { Link } from '@/i18n/navigation';
import { EconomyActionNotice, EconomyIssue, EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from './economy-ui';
import { TopUpStripePaymentElement } from './top-up-stripe-payment-element';

export function EconomyTopUpsWorkspace({ data }: { data: EconomyTopUpsData }) {
  const t = useTranslations('economy');
  const router = useRouter();
  const [units, setUnits] = useState('');
  const [result, setResult] = useState<EconomyActionResult<unknown> | null>(null);
  const [payment, setPayment] = useState<{ clientSecret: string; publishableKey: string; topUpId: string } | null>(null);
  const [pending, startTransition] = useTransition();

  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('topUps.title')} description={t('topUps.description')} />
      <EconomyIssue issue={data.issue} />
      <EconomyActionNotice result={result} />
      <Card>
        <CardHeader>
          <CardTitle>{t('topUps.create')}</CardTitle>
          <CardDescription>{t('topUps.paymentRequired')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex max-w-xl flex-col gap-4 sm:flex-row sm:items-end" onSubmit={(event) => {
            event.preventDefault();
            startTransition(async () => {
              const next = await createTopUpAction(Number(units), crypto.randomUUID());
              setResult(next);
              if (next.success && next.data?.clientSecret && next.data.publishableKey && next.data.topUpId) {
                setPayment({
                  clientSecret: next.data.clientSecret,
                  publishableKey: next.data.publishableKey,
                  topUpId: next.data.topUpId,
                });
              }
              if (next.success) router.refresh();
            });
          }}>
            <label className="flex flex-1 flex-col gap-2 text-sm font-medium" htmlFor="top-up-units">
              {t('topUps.units')}
              <Input id="top-up-units" inputMode="numeric" min="1" onChange={(event) => setUnits(event.target.value)} required type="number" value={units} />
            </label>
            <Button disabled={pending} type="submit">{t('topUps.create')}</Button>
          </form>
          {payment ? <div className="mt-5"><TopUpStripePaymentElement {...payment} /></div> : null}
        </CardContent>
      </Card>
      <Card>
        <CardHeader><CardTitle>{t('topUps.history')}</CardTitle></CardHeader>
        <CardContent className="overflow-x-auto">
          <Table>
            <TableHeader><TableRow><TableHead>{t('common.identifier')}</TableHead><TableHead>{t('common.amount')}</TableHead><TableHead>{t('common.state')}</TableHead><TableHead>{t('common.updated')}</TableHead></TableRow></TableHeader>
            <TableBody>
              {data.topUps.map((topUp) => (
                <TableRow key={topUp.topUpId}>
                  <TableCell className="font-mono text-sm"><Link className="underline underline-offset-4" href={`/workspace/economy/top-ups/${topUp.topUpId}`}>{topUp.topUpId}</Link></TableCell>
                  <TableCell>{formatEconomyUnits(topUp.hardCoinUnits)} HardCoin</TableCell>
                  <TableCell><Badge variant="secondary">{topUp.status}</Badge></TableCell>
                  <TableCell>{formatEconomyDate(topUp.providerBoundAt ?? topUp.requestedAt)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {!data.topUps.length ? <p className="py-6 text-center text-sm text-muted-foreground">{t('common.empty')}</p> : null}
        </CardContent>
      </Card>
    </EconomyWorkspace>
  );
}
