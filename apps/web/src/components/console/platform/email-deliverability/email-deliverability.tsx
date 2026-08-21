'use client';

import {
  getNotificationTimelineAction,
  requeueNotificationAction,
  unsuppressEmailAction,
  type TimelineEvent,
} from '@/lib/notifications/email-deliverability-actions';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { Loader2, Mail, MailOpen, MailWarning, ShieldOff } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useMemo, useState, useTransition } from 'react';
import { toast } from 'sonner';

export interface DeliverabilityEvent {
  id: string;
  occurredAt: string;
  eventType: string;
  recipientEmail: string;
  providerMessageId: string;
  bounceType: string | null;
  diagnosticCode: string | null;
  payloadPreview: string | null;
}

export interface DeliverabilitySuppression {
  id: string;
  emailAddress: string;
  reason: string;
  bounceType: string | null;
  suppressedAt: string;
  releasedAt: string | null;
  isActive: boolean;
}

export interface DeliverabilityDeadLetter {
  id: string;
  title: string;
  type: string;
  channel: string;
  recipientEmail: string | null;
  recipientId: string | null;
  lastError: string | null;
  attemptCount: number;
  requeueCount: number;
  createdAt: string;
}

interface TimelineState {
  notificationId: string;
  loading: boolean;
  error: boolean;
  providerMessageId: string | null;
  events: TimelineEvent[];
}

/** Deterministic UTC rendering ("2026-08-19 10:01") — stable across locales and test environments. */
function formatUtc(iso: string): string {
  return iso ? `${iso.slice(0, 10)} ${iso.slice(11, 16)} UTC` : '—';
}

function eventTypeBadgeVariant(eventType: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  if (eventType === 'Bounce' || eventType === 'Complaint') return 'destructive';
  if (eventType === 'Delivery') return 'default';
  if (eventType === 'Open') return 'secondary';
  return 'outline';
}

