'use client';

import type { EconomyConsoleData } from '@/lib/economy/console';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { EconomyIssue, EconomyPageHeader, EconomyWorkspace } from './economy-ui';
import { useTranslations } from 'next-intl';
import type { EconomyConsoleSurface } from '@/lib/economy/console';
import { EconomyConsoleActions } from './economy-console-actions';

const visibleKeys = ['id', 'state', 'status', 'type', 'capability', 'scope', 'version', 'currency', 'amountUnits', 'updatedAt', 'createdAt', 'expiresAt', 'isHealthy', 'ready'];

function text(value: unknown, t: (key: string, values?: Record<string, number>) => string) {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'boolean') return value ? t('common.yes') : t('common.no');
  if (typeof value === 'string' || typeof value === 'number') return String(value);
  return Array.isArray(value) ? t('common.itemCount', { count: value.length }) : t('common.detailAvailable');
}

export function EconomyOperationalConsole({ data, description, surface, title }: {
  data: EconomyConsoleData;
  description: string;
  surface: EconomyConsoleSurface;
  title: string;
}) {
  const t = useTranslations('economy');
  return (
    <EconomyWorkspace>
      <EconomyPageHeader title={title} description={description} badge={t('common.tenantScoped')} />
      <EconomyIssue issue={data.issue} />
      <EconomyConsoleActions surface={surface} />
      {data.sections.map((section) => (
        <section key={section.label} className="space-y-3">
          <div className="flex items-center gap-2"><h2 className="text-lg font-semibold">{section.label}</h2><Badge variant="secondary">{section.records.length}</Badge></div>
          <div className="grid gap-3 lg:grid-cols-2">
            {section.records.length ? section.records.map((record, index) => {
              const fields = visibleKeys.filter((key) => key in record).slice(0, 6);
              return (
                <Card key={String(record.id ?? `${section.label}-${index}`)}>
                  <CardHeader><CardTitle className="text-base">{text(record.id ?? record.capability ?? record.type ?? `${section.label} ${index + 1}`, t)}</CardTitle><CardDescription>{text(record.state ?? record.status ?? t('common.recorded'), t)}</CardDescription></CardHeader>
                  <CardContent className="grid gap-2 text-sm sm:grid-cols-2">
                    {fields.map((key) => <div key={key}><span className="text-muted-foreground">{key}</span><p className="truncate font-medium">{text(record[key], t)}</p></div>)}
                  </CardContent>
                </Card>
              );
            }) : <p className="text-sm text-muted-foreground">{t('common.empty')}</p>}
          </div>
        </section>
      ))}
    </EconomyWorkspace>
  );
}
