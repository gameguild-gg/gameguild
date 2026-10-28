'use client';

import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { BarChart3, CheckCircle2, Pause, Play, RotateCcw } from 'lucide-react';
import { useCallback, useState } from 'react';

/** Promotional campaign management and reporting dashboard (issue #418): the console
 * surface over the promo-campaign API — campaign lifecycle management (activate,
 * pause, complete, cancel) plus the per-campaign performance report with usage,
 * discount impact, conversion and customer-acquisition-cost attribution. The server
 * page loads the initial list through the session token; this console refetches and
 * issues lifecycle transitions through the scoped proxy. Authorization stays
 * server-side on the promo-campaigns controller. */

export interface PromoCampaignRow {
  readonly id: string;
  readonly name: string;
  readonly key: string;
  readonly description?: string | null;
  readonly status: string;
  readonly startsAtLocal?: string | null;
  readonly endsAtLocal?: string | null;
  readonly timeZoneId: string;
  readonly budget?: number | null;
  readonly acquisitionSpend?: number | null;
  readonly currency: string;
  readonly codeCount: number;
  readonly attributedCustomers: number;
  readonly customerAcquisitionCost?: number | null;
}

export interface CampaignPerformanceReport {
  readonly campaignId: string;
  readonly totalCodes: number;
  readonly activeCodes: number;
  readonly totalUses: number;
  readonly uniqueUsers: number;
  readonly totalDiscountGiven: number;
  readonly averageDiscountPerUse: number;
  readonly redemptionRate?: number | null;
  readonly conversionRate?: number | null;
  readonly acquiredCustomers: number;
  readonly campaignAcquisitionSpend?: number | null;
  readonly customerAcquisitionCost?: number | null;
}

interface PromoCampaignsConsoleProps {
  readonly initialCampaigns: readonly PromoCampaignRow[];
}

const STATUS_VARIANT: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Active: 'default',
  Draft: 'secondary',
  Paused: 'outline',
  Completed: 'secondary',
  Cancelled: 'destructive',
  Archived: 'outline',
};

const LIFECYCLE_ACTIONS: ReadonlyArray<{
  readonly action: 'activate' | 'pause' | 'complete' | 'cancel';
  readonly label: string;
  readonly icon: typeof Play;
}> = [
  { action: 'activate', label: 'Activate', icon: Play },
  { action: 'pause', label: 'Pause', icon: Pause },
  { action: 'complete', label: 'Complete', icon: CheckCircle2 },
  { action: 'cancel', label: 'Cancel', icon: RotateCcw },
];

function money(value: number | null | undefined, currency: string): string {
  if (value === null || value === undefined) return '—';
  return new Intl.NumberFormat('en', { style: 'currency', currency, maximumFractionDigits: 2 }).format(value);
}

function percent(value: number | null | undefined): string {
  return value === null || value === undefined ? '—' : `${(value * 100).toFixed(1)}%`;
}

function windowLabel(campaign: PromoCampaignRow): string {
  const start = campaign.startsAtLocal?.slice(0, 10) ?? '…';
  const end = campaign.endsAtLocal?.slice(0, 10) ?? '∞';
  return `${start} → ${end} (${campaign.timeZoneId})`;
}

