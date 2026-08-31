'use client';

import { updateLaunchPadSettings } from '@/lib/launch-pad/actions';
import type { LaunchPadSettings } from '@/lib/launch-pad/queries';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { Label } from '@game-guild/ui/components/label';
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import { useState, useTransition, type FormEvent } from 'react';

export function LaunchPadSettingsForm({ settings }: { settings: LaunchPadSettings | null }) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<Awaited<ReturnType<typeof updateLaunchPadSettings>> | null>(null);
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    startTransition(async () => setResult(await updateLaunchPadSettings(new FormData(event.currentTarget))));
  }
  return (
    <form className="max-w-3xl space-y-5 rounded-md border bg-card p-5" onSubmit={submit}>
      <div className="space-y-2">
        <Label htmlFor="launch-pad-version-policy">Eligible project versions</Label>
        <select
          id="launch-pad-version-policy"
          name="versionSubmissionPolicy"
          defaultValue={settings?.versionSubmissionPolicy ?? 'ReleasedImmutable'}
          className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
        >
          <option value="ReleasedImmutable">Released only; immutable after first submission</option>
          <option value="ReadyMutableUntilReview">Ready for Testing or Released; replace while Pending</option>
        </select>
        <p className="text-sm text-muted-foreground">The Launch Pad default is Released only. Changes affect new submissions and future replacements, never approved historical applications.</p>
      </div>
      <Button type="submit" disabled={pending}>{pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save Launch Pad policy</Button>
      {result ? <Alert variant={result.success ? 'default' : 'destructive'}>{result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}<AlertDescription>{result.success ? 'Launch Pad settings updated.' : result.error}</AlertDescription></Alert> : null}
    </form>
  );
}
