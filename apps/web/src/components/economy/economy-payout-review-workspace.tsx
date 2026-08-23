'use client';

import { reviewPayoutRequestAction, type EconomyPayoutReviewActionResult } from '@/lib/economy/admin-actions';
import type { EconomyPayoutReviewWorkspaceData } from '@/lib/economy/admin-queries';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { AlertTriangle, CheckCircle2, ClipboardCheck, ShieldCheck } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

function timestamp(value: string | undefined) {
  return value ? `${value.slice(0, 10)} ${value.slice(11, 16)} UTC` : '—';
}

function stateVariant(state: string | undefined): 'default' | 'destructive' | 'outline' | 'secondary' {
  if (state === 'Rejected') return 'destructive';
  if (state === 'Approved') return 'default';
  return state === 'Cancelled' ? 'outline' : 'secondary';
}

export function EconomyPayoutReviewWorkspace({ data }: { data: EconomyPayoutReviewWorkspaceData }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [message, setMessage] = useState<EconomyPayoutReviewActionResult | null>(null);
  const [reasons, setReasons] = useState<Record<string, string>>({});

  function review(requestId: string, outcome: 'approve' | 'reject') {
    startTransition(async () => {
      const result = await reviewPayoutRequestAction(requestId, outcome, reasons[requestId] || '');
      setMessage(result);
      if (result.success) router.refresh();
    });
  }

  return (
    <div className="flex min-h-0 flex-col gap-6 p-6">
      <div className="flex flex-col gap-2">
        <div className="flex items-center gap-2">
          <ClipboardCheck className="size-5" aria-hidden="true" />
          <h1 className="text-2xl font-semibold">Payout review</h1>
          <Badge variant="secondary">Tenant scoped</Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          Decisions create immutable evidence only. Approval never reserves, dispatches, or transfers value from this console.
        </p>
      </div>

      {data.issue ? (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Review data is unavailable</AlertTitle>
          <AlertDescription>{data.issue}</AlertDescription>
        </Alert>
      ) : null}

      {message ? (
        <Alert variant={message.success ? 'default' : 'destructive'}>
          {message.success ? <CheckCircle2 className="size-4" /> : <AlertTriangle className="size-4" />}
          <AlertTitle>{message.success ? 'Decision recorded' : 'Decision not recorded'}</AlertTitle>
          <AlertDescription>{message.message}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <ShieldCheck className="size-4" aria-hidden="true" />
            Review queue
          </CardTitle>
          <CardDescription>Every decision requires a non-empty reason and is scoped from the authenticated actor’s tenant.</CardDescription>
        </CardHeader>
        <CardContent>
          {data.requests.length === 0 ? (
            <p className="text-sm text-muted-foreground">There are no payout requests available to your tenant.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Request</TableHead>
                  <TableHead>Payee</TableHead>
                  <TableHead>Amount</TableHead>
                  <TableHead>State</TableHead>
                  <TableHead>Audit evidence</TableHead>
                  <TableHead className="min-w-80">Decision</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.requests.map((request) => {
                  const requestId = request.id || '';
                  const audit = data.reviewAudits[requestId] || [];
                  const reviewable = request.state === 'Submitted';
                  return (
                    <TableRow key={requestId || `${request.payeeId}-${request.createdAt}`}>
                      <TableCell className="font-mono text-xs">{requestId || '—'}</TableCell>
                      <TableCell className="font-mono text-xs">{request.payeeId || '—'}</TableCell>
                      <TableCell>{request.hardCoinUnits ?? 0}</TableCell>
                      <TableCell><Badge variant={stateVariant(request.state)}>{request.state || 'Unknown'}</Badge></TableCell>
                      <TableCell>
                        {audit.length === 0 ? (
                          <span className="text-sm text-muted-foreground">No prior review</span>
                        ) : (
                          <ul className="space-y-1 text-xs text-muted-foreground">
                            {audit.map((entry) => (
                              <li key={entry.id || `${entry.actorId}-${entry.occurredAt}`}>
                                {entry.outcome || 'Review'} · {timestamp(entry.occurredAt)} · {entry.reason || 'No reason'}
                              </li>
                            ))}
                          </ul>
                        )}
                      </TableCell>
                      <TableCell>
                        {reviewable ? (
                          <div className="flex min-w-72 flex-col gap-2">
                            <Input
                              aria-label={`Reason for payout request ${requestId}`}
                              disabled={pending}
                              onChange={(event) => setReasons((current) => ({ ...current, [requestId]: event.target.value }))}
                              placeholder="Immutable decision reason"
                              value={reasons[requestId] || ''}
                            />
                            <div className="flex flex-wrap gap-2">
                              <Button disabled={pending || !requestId} onClick={() => review(requestId, 'approve')} size="sm" type="button">
                                Approve
                              </Button>
                              <Button disabled={pending || !requestId} onClick={() => review(requestId, 'reject')} size="sm" type="button" variant="outline">
                                Reject
                              </Button>
                            </div>
                          </div>
                        ) : (
                          <span className="text-sm text-muted-foreground">No further decision is available.</span>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
