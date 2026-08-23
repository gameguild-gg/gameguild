'use client';

import {
  cancelPayoutRequestAction,
  convertHardToSoftAction,
  submitPayoutRequestAction,
  type EconomyActionResult,
} from '@/lib/economy/actions';
import type { EconomyWorkspaceData } from '@/lib/economy/queries';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { AlertTriangle, ArrowRightLeft, Landmark, ShieldCheck, WalletCards } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { type ReactNode, useState, useTransition } from 'react';

function amount(value: number | null | undefined) {
  return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value ?? 0);
}

function timestamp(value: string | null | undefined) {
  return value ? `${value.slice(0, 10)} ${value.slice(11, 16)} UTC` : '—';
}

function isReady(data: EconomyWorkspaceData, capability: string) {
  return data.capabilities.some(
    (item) => item.capability === capability && item.state === 'Ready',
  );
}

function capabilityDiagnostic(data: EconomyWorkspaceData, capability: string) {
  return data.capabilities
    .find((item) => item.capability === capability)
    ?.diagnostics?.filter(Boolean)
    .join(' · ');
}

function requestBadgeVariant(state: string | null | undefined): 'default' | 'destructive' | 'outline' | 'secondary' {
  if (state === 'Rejected') return 'destructive';
  if (state === 'Approved') return 'default';
  return state === 'Cancelled' ? 'outline' : 'secondary';
}

function operationBadgeVariant(state: string | null | undefined): 'default' | 'destructive' | 'outline' | 'secondary' {
  if (state === 'Failed') return 'destructive';
  if (state === 'Succeeded') return 'default';
  return state === 'Cancelled' ? 'outline' : 'secondary';
}

