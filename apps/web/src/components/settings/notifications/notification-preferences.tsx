'use client';

import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import type { PreferenceFlag } from '@/lib/notifications/preferences-action';
import {
  updateDigestFrequencyAction,
  updateMutedTypesAction,
  updatePreferenceFlagsAction,
  updateQuietHoursAction,
} from '@/lib/notifications/preferences-action';
import { Loader2 } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useMemo, useState, useTransition } from 'react';
import { toast } from 'sonner';

export interface NotificationPreferencesData {
  emailEnabled: boolean;
  inAppEnabled: boolean;
  pushEnabled: boolean;
  smsEnabled: boolean;
  marketingEnabled: boolean;
  socialEnabled: boolean;
  learningEnabled: boolean;
  achievementsEnabled: boolean;
  emailDigestFrequency: string | null;
  quietHoursStart: string | null;
  quietHoursEnd: string | null;
  timezone: string | null;
  mutedTypes: string[];
}

export interface NotificationTypeCatalogItem {
  type: string;
  displayName: string;
  category: string;
  suppressible: boolean;
}

const CHANNELS: Array<{ flag: PreferenceFlag; key: string }> = [
  { flag: 'emailEnabled', key: 'email' },
  { flag: 'inAppEnabled', key: 'inApp' },
  { flag: 'pushEnabled', key: 'push' },
  { flag: 'smsEnabled', key: 'sms' },
];

const CATEGORIES: Array<{ flag: PreferenceFlag; key: string }> = [
  { flag: 'marketingEnabled', key: 'marketing' },
  { flag: 'socialEnabled', key: 'social' },
  { flag: 'learningEnabled', key: 'learning' },
  { flag: 'achievementsEnabled', key: 'achievements' },
];

const DIGEST_OPTIONS = ['Daily', 'Weekly', 'BiWeekly'] as const;

/** TimeOnly arrives as "HH:MM:SS"; the time input wants "HH:MM". */
function toTimeInputValue(time: string | null): string {
  return time ? time.slice(0, 5) : '';
}