export function EmailDeliverability({
  events,
  suppressions,
  deadLetters,
}: {
  events: DeliverabilityEvent[];
  suppressions: DeliverabilitySuppression[];
  deadLetters: DeliverabilityDeadLetter[];
}) {
  const t = useTranslations('emailDeliverability');
  const [isPending, startTransition] = useTransition();
  const [tab, setTab] = useState('events');
  const [deadLetterFilter, setDeadLetterFilter] = useState<string | null>(null);
  const [confirmUnsuppress, setConfirmUnsuppress] = useState<DeliverabilitySuppression | null>(null);
  const [timeline, setTimeline] = useState<TimelineState | null>(null);

  const activeSuppressedEmails = useMemo(
    () => new Set(suppressions.filter((s) => s.isActive).map((s) => s.emailAddress.toLowerCase())),
    [suppressions],
  );

  const visibleDeadLetters = useMemo(
    () =>
      deadLetterFilter
        ? deadLetters.filter((row) => row.recipientEmail?.toLowerCase() === deadLetterFilter.toLowerCase())
        : deadLetters,
    [deadLetters, deadLetterFilter],
  );

  function reportActionFailure(status: string) {
    toast.error(status === 'unauthorized' ? t('toast.unauthorized') : t('toast.error'));
  }

  function handleUnsuppress(email: string) {
    startTransition(async () => {
      const result = await unsuppressEmailAction(email);
      if (result.success) {
        toast.success(t('toast.unsuppressed'));
      } else {
        reportActionFailure(result.status);
      }
    });
  }

  function handleRequeue(notificationId: string) {
    startTransition(async () => {
      const result = await requeueNotificationAction(notificationId);
      if (result.success) {
        toast.success(t('toast.requeued'));
      } else {
        reportActionFailure(result.status);
      }
    });
  }

  async function handleOpenTimeline(deadLetter: DeliverabilityDeadLetter) {
    setTimeline({ notificationId: deadLetter.id, loading: true, error: false, providerMessageId: null, events: [] });
    const result = await getNotificationTimelineAction(deadLetter.id);
    setTimeline({
      notificationId: deadLetter.id,
      loading: false,
      error: !result.success,
      providerMessageId: result.providerMessageId,
      // Defensive sort: the API returns oldest-first, keep it guaranteed chronological.
      events: [...result.events].sort((a, b) => a.occurredAt.localeCompare(b.occurredAt)),
    });
  }

  function viewDeadLettersFor(email: string) {
    setDeadLetterFilter(email);
    setTab('deadLetters');
  }

  return (
    <Tabs value={tab} onValueChange={setTab}>
      <TabsList>
        <TabsTrigger value="events">{t('tabs.events')}</TabsTrigger>
        <TabsTrigger value="suppressions">{t('tabs.suppressions')}</TabsTrigger>
        <TabsTrigger value="deadLetters">{t('tabs.deadLetters')}</TabsTrigger>
      </TabsList>

      <TabsContent value="events">
        <Card>
          <CardHeader>
            <CardTitle>{t('tabs.events')}</CardTitle>
            <CardDescription>{t('events.description')}</CardDescription>
          </CardHeader>
          <CardContent>
            {events.length === 0 ? (
              <div data-testid="empty-events" className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
                {t('events.empty')}
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('events.time')}</TableHead>
                    <TableHead>{t('events.type')}</TableHead>
                    <TableHead>{t('events.recipient')}</TableHead>
                    <TableHead>{t('events.diagnostic')}</TableHead>
                    <TableHead className="text-right" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {events.map((event) => (
                    <TableRow key={event.id}>
                      <TableCell className="whitespace-nowrap text-sm" title={event.occurredAt}>
                        {formatUtc(event.occurredAt)}
                      </TableCell>
                      <TableCell>
                        <Badge variant={eventTypeBadgeVariant(event.eventType)}>
                          {t(`events.types.${event.eventType}`, { defaultMessage: event.eventType })}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm">{event.recipientEmail}</TableCell>
                      <TableCell className="max-w-72 text-sm">
                        {event.bounceType || event.diagnosticCode ? (
                          <span className="flex flex-col">
                            {event.bounceType ? <span className="font-medium">{event.bounceType}</span> : null}
                            {event.diagnosticCode ? (
                              <span className="text-xs text-muted-foreground" title={event.diagnosticCode}>
                                {event.diagnosticCode}
                              </span>
                            ) : null}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => viewDeadLettersFor(event.recipientEmail)}
                        >
                          {t('events.viewDeadLetters')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="suppressions">
        <Card>
          <CardHeader>
            <CardTitle>{t('tabs.suppressions')}</CardTitle>
            <CardDescription>{t('suppressions.description')}</CardDescription>
          </CardHeader>
          <CardContent>
            {suppressions.length === 0 ? (
              <div data-testid="empty-suppressions" className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
                {t('suppressions.empty')}
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('suppressions.email')}</TableHead>
                    <TableHead>{t('suppressions.reason')}</TableHead>
                    <TableHead>{t('suppressions.bounceType')}</TableHead>
                    <TableHead>{t('suppressions.suppressedAt')}</TableHead>
                    <TableHead>{t('suppressions.releasedAt')}</TableHead>
                    <TableHead className="text-right">{t('suppressions.unsuppress')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {suppressions.map((suppression) => (
                    <TableRow key={suppression.id}>
                      <TableCell className="flex items-center gap-2 font-medium">
                        {suppression.isActive ? <ShieldOff className="size-4 text-destructive" /> : null}
                        {suppression.emailAddress}
                      </TableCell>
                      <TableCell>
                        <Badge variant={suppression.isActive ? 'destructive' : 'outline'}>
                          {t(`suppressions.reasons.${suppression.reason}`, { defaultMessage: suppression.reason })}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">{suppression.bounceType ?? '—'}</TableCell>
                      <TableCell className="whitespace-nowrap text-sm" title={suppression.suppressedAt}>
                        {formatUtc(suppression.suppressedAt)}
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-sm text-muted-foreground" title={suppression.releasedAt ?? undefined}>
                        {suppression.releasedAt ? formatUtc(suppression.releasedAt) : '—'}
                      </TableCell>
                      <TableCell className="text-right">
                        {suppression.isActive ? (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={isPending}
                            onClick={() => setConfirmUnsuppress(suppression)}
                          >
                            {t('suppressions.unsuppress')}
                          </Button>
                        ) : (
                          <span className="text-sm text-muted-foreground">—</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="deadLetters">
        <Card>
          <CardHeader>
            <CardTitle>{t('tabs.deadLetters')}</CardTitle>
            <CardDescription>{t('deadLetters.description')}</CardDescription>
            {deadLetterFilter ? (
              <Button type="button" variant="ghost" size="sm" className="w-fit" onClick={() => setDeadLetterFilter(null)}>
                {t('deadLetters.clearFilter')}
              </Button>
            ) : null}
          </CardHeader>
          <CardContent>
            {visibleDeadLetters.length === 0 ? (
              <div data-testid="empty-deadletters" className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
                {t('deadLetters.empty')}
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('deadLetters.title')}</TableHead>
                    <TableHead>{t('deadLetters.type')}</TableHead>
                    <TableHead>{t('deadLetters.recipient')}</TableHead>
                    <TableHead>{t('deadLetters.error')}</TableHead>
                    <TableHead>{t('deadLetters.attempts')}</TableHead>
                    <TableHead className="text-right">{t('deadLetters.requeue')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {visibleDeadLetters.map((row) => {
                    const suppressed = row.recipientEmail
                      ? activeSuppressedEmails.has(row.recipientEmail.toLowerCase())
                      : false;

                    return (
                      <TableRow key={row.id}>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <MailWarning className="size-4 text-muted-foreground" />
                            <span className="font-medium">{row.title}</span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">{row.type}</Badge>
                        </TableCell>
                        <TableCell className="text-sm">{row.recipientEmail ?? '—'}</TableCell>
                        <TableCell className="max-w-72 text-sm">
                          {row.lastError ? (
                            <span className="block truncate text-muted-foreground" title={row.lastError}>
                              {row.lastError}
                            </span>
                          ) : (
                            '—'
                          )}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {row.attemptCount} / {t('deadLetters.requeues')} {row.requeueCount}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => handleOpenTimeline(row)}
                              aria-label={`${t('deadLetters.viewTimeline')}: ${row.title}`}
                            >
                              {t('deadLetters.viewTimeline')}
                            </Button>
                            {suppressed ? (
                              <Tooltip>
                                <TooltipTrigger asChild>
                                  <span className="inline-flex">
                                    <Button type="button" variant="outline" size="sm" disabled>
                                      {t('deadLetters.requeue')}
                                    </Button>
                                  </span>
                                </TooltipTrigger>
                                <TooltipContent>{t('deadLetters.suppressedTooltip')}</TooltipContent>
                              </Tooltip>
                            ) : (
                              <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                disabled={isPending}
                                onClick={() => handleRequeue(row.id)}
                              >
                                {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
                                {t('deadLetters.requeue')}
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <AlertDialog
        open={confirmUnsuppress !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmUnsuppress(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('suppressions.confirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('suppressions.confirmDescription', { email: confirmUnsuppress?.emailAddress ?? '' })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => setConfirmUnsuppress(null)}>{t('suppressions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (confirmUnsuppress) handleUnsuppress(confirmUnsuppress.emailAddress);
                setConfirmUnsuppress(null);
              }}
            >
              {t('suppressions.confirmAction')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <Sheet open={timeline !== null} onOpenChange={(open) => (open ? null : setTimeline(null))}>
        <SheetContent className="w-full overflow-y-auto sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{t('timeline.title')}</SheetTitle>
            <SheetDescription>{t('timeline.description')}</SheetDescription>
          </SheetHeader>
          {timeline?.loading ? (
            <div className="flex items-center gap-2 px-4 py-8 text-sm text-muted-foreground">
              <Loader2 className="size-4 animate-spin" />
            </div>
          ) : timeline?.error ? (
            <p className="px-4 py-8 text-sm text-destructive">{t('toast.error')}</p>
          ) : timeline && timeline.providerMessageId === null ? (
            <p data-testid="timeline-no-correlation" className="px-4 py-8 text-sm text-muted-foreground">
              {t('timeline.noCorrelation')}
            </p>
          ) : timeline && timeline.events.length === 0 ? (
            <p data-testid="timeline-empty" className="px-4 py-8 text-sm text-muted-foreground">
              {t('timeline.empty')}
            </p>
          ) : timeline ? (
            <ol data-testid="timeline-events" className="mt-2 space-y-3 px-4 pb-8">
              {timeline.events.map((event) => (
                <li key={event.id} className="flex gap-3 rounded-lg border p-3" data-testid={`timeline-event-${event.id}`}>
                  <span className="mt-0.5">
                    {event.eventType === 'Open' ? (
                      <MailOpen className="size-4 text-muted-foreground" />
                    ) : (
                      <Mail className="size-4 text-muted-foreground" />
                    )}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <Badge variant={eventTypeBadgeVariant(event.eventType)}>
                        {t(`events.types.${event.eventType}`, { defaultMessage: event.eventType })}
                      </Badge>
                      <span className="text-xs text-muted-foreground" title={event.occurredAt}>
                        {formatUtc(event.occurredAt)}
                      </span>
                    </div>
                    <p className="mt-1 truncate text-sm">{event.recipientEmail}</p>
                    {event.bounceType ? (
                      <p className="text-xs text-muted-foreground">{event.bounceType}</p>
                    ) : null}
                    {event.diagnosticCode ? (
                      <p className="truncate text-xs text-muted-foreground" title={event.diagnosticCode}>
                        {event.diagnosticCode}
                      </p>
                    ) : null}
                    {event.payloadPreview ? (
                      <details className="mt-1">
                        <summary className="cursor-pointer text-xs text-muted-foreground">{t('timeline.payload')}</summary>
                        {/* API-capped 500-char preview — the raw payload is never exposed. */}
                        <pre className="mt-1 max-h-32 overflow-auto whitespace-pre-wrap break-all rounded bg-muted p-2 text-xs">
                          {event.payloadPreview}
                        </pre>
                      </details>
                    ) : null}
                  </div>
                </li>
              ))}
            </ol>
          ) : null}
        </SheetContent>
      </Sheet>
    </Tabs>
  );
}
