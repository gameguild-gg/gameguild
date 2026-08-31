'use client';

import {
  beginEconomyConsoleStepUpAction,
  executeEconomyConsoleAction,
  verifyEconomyConsoleStepUpAction,
  type EconomyConsoleActionResult,
} from '@/lib/economy/console-actions';
import {
  economyConsoleActionDefinitions,
  type EconomyConsoleActionDefinition,
  type EconomyConsoleActionField,
} from '@/lib/economy/console-action-definitions';
import type { EconomyConsoleSurface } from '@/lib/economy/console';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { CheckCircle2, KeyRound, ShieldAlert } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useRouter } from 'next/navigation';
import { useMemo, useState, useTransition } from 'react';

function translationKey(action: string) {
  return action.replaceAll('.', '_').replaceAll('-', '_');
}

function initialValues(definition: EconomyConsoleActionDefinition | undefined) {
  return Object.fromEntries((definition?.fields ?? []).map((item) => [item.key,
    ['payload', 'buffers'].includes(item.key) ? '{}' : ['services', 'custodyObservationIds', 'verifiedSessionIds'].includes(item.key) ? '[]' : '',
  ]));
}

function ActionField({ field, value, onChange }: {
  field: EconomyConsoleActionField;
  value: string | boolean;
  onChange: (value: string | boolean) => void;
}) {
  const t = useTranslations('economy.console.fields');
  const label = t.has(field.key) ? t(field.key) : field.key;
  if (field.kind === 'checkbox') {
    return (
      <label className="flex items-center gap-2 rounded-md border p-3 text-sm">
        <input checked={Boolean(value)} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
        <span>{label}</span>
      </label>
    );
  }
  if (field.kind === 'select') {
    return (
      <label className="grid gap-1.5 text-sm">
        <span className="font-medium">{label}</span>
        <select className="h-10 rounded-md border bg-background px-3" onChange={(event) => onChange(event.target.value)} required={field.required} value={String(value)}>
          <option value="">—</option>
          {field.options.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
      </label>
    );
  }
  if (field.kind === 'textarea') {
    return (
      <label className="grid gap-1.5 text-sm sm:col-span-2">
        <span className="font-medium">{label}</span>
        <textarea className="min-h-24 rounded-md border bg-background p-3 font-mono text-sm" onChange={(event) => onChange(event.target.value)} required={field.required} value={String(value)} />
      </label>
    );
  }
  return (
    <label className="grid gap-1.5 text-sm">
      <span className="font-medium">{label}</span>
      <Input min={field.kind === 'number' ? 0 : undefined} onChange={(event) => onChange(event.target.value)} required={field.required} type={field.kind} value={String(value)} />
    </label>
  );
}

export function EconomyConsoleActions({ surface }: { surface: EconomyConsoleSurface }) {
  const t = useTranslations('economy.console');
  const router = useRouter();
  const definitions = useMemo(() => economyConsoleActionDefinitions[surface] ?? [], [surface]);
  const [selectedAction, setSelectedAction] = useState<string>(definitions[0]?.action ?? '');
  const definition = definitions.find((item) => item.action === selectedAction);
  const [values, setValues] = useState<Record<string, string | boolean>>(() => initialValues(definition));
  const [challengeId, setChallengeId] = useState<string | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [result, setResult] = useState<EconomyConsoleActionResult | null>(null);
  const [pending, startTransition] = useTransition();

  if (!definition) return null;
  const activeDefinition = definition;

  function choose(actionName: string) {
    const next = definitions.find((item) => item.action === actionName);
    setSelectedAction(actionName);
    setValues(initialValues(next));
    setChallengeId(null);
    setMfaCode('');
    setResult(null);
  }

  function complete(actionResult: EconomyConsoleActionResult) {
    setResult(actionResult);
    if (actionResult.success) router.refresh();
  }

  function submit() {
    startTransition(async () => {
      if (activeDefinition.stepUp) {
        const challenge = await beginEconomyConsoleStepUpAction(activeDefinition.action, values);
        setResult(challenge);
        if (challenge.success && challenge.challengeId) setChallengeId(challenge.challengeId);
        return;
      }
      complete(await executeEconomyConsoleAction(activeDefinition.action, values));
    });
  }

  function verifyAndExecute(activeChallengeId: string) {
    startTransition(async () => {
      const verified = await verifyEconomyConsoleStepUpAction(activeChallengeId, mfaCode);
      if (!verified.success || !verified.receipt) {
        setResult(verified);
        return;
      }
      const executed = await executeEconomyConsoleAction(activeDefinition.action, values, verified.receipt);
      setChallengeId(null);
      setMfaCode('');
      complete(executed);
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('title')}</CardTitle>
        <CardDescription>{t('description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {result ? (
          <Alert variant={result.success ? 'default' : 'destructive'}>
            {result.success ? <CheckCircle2 className="size-4" /> : <ShieldAlert className="size-4" />}
            <AlertTitle>{result.success ? t('accepted') : t('rejected')}</AlertTitle>
            <AlertDescription>{result.message}</AlertDescription>
          </Alert>
        ) : null}

        <label className="grid gap-1.5 text-sm">
          <span className="font-medium">{t('selectAction')}</span>
          <select className="h-10 rounded-md border bg-background px-3" disabled={pending || Boolean(challengeId)} onChange={(event) => choose(event.target.value)} value={selectedAction}>
            {definitions.map((item) => <option key={item.action} value={item.action}>{t(`actions.${translationKey(item.action)}`)}</option>)}
          </select>
        </label>

        <div className="grid gap-3 sm:grid-cols-2">
          {activeDefinition.fields.map((item) => (
            <ActionField key={item.key} field={item} value={values[item.key] as string | boolean} onChange={(value) => setValues((current) => ({ ...current, [item.key]: value }))} />
          ))}
        </div>

        {challengeId ? (
          <div className="grid gap-3 rounded-lg border border-amber-500/40 bg-amber-500/5 p-4">
            <div className="flex items-center gap-2 font-medium"><KeyRound className="size-4" />{t('mfaTitle')}</div>
            <p className="text-sm text-muted-foreground">{t('mfaDescription')}</p>
            <Input autoComplete="one-time-code" inputMode="numeric" onChange={(event) => setMfaCode(event.target.value)} placeholder={t('mfaCode')} value={mfaCode} />
            <div className="flex gap-2">
              <Button disabled={pending || !mfaCode.trim()} onClick={() => verifyAndExecute(challengeId)} type="button">{pending ? t('pending') : t('verifyAndExecute')}</Button>
              <Button disabled={pending} onClick={() => { setChallengeId(null); setMfaCode(''); }} type="button" variant="outline">{t('cancel')}</Button>
            </div>
          </div>
        ) : (
          <Button disabled={pending} onClick={submit} type="button">{pending ? t('pending') : activeDefinition.stepUp ? t('beginMfa') : t('execute')}</Button>
        )}
      </CardContent>
    </Card>
  );
}