export function EconomyWalletWorkspace({ data }: { data: EconomyWorkspaceData }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [message, setMessage] = useState<EconomyActionResult | null>(null);
  const [payoutAmount, setPayoutAmount] = useState('');
  const [conversionAmount, setConversionAmount] = useState('');
  const canConvert = isReady(data, 'ConvertHardToSoft');
  const payoutDiagnostic = capabilityDiagnostic(data, 'PayoutExecution');
  const conversionDiagnostic = capabilityDiagnostic(data, 'ConvertHardToSoft');

  function execute(action: () => Promise<EconomyActionResult>) {
    startTransition(async () => {
      const result = await action();
      setMessage(result);
      if (result.success) router.refresh();
    });
  }

  return (
    <div className="flex min-h-0 flex-col gap-8 p-6">
      <div className="flex flex-col gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <WalletCards className="size-5" aria-hidden="true" />
          <h1 className="text-2xl font-semibold">Economy wallet</h1>
          <Badge variant="secondary">Safe reads</Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          Wallet balances, provenance, requests, and value-movement readiness remain explicit.
        </p>
      </div>

      {data.issue ? (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Economy data is unavailable</AlertTitle>
          <AlertDescription>{data.issue}</AlertDescription>
        </Alert>
      ) : null}

      {message ? (
        <Alert variant={message.success ? 'default' : 'destructive'}>
          {message.success ? <ShieldCheck className="size-4" /> : <AlertTriangle className="size-4" />}
          <AlertTitle>{message.success ? 'Recorded safely' : 'Action not completed'}</AlertTitle>
          <AlertDescription>{message.message}</AlertDescription>
        </Alert>
      ) : null}

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-label="Wallet balances">
        <BalanceCard label="Withdrawable HardCoin" value={data.wallet?.withdrawableHard} />
        <BalanceCard label="Available HardCoin" value={data.wallet?.availableHardToSpend} />
        <BalanceCard label="SoftCoin" value={data.wallet?.availableSoftToSpend ?? data.wallet?.soft} />
        <BalanceCard label="Pending / held HardCoin" value={(data.wallet?.pendingHard ?? 0) + (data.wallet?.heldHard ?? 0)} />
      </section>

      <section className="grid grid-cols-1 gap-5 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Landmark className="size-4" aria-hidden="true" />
              Payout request
            </CardTitle>
            <CardDescription>
              A request is reviewable evidence only. It does not reserve, dispatch, or transfer value.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form
              className="flex flex-col gap-4"
              onSubmit={(event) => {
                event.preventDefault();
                execute(() => submitPayoutRequestAction(Number(payoutAmount), crypto.randomUUID()));
              }}
            >
              <label className="flex flex-col gap-2 text-sm font-medium" htmlFor="economy-payout-amount">
                HardCoin units
                <Input
                  id="economy-payout-amount"
                  inputMode="numeric"
                  min="1"
                  name="hardCoinUnits"
                  onChange={(event) => setPayoutAmount(event.target.value)}
                  required
                  type="number"
                  value={payoutAmount}
                />
              </label>
              <Button disabled={pending} type="submit">
                Record payout request
              </Button>
              <p className="text-sm text-muted-foreground">
                {payoutDiagnostic || 'Execution is assessed independently after review.'}
              </p>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <ArrowRightLeft className="size-4" aria-hidden="true" />
              Hard-to-soft conversion
            </CardTitle>
            <CardDescription>
              This command remains disabled until the signed policy and capability predicates are ready.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form
              className="flex flex-col gap-4"
              onSubmit={(event) => {
                event.preventDefault();
                execute(() => convertHardToSoftAction(Number(conversionAmount), crypto.randomUUID()));
              }}
            >
              <label className="flex flex-col gap-2 text-sm font-medium" htmlFor="economy-conversion-amount">
                HardCoin units
                <Input
                  disabled={!canConvert}
                  id="economy-conversion-amount"
                  inputMode="numeric"
                  min="1"
                  name="principalHardCoinUnits"
                  onChange={(event) => setConversionAmount(event.target.value)}
                  required
                  type="number"
                  value={conversionAmount}
                />
              </label>
              <Button disabled={pending || !canConvert} type="submit">
                {canConvert ? 'Convert HardCoin' : 'Conversion disabled'}
              </Button>
              <p className="text-sm text-muted-foreground">
                {conversionDiagnostic || 'Capability readiness has not been reported.'}
              </p>
            </form>
          </CardContent>
        </Card>
      </section>

      <section className="grid grid-cols-1 gap-5 2xl:grid-cols-2">
        <DataTable
          columns={['Request', 'Amount', 'State', 'Updated', 'Action']}
          title="Payout requests"
          empty="No payout requests have been recorded."
        >
          {data.payoutRequests.map((request) => (
            <TableRow key={request.id}>
              <TableCell className="font-mono text-sm">{request.id || '—'}</TableCell>
              <TableCell>{amount(request.hardCoinUnits)}</TableCell>
              <TableCell><Badge variant={requestBadgeVariant(request.state)}>{request.state || 'Unknown'}</Badge></TableCell>
              <TableCell>{timestamp(request.updatedAt)}</TableCell>
              <TableCell className="text-right">
                {request.state === 'Submitted' ? (
                  <Button
                    disabled={pending || !request.id}
                    onClick={() => execute(() => cancelPayoutRequestAction(request.id || ''))}
                    size="sm"
                    type="button"
                    variant="outline"
                  >
                    Cancel
                  </Button>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
        </DataTable>

        <DataTable
          columns={['Operation', 'Amount', 'State', 'Updated', '']}
          title="Payout operations"
          empty="No payout operations have been created."
        >
          {data.payoutOperations.map((operation) => (
            <TableRow key={operation.id}>
              <TableCell className="font-mono text-sm">{operation.id || '—'}</TableCell>
              <TableCell>{amount(operation.hardCoinUnits)}</TableCell>
              <TableCell><Badge variant={operationBadgeVariant(operation.state)}>{operation.state || 'Unknown'}</Badge></TableCell>
              <TableCell>{timestamp(operation.updatedAt)}</TableCell>
              <TableCell />
            </TableRow>
          ))}
        </DataTable>
      </section>

      <DataTable
        columns={['Recorded', 'Template', 'Provenance', 'Side', 'Amount']}
        title="Journal history"
        empty="No wallet journal entries are available."
      >
        {data.transactions.map((transaction) => (
          <TableRow key={`${transaction.journalEntryId}-${transaction.journalSequence}`}>
            <TableCell>{timestamp(transaction.recordedAt)}</TableCell>
            <TableCell>{transaction.templateKind || 'Unknown'}</TableCell>
            <TableCell>{transaction.provenance || '—'}</TableCell>
            <TableCell>{transaction.side || '—'}</TableCell>
            <TableCell>{amount(transaction.amountUnits)} {transaction.currency || ''}</TableCell>
          </TableRow>
        ))}
      </DataTable>
    </div>
  );
}

function BalanceCard({ label, value }: { label: string; value: number | null | undefined }) {
  return (
    <Card>
      <CardContent className="flex flex-col gap-2 p-4">
        <span className="text-sm text-muted-foreground">{label}</span>
        <strong className="text-2xl font-semibold">{amount(value)}</strong>
      </CardContent>
    </Card>
  );
}

function DataTable({
  children,
  columns,
  empty,
  title,
}: {
  children: ReactNode;
  columns: string[];
  empty: string;
  title: string;
}) {
  const rows = Array.isArray(children) ? children : [children];
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {rows.length === 0 ? (
          <p className="text-sm text-muted-foreground">{empty}</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                {columns.map((column, index) => (
                  <TableHead key={`${column}-${index}`} className={index === columns.length - 1 ? 'text-right' : undefined}>
                    {column}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>{children}</TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  );
}
