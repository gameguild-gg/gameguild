'use client';

import { claimBountyAction, createBountyAction, reclaimBountyAction, type EconomyActionResult } from '@/lib/economy/actions';
import type { EconomyBountiesData } from '@/lib/economy/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useState, useTransition } from 'react';
import { EconomyActionNotice, EconomyIssue, EconomyPageHeader, EconomyWorkspace, formatEconomyDate, formatEconomyUnits } from './economy-ui';
import { Link } from '@/i18n/navigation';

export function EconomyBountiesWorkspace({ data }: { data: EconomyBountiesData }) {
  const t = useTranslations('economy');
  const router = useRouter();
  const [amount, setAmount] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [minimumReputation, setMinimumReputation] = useState('0');
  const [result, setResult] = useState<EconomyActionResult | null>(null);
  const [pending, startTransition] = useTransition();

  function run(action: () => Promise<EconomyActionResult>) {
    startTransition(async () => { const next = await action(); setResult(next); if (next.success) router.refresh(); });
  }

  return <EconomyWorkspace>
    <EconomyPageHeader title={t('bounties.title')} description={t('bounties.description')} />
    <EconomyIssue issue={data.issue} /><EconomyActionNotice result={result} />
    <Card><CardHeader><CardTitle>{t('bounties.create')}</CardTitle></CardHeader><CardContent>
      <form className="grid gap-4 md:grid-cols-3" onSubmit={(event) => {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        run(() => createBountyAction({ amountUnits: Number(amount), currency: 'HardCoin', expiresAt: new Date(expiresAt).toISOString(), idempotencyKey: crypto.randomUUID(), minimumReputation: Number(minimumReputation), requiresInstructorVerification: form.get('instructor') === 'on', requiresPrerequisite: form.get('prerequisite') === 'on' }));
      }}>
        <Field label={t('common.amount')}><Input min="1" onChange={(event) => setAmount(event.target.value)} required type="number" value={amount} /></Field>
        <Field label={t('bounties.expires')}><Input onChange={(event) => setExpiresAt(event.target.value)} required type="datetime-local" value={expiresAt} /></Field>
        <Field label={t('bounties.minimumReputation')}><Input min="0" onChange={(event) => setMinimumReputation(event.target.value)} required type="number" value={minimumReputation} /></Field>
        <label className="flex items-center gap-2 text-sm"><input name="prerequisite" type="checkbox" />{t('bounties.prerequisite')}</label>
        <label className="flex items-center gap-2 text-sm"><input name="instructor" type="checkbox" />{t('bounties.instructor')}</label>
        <Button disabled={pending} type="submit">{t('bounties.create')}</Button>
      </form>
    </CardContent></Card>
    <div className="grid gap-4 lg:grid-cols-2">{data.bounties.map((bounty) => {
      const id = bounty.id?.value ?? '';
      return <Card key={id}><CardHeader><div className="flex items-center justify-between gap-3"><CardTitle className="font-mono text-base"><Link className="underline underline-offset-4" href={`/workspace/economy/bounties/${id}`}>{id}</Link></CardTitle><Badge variant="secondary">{bounty.status}</Badge></div></CardHeader><CardContent className="grid gap-4"><p className="text-sm">{formatEconomyUnits(bounty.amount?.units)} {bounty.amount?.currency}</p><p className="text-sm text-muted-foreground">{t('bounties.expires')}: {formatEconomyDate(bounty.expiresAt)}</p><div className="flex gap-2"><Button disabled={pending || bounty.status !== 'Open'} onClick={() => run(() => claimBountyAction(id, crypto.randomUUID()))} size="sm" type="button">{t('bounties.claim')}</Button><Button disabled={pending || bounty.status !== 'Expired'} onClick={() => run(() => reclaimBountyAction(id, crypto.randomUUID()))} size="sm" type="button" variant="outline">{t('bounties.reclaim')}</Button></div></CardContent></Card>;
    })}</div>
  </EconomyWorkspace>;
}

function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label className="flex flex-col gap-2 text-sm font-medium">{label}{children}</label>; }
