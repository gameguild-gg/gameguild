'use client';

import { updateThemePreferenceAction } from '@/lib/user-settings/actions';
import {
  parseThemePreference,
  type ThemePreference,
} from '@/lib/user-settings/preferences-mappers';
import { Field, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Label } from '@game-guild/ui/components/label';
import { RadioGroup, RadioGroupItem } from '@game-guild/ui/components/radio-group';
import { cn } from '@/lib/utils';
import { Monitor, Moon, Sun } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useTheme } from 'next-themes';
import * as React from 'react';
import { toast } from 'sonner';

const THEME_OPTIONS: Array<{
  value: ThemePreference;
  icon: typeof Sun;
  labelKey: string;
  descriptionKey: string;
}> = [
  { value: 'light', icon: Sun, labelKey: 'theme.light', descriptionKey: 'theme.lightDescription' },
  { value: 'dark', icon: Moon, labelKey: 'theme.dark', descriptionKey: 'theme.darkDescription' },
  {
    value: 'system',
    icon: Monitor,
    labelKey: 'theme.system',
    descriptionKey: 'theme.systemDescription',
  },
];

interface AppearanceFormProps {
  initialTheme: ThemePreference | null;
}

export function AppearanceForm({ initialTheme }: AppearanceFormProps) {
  const t = useTranslations('settings.appearance');
  const { theme: activeTheme, setTheme } = useTheme();
  const [isPending, startTransition] = React.useTransition();

  // The server value is the source of truth until next-themes reports the
  // locally-active theme after hydration.
  const selected = parseThemePreference(activeTheme) ?? initialTheme ?? 'system';

  function selectTheme(next: string) {
    const theme = parseThemePreference(next);
    if (!theme) return;
    // Instant local response; the server sync is eventual.
    setTheme(theme);

    startTransition(async () => {
      const result = await updateThemePreferenceAction(theme);
      if (!result.success) {
        toast.error(t('saveFailed'), { description: result.error });
      }
    });
  }

  return (
    <FieldGroup>
      <Field>
        <FieldLabel>{t('theme.label')}</FieldLabel>
        <RadioGroup
          value={selected}
          onValueChange={selectTheme}
          disabled={isPending}
          className="grid gap-3 sm:grid-cols-3"
        >
          {THEME_OPTIONS.map(({ value, icon: Icon, labelKey, descriptionKey }) => (
            <Label
              key={value}
              htmlFor={`theme-${value}`}
              className={cn(
                'flex cursor-pointer flex-col items-start gap-2 rounded-lg border p-4 transition-colors',
                selected === value
                  ? 'border-primary bg-muted/60'
                  : 'hover:bg-muted/40',
              )}
            >
              <span className="flex w-full items-center justify-between">
                <Icon className="size-5 text-muted-foreground" />
                <RadioGroupItem id={`theme-${value}`} value={value} />
              </span>
              <span className="text-sm font-medium">{t(labelKey)}</span>
              <span className="text-xs font-normal text-muted-foreground">
                {t(descriptionKey)}
              </span>
            </Label>
          ))}
        </RadioGroup>
      </Field>
    </FieldGroup>
  );
}
