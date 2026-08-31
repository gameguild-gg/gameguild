'use client';

import { cancelPayoutRequestAction, createPayoutOnboardingAction, submitPayoutRequestAction, type EconomyActionResult } from '@/lib/economy/actions';
import type { EconomyPayoutsData } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useState, useTransition } from 'react';
import { EconomyActionNotice, EconomyIssue, EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from './economy-ui';
import { Link } from '@/i18n/navigation';

export function EconomyPayoutsWorkspace({ data }: { data: EconomyPayoutsData }) {
  const t = useTranslations('economy');
  const router = useRouter();
  const [amount, setAmount] = useState('');
  const [result, setResult] = useState<EconomyActionResult<unknown> | null>(null);
  const [pending, startTransition] = useTransition();

  function run(action: () => Promise<EconomyActionResult<unknown>>) {
    startTransition(async () => {
      const next = await action();
      setResult(next);
      if (next.success) router.refresh();
    });
  }

  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={t('payouts.title')} description={t('payouts.description')} badge={data.account?.state ?? t('payouts.notConnected')} />
      <EconomyIssue issue={data.issue} />
      <EconomyActionNotice result={result} />
      <div className="grid gap-4 xl:grid-cols-2">
        <Card>
          <CardHeader><CardTitle>{t('payouts.account')}</CardTitle><CardDescription>{data.account?.payoutsEnabled ? t('payouts.providerEnabled') : t('payouts.providerNotReady')}</CardDescription></CardHeader>
          <CardContent><Button disabled={pending} onClick={() => run(async () => {
            const next = await createPayoutOnboardingAction();
            if (next.success && next.data?.onboardingUri) window.location.assign(next.data.onboardingUri);
            return next;
          })} type="button">{t('payouts.onboarding')}</Button></CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>{t('payouts.request')}</CardTitle><CardDescription>{t('payouts.noDispatch')}</CardDescription></CardHeader>
          <CardContent><form className="flex gap-3" onSubmit={(event) => { event.preventDefault(); run(() => submitPayoutRequestAction(Number(amount), crypto.randomUUID())); }}><Input inputMode="numeric" min="1" onChange={(event) => setAmount(event.target.value)} required type="number" value={amount} /><Button disabled={pending} type="submit">{t('payouts.request')}</Button></form></CardContent>
        </Card>
      </div>
      <PayoutTable data={data.requests} pending={pending} title={t('payouts.requests')} t={t} onCancel={(id) => run(() => cancelPayoutRequestAction(id))} />
      <PayoutTable data={data.operations} detail pending={pending} title={t('payouts.operations')} t={t} />
    </EconomyWorkspace>
  );
}

function PayoutTable({ data, detail = false, title, pending, onCancel, t }: { data: Array<{ id?: string; hardCoinUnits?: number; state?: string; updatedAt?: string }>; detail?: boolean; title: string; pending: boolean; onCancel?: (id: string) => void; t: (key: string) => string }) {
  return <Card><CardHeader><CardTitle>{title}</CardTitle></CardHeader><CardContent className="overflow-x-auto"><Table><TableHeader><TableRow><TableHead>{t('common.identifier')}</TableHead><TableHead>HardCoin</TableHead><TableHead>{t('common.state')}</TableHead><TableHead>{t('common.updated')}</TableHead>{onCancel ? <TableHead /> : null}</TableRow></TableHeader><TableBody>{data.map((item, index) => <TableRow key={item.id ?? `${title}-${index}`}><TableCell className="font-mono text-sm">{detail && item.id ? <Link className="underline underline-offset-4" href={`/workspace/economy/payouts/${item.id}`}>{item.id}</Link> : item.id}</TableCell><TableCell>{formatEconomyUnits(item.hardCoinUnits)}</TableCell><TableCell><Badge variant="secondary">{item.state}</Badge></TableCell><TableCell>{formatEconomyDate(item.updatedAt)}</TableCell>{onCancel ? <TableCell><Button disabled={pending || item.state !== 'Submitted'} onClick={() => onCancel(item.id ?? '')} size="sm" type="button" variant="outline">{t('common.cancel')}</Button></TableCell> : null}</TableRow>)}</TableBody></Table></CardContent></Card>;
}