export function PromoCampaignsConsole({ initialCampaigns }: PromoCampaignsConsoleProps) {
  const [campaigns, setCampaigns] = useState<readonly PromoCampaignRow[]>(initialCampaigns);
  const [performance, setPerformance] = useState<Record<string, CampaignPerformanceReport>>({});
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadPerformance = useCallback(async (campaignId: string) => {
    setError(null);
    setPendingAction(`report:${campaignId}`);
    try {
      const response = await fetch(`/api/billing/promo-campaigns?campaignId=${encodeURIComponent(campaignId)}`, {
        cache: 'no-store',
      });
      if (!response.ok) {
        setError(`The campaign performance API rejected the report query (HTTP ${response.status}).`);
        return;
      }
      const report = (await response.json()) as CampaignPerformanceReport;
      setPerformance((current) => ({ ...current, [campaignId]: report }));
    } catch {
      setError('The campaign performance report could not be loaded.');
    } finally {
      setPendingAction(null);
    }
  }, []);

  const runLifecycle = useCallback(
    async (campaignId: string, action: 'activate' | 'pause' | 'complete' | 'cancel') => {
      setError(null);
      setPendingAction(`${action}:${campaignId}`);
      try {
        const response = await fetch('/api/billing/promo-campaigns', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ campaignId, action }),
        });
        if (!response.ok) {
          setError(`The campaign lifecycle transition was rejected (HTTP ${response.status}).`);
          return;
        }
        const refreshed = await fetch('/api/billing/promo-campaigns', { cache: 'no-store' });
        if (refreshed.ok) {
          const payload = (await refreshed.json()) as { items?: PromoCampaignRow[] };
          setCampaigns(payload.items ?? []);
        }
      } catch {
        setError('The campaign lifecycle transition could not be issued.');
      } finally {
        setPendingAction(null);
      }
    },
    [],
  );

  return (
    <div className="space-y-6">
      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Campaign dashboard error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="size-5" aria-hidden />
            Promotional campaigns
          </CardTitle>
          <CardDescription>
            Marketing campaigns grouping promo codes under one budget, acquisition spend and reporting key. Lifecycle
            transitions and performance reports (uses, discount impact, conversion, CAC attribution) run against the
            promo-campaign API.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Campaign</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Window (local)</TableHead>
                <TableHead className="text-right">Codes</TableHead>
                <TableHead className="text-right">Attributed customers</TableHead>
                <TableHead className="text-right">CAC</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {campaigns.map((campaign) => {
                const report = performance[campaign.id];
                return (
                  <TableRow key={campaign.id} data-testid={`campaign-row-${campaign.id}`}>
                    <TableCell>
                      <div className="font-medium">{campaign.name}</div>
                      <div className="text-xs text-muted-foreground">{campaign.key}</div>
                      {report ? (
                        <dl className="mt-2 grid gap-x-4 gap-y-1 text-xs text-muted-foreground sm:grid-cols-2">
                          <div className="flex justify-between gap-3">
                            <dt>Uses / unique users</dt>
                            <dd data-testid={`uses-${campaign.id}`}>
                              {report.totalUses} / {report.uniqueUsers}
                            </dd>
                          </div>
                          <div className="flex justify-between gap-3">
                            <dt>Discount given</dt>
                            <dd>{money(report.totalDiscountGiven, campaign.currency)}</dd>
                          </div>
                          <div className="flex justify-between gap-3">
                            <dt>Conversion</dt>
                            <dd>{percent(report.conversionRate)}</dd>
                          </div>
                          <div className="flex justify-between gap-3">
                            <dt>Report CAC</dt>
                            <dd>{money(report.customerAcquisitionCost, campaign.currency)}</dd>
                          </div>
                        </dl>
                      ) : null}
                    </TableCell>
                    <TableCell>
                      <Badge variant={STATUS_VARIANT[campaign.status] ?? 'outline'}>{campaign.status}</Badge>
                    </TableCell>
                    <TableCell className="text-xs">{windowLabel(campaign)}</TableCell>
                    <TableCell className="text-right">{campaign.codeCount}</TableCell>
                    <TableCell className="text-right">{campaign.attributedCustomers}</TableCell>
                    <TableCell className="text-right">
                      {money(campaign.customerAcquisitionCost, campaign.currency)}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap justify-end gap-1">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={pendingAction !== null}
                          onClick={() => {
                            void loadPerformance(campaign.id);
                          }}
                        >
                          {report ? 'Refresh report' : 'Load report'}
                        </Button>
                        {LIFECYCLE_ACTIONS.filter(({ action }) => action !== campaign.status.toLowerCase())
                          .map(({ action, label, icon: Icon }) => (
                            <Button
                              key={action}
                              type="button"
                              variant="ghost"
                              size="sm"
                              disabled={pendingAction !== null}
                              onClick={() => {
                                void runLifecycle(campaign.id, action);
                              }}
                            >
                              <Icon className="size-4" aria-hidden />
                              {label}
                            </Button>
                          ))}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
              {campaigns.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-muted-foreground">
                    No campaigns match the current filter.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