export function NotificationPreferences({
  preferences,
  catalog,
}: {
  preferences: NotificationPreferencesData;
  catalog: NotificationTypeCatalogItem[];
}) {
  const t = useTranslations('notificationPrefs');
  const [pending, startTransition] = useTransition();

  const [flags, setFlags] = useState({
    emailEnabled: preferences.emailEnabled,
    inAppEnabled: preferences.inAppEnabled,
    pushEnabled: preferences.pushEnabled,
    smsEnabled: preferences.smsEnabled,
    marketingEnabled: preferences.marketingEnabled,
    socialEnabled: preferences.socialEnabled,
    learningEnabled: preferences.learningEnabled,
    achievementsEnabled: preferences.achievementsEnabled,
  });
  const [mutedTypes, setMutedTypes] = useState<string[]>(preferences.mutedTypes);
  const [digest, setDigest] = useState(preferences.emailDigestFrequency ?? 'off');
  const [quietStart, setQuietStart] = useState(toTimeInputValue(preferences.quietHoursStart));
  const [quietEnd, setQuietEnd] = useState(toTimeInputValue(preferences.quietHoursEnd));
  const [timezone, setTimezone] = useState(preferences.timezone ?? '');

  const catalogGroups = useMemo(() => {
    const groups = new Map<string, NotificationTypeCatalogItem[]>();
    for (const item of catalog) {
      const list = groups.get(item.category) ?? [];
      list.push(item);
      groups.set(item.category, list);
    }
    return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [catalog]);

  function reportError(status: string) {
    toast.error(
      status === 'unauthorized' ? t('toast.unauthorized') : t('toast.error'),
    );
  }

  function toggleFlag(flag: PreferenceFlag, next: boolean) {
    const previous = flags[flag];
    setFlags((current) => ({ ...current, [flag]: next }));
    startTransition(async () => {
      const result = await updatePreferenceFlagsAction({ [flag]: next });
      if (!result.success) {
        setFlags((current) => ({ ...current, [flag]: previous }));
        reportError(result.status);
      }
    });
  }

  function toggleMuted(type: string, nextMuted: boolean) {
    const previous = mutedTypes;
    const next = nextMuted
      ? [...previous, type]
      : previous.filter((name) => name !== type);
    setMutedTypes(next);
    startTransition(async () => {
      const result = await updateMutedTypesAction(next);
      if (!result.success) {
        setMutedTypes(previous);
        reportError(result.status);
      }
    });
  }

  function changeDigest(value: string) {
    const previous = digest;
    const next = value === 'off' ? null : value;
    setDigest(value);
    startTransition(async () => {
      const result = await updateDigestFrequencyAction(next);
      if (!result.success) {
        setDigest(previous);
        reportError(result.status);
      }
    });
  }

  function saveQuietHours() {
    const normalize = (value: string) => (value ? `${value}:00` : null);
    startTransition(async () => {
      const result = await updateQuietHoursAction(
        normalize(quietStart),
        normalize(quietEnd),
        timezone.trim() || null,
      );
      if (result.success) {
        toast.success(t('toast.saved'));
      } else {
        reportError(result.status);
      }
    });
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>{t('channels.title')}</CardTitle>
          <CardDescription>{t('channels.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {CHANNELS.map(({ flag, key }) => (
            <div
              key={flag}
              data-testid={`channel-${key}`}
              className="flex items-center justify-between rounded-lg border p-4"
            >
              <Label htmlFor={`channel-${key}-switch`}>{t(`channels.${key}`)}</Label>
              <Switch
                id={`channel-${key}-switch`}
                checked={flags[flag]}
                disabled={pending}
                onCheckedChange={(checked) => toggleFlag(flag, checked)}
              />
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('categories.title')}</CardTitle>
          <CardDescription>{t('categories.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {CATEGORIES.map(({ flag, key }) => (
            <div
              key={flag}
              data-testid={`category-${key}`}
              className="flex items-center justify-between rounded-lg border p-4"
            >
              <Label htmlFor={`category-${key}-switch`}>{t(`categories.${key}`)}</Label>
              <Switch
                id={`category-${key}-switch`}
                checked={flags[flag]}
                disabled={pending}
                onCheckedChange={(checked) => toggleFlag(flag, checked)}
              />
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('types.title')}</CardTitle>
          <CardDescription>{t('types.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {catalogGroups.map(([category, items]) => (
            <section key={category} data-testid={`type-group-${category}`}>
              <h3 className="mb-3 text-sm font-semibold text-muted-foreground">
                {t(`types.groups.${category}`)}
              </h3>
              <ul className="space-y-2">
                {items.map((item) => (
                  <li
                    key={item.type}
                    data-testid={`type-${item.type}`}
                    className="flex items-center justify-between gap-3 rounded-lg border p-3"
                  >
                    <span className="text-sm font-medium">{item.displayName}</span>
                    {item.suppressible ? (
                      <Switch
                        aria-label={item.displayName}
                        checked={!mutedTypes.includes(item.type)}
                        disabled={pending}
                        onCheckedChange={(checked) => toggleMuted(item.type, !checked)}
                      />
                    ) : (
                      <span className="text-xs text-muted-foreground">
                        {t('types.alwaysSent')}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('digest.title')}</CardTitle>
          <CardDescription>{t('digest.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          <Select value={digest} onValueChange={changeDigest} disabled={pending}>
            <SelectTrigger className="w-full sm:w-64" aria-label={t('digest.title')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="off">{t('digest.off')}</SelectItem>
              {DIGEST_OPTIONS.map((option) => (
                <SelectItem key={option} value={option}>
                  {t(`digest.${option}`)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('quietHours.title')}</CardTitle>
          <CardDescription>{t('quietHours.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4">
            <div className="space-y-2">
              <Label htmlFor="quiet-hours-start">{t('quietHours.start')}</Label>
              <Input
                id="quiet-hours-start"
                type="time"
                value={quietStart}
                onChange={(event) => setQuietStart(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="quiet-hours-end">{t('quietHours.end')}</Label>
              <Input
                id="quiet-hours-end"
                type="time"
                value={quietEnd}
                onChange={(event) => setQuietEnd(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="quiet-hours-timezone">{t('quietHours.timezone')}</Label>
              <Input
                id="quiet-hours-timezone"
                placeholder={t('quietHours.timezonePlaceholder')}
                value={timezone}
                onChange={(event) => setTimezone(event.target.value)}
              />
            </div>
          </div>
          <Button onClick={saveQuietHours} disabled={pending}>
            {pending ? <Loader2 className="size-4 animate-spin" /> : null}
            {t('quietHours.save')}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
