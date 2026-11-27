'use client';

import * as React from 'react';
import { Button } from '@game-guild/ui/components/button';

/** Puts `text` on the clipboard; false when the browser refuses or has no clipboard. */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    if (!navigator.clipboard?.writeText) {
      return false;
    }
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

/**
 * One-time recovery codes (issue #1298 J2). The codes arrive as a server-action result and
 * live only in these props, so once the visitor dismisses the reveal there is nowhere left
 * to show them from again: the "shown exactly once" contract, by construction.
 */
export function BackupCodesReveal({
  codes,
  title,
  description,
  copyLabel,
  copiedLabel,
  copyFailedLabel,
  dismissLabel,
  onDismiss,
  testId = 'backup-codes-reveal',
}: {
  codes: readonly string[];
  title: string;
  description?: string;
  copyLabel: string;
  copiedLabel: string;
  copyFailedLabel: string;
  dismissLabel: string;
  onDismiss: () => void;
  testId?: string;
}) {
  const [copyState, setCopyState] = React.useState<'idle' | 'copied' | 'failed'>('idle');

  async function handleCopy() {
    setCopyState((await copyToClipboard(codes.join('\n'))) ? 'copied' : 'failed');
  }

  return (
    <div className="space-y-3 rounded-lg border p-4" data-testid={testId}>
      <p className="text-sm font-medium">{title}</p>
      {description ? <p className="text-muted-foreground text-sm">{description}</p> : null}
      <ul className="grid grid-cols-2 gap-1 font-mono text-sm sm:grid-cols-3" data-testid={`${testId}-list`}>
        {codes.map((code, index) => (
          <li key={`${code}-${index}`}>{code}</li>
        ))}
      </ul>
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant="outline" onClick={() => void handleCopy()} data-testid={`${testId}-copy`}>
          {copyState === 'copied' ? copiedLabel : copyLabel}
        </Button>
        <Button type="button" onClick={onDismiss} data-testid={`${testId}-dismiss`}>
          {dismissLabel}
        </Button>
      </div>
      {copyState === 'failed' ? (
        <p role="alert" className="text-destructive text-sm">{copyFailedLabel}</p>
      ) : null}
    </div>
  );
}
