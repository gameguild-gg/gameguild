'use client';

import { updateLocalizationPreferenceAction } from '@/lib/user-settings/actions';
import { usePathname, useRouter } from '@/i18n/navigation';
import type { LocalizationPreferenceData } from '@/lib/user-settings/preferences-mappers';
import { Button } from '@game-guild/ui/components/button';
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from '@game-guild/ui/components/field';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { Save } from 'lucide-react';
import { useTranslations } from 'next-intl';
import * as React from 'react';
import { toast } from 'sonner';

const LANGUAGE_OPTIONS = [
  { value: 'en-US', label: 'English (United States)' },
  { value: 'pt-BR', label: 'Português (Brasil)' },
] as const;

const DATE_FORMAT_OPTIONS = ['MM/dd/yyyy', 'dd/MM/yyyy', 'yyyy-MM-dd'] as const;

const TIME_FORMAT_OPTIONS = [
  { value: '12h', label: '12-hour' },
  { value: '24h', label: '24-hour' },
] as const;

const CURRENCY_OPTIONS = ['USD', 'EUR', 'BRL', 'GBP', 'CAD', 'JPY'] as const;

const FALLBACK_TIMEZONES = [
  'UTC',
  'America/Sao_Paulo',
  'America/New_York',
  'America/Chicago',
  'America/Los_Angeles',
  'Europe/London',
  'Europe/Berlin',
  'Europe/Lisbon',
  'Asia/Tokyo',
] as const;

function getTimezoneOptions(): readonly string[] {
  try {
    const supported = Intl.supportedValuesOf('timeZone');
    return supported.length > 0 ? supported : FALLBACK_TIMEZONES;
  } catch {
    return FALLBACK_TIMEZONES;
  }
}

interface LocalizationFormProps {
  defaultValues: LocalizationPreferenceData;
}

export function LocalizationForm({ defaultValues }: LocalizationFormProps) {
  const t = useTranslations('settings.localization');
  const pathname = usePathname();
  const router = useRouter();
  const [values, setValues] = React.useState<LocalizationPreferenceData>(defaultValues);
  const [isPending, startTransition] = React.useTransition();

  // The timezone list is environment-dependent; regenerate on mount so the
  // user's current value is always selectable even if it isn't in the
  // fallback list.
  const timezoneOptions = React.useMemo(getTimezoneOptions, []);
  const timezoneSelectable =
    values.timezone === 'UTC' || timezoneOptions.includes(values.timezone);

  function update<K extends keyof LocalizationPreferenceData>(
    key: K,
    value: LocalizationPreferenceData[K],
  ) {
    setValues((prev) => ({ ...prev, [key]: value }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    startTransition(async () => {
      const result = await updateLocalizationPreferenceAction(values);
      if (result.success) {
        toast.success(t('saved'));
        if (values.language === 'en-US' || values.language === 'pt-BR') {
          router.replace(pathname, { locale: values.language });
        }
      } else {
        toast.error(t('saveFailed'), { description: result.error });
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <FieldGroup>
        <Field>
          <FieldLabel htmlFor="localization-language">{t('language')}</FieldLabel>
          <Select
            value={values.language}
            onValueChange={(value) => update('language', value)}
            disabled={isPending}
          >
            <SelectTrigger id="localization-language" className="w-full sm:max-w-xs">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {LANGUAGE_OPTIONS.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>

        <Field>
          <FieldLabel htmlFor="localization-timezone">{t('timezone')}</FieldLabel>
          <Select
            value={values.timezone}
            onValueChange={(value) => update('timezone', value)}
            disabled={isPending}
          >
            <SelectTrigger id="localization-timezone" className="w-full sm:max-w-xs">
              <SelectValue />
            </SelectTrigger>
            <SelectContent className="max-h-72">
              {!timezoneSelectable && (
                <SelectItem value={values.timezone}>{values.timezone}</SelectItem>
              )}
              {timezoneOptions.map((zone) => (
                <SelectItem key={zone} value={zone}>
                  {zone}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>

        <div className="grid gap-6 sm:grid-cols-2">
          <Field>
            <FieldLabel htmlFor="localization-date-format">{t('dateFormat')}</FieldLabel>
            <Select
              value={values.dateFormat}
              onValueChange={(value) => update('dateFormat', value)}
              disabled={isPending}
            >
              <SelectTrigger id="localization-date-format" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {DATE_FORMAT_OPTIONS.map((format) => (
                  <SelectItem key={format} value={format}>
                    {format}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>

          <Field>
            <FieldLabel htmlFor="localization-time-format">{t('timeFormat')}</FieldLabel>
            <Select
              value={values.timeFormat}
              onValueChange={(value) => update('timeFormat', value)}
              disabled={isPending}
            >
              <SelectTrigger id="localization-time-format" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {TIME_FORMAT_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>
        </div>

        <Field>
          <FieldLabel htmlFor="localization-currency">{t('currency')}</FieldLabel>
          <Select
            value={values.currency}
            onValueChange={(value) => update('currency', value)}
            disabled={isPending}
          >
            <SelectTrigger id="localization-currency" className="w-full sm:max-w-xs">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {CURRENCY_OPTIONS.map((code) => (
                <SelectItem key={code} value={code}>
                  {code}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <FieldDescription>{t('currencyDescription')}</FieldDescription>
        </Field>

        <Field>
          <Button type="submit" disabled={isPending}>
            <Save className="size-4" />
            {isPending ? t('saving') : t('save')}
          </Button>
        </Field>
      </FieldGroup>
    </form>
  );
}
