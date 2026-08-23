'use client';

import { updatePrivacyPreferenceAction } from '@/lib/user-settings/actions';
import type {
  PrivacyPreferenceData,
  ProfileVisibility,
} from '@/lib/user-settings/preferences-mappers';
import { Field, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Label } from '@game-guild/ui/components/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import { useTranslations } from 'next-intl';
import * as React from 'react';
import { toast } from 'sonner';

const VISIBILITY_OPTIONS = [
  { value: 'public', labelKey: 'visibility.public' },
  { value: 'members', labelKey: 'visibility.members' },
  { value: 'private', labelKey: 'visibility.private' },
] as const;

interface PrivacyFormProps {
  defaultValues: PrivacyPreferenceData;
}

function isProfileVisibility(value: string): value is ProfileVisibility {
  return value === 'public' || value === 'members' || value === 'private';
}

interface ToggleRowProps {
  id: string;
  label: string;
  description: string;
  checked: boolean;
  disabled: boolean;
  onCheckedChange: (checked: boolean) => void;
}

function ToggleRow({
  id,
  label,
  description,
  checked,
  disabled,
  onCheckedChange,
}: ToggleRowProps) {
  return (
    <div className="flex items-start justify-between gap-4 rounded-lg border p-4">
      <div className="min-w-0">
        <Label htmlFor={id} className="text-sm font-medium">
          {label}
        </Label>
        <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      </div>
      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        onCheckedChange={onCheckedChange}
        aria-label={label}
      />
    </div>
  );
}

export function PrivacyForm({ defaultValues }: PrivacyFormProps) {
  const t = useTranslations('settings.privacy');
  const [values, setValues] = React.useState<PrivacyPreferenceData>(defaultValues);
  const [isPending, startTransition] = React.useTransition();

  function persist(next: PrivacyPreferenceData) {
    setValues(next);
    startTransition(async () => {
      const result = await updatePrivacyPreferenceAction(next);
      if (!result.success) {
        toast.error(t('saveFailed'), { description: result.error });
      }
    });
  }

  function update<K extends keyof PrivacyPreferenceData>(
    key: K,
    value: PrivacyPreferenceData[K],
  ) {
    persist({ ...values, [key]: value });
  }

  return (
    <FieldGroup>
      <Field>
        <FieldLabel htmlFor="privacy-visibility">{t('profileVisibility.label')}</FieldLabel>
        <Select
          value={values.profileVisibility}
          onValueChange={(value) => {
            if (isProfileVisibility(value)) update('profileVisibility', value);
          }}
          disabled={isPending}
        >
          <SelectTrigger id="privacy-visibility" className="w-full sm:max-w-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {VISIBILITY_OPTIONS.map(({ value, labelKey }) => (
              <SelectItem key={value} value={value}>
                {t(labelKey)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field>
        <FieldLabel>{t('sharingLabel')}</FieldLabel>
        <div className="flex flex-col gap-3">
          <ToggleRow
            id="privacy-activity-tracking"
            label={t('activityTracking.label')}
            description={t('activityTracking.description')}
            checked={values.activityTracking}
            disabled={isPending}
            onCheckedChange={(checked) => update('activityTracking', checked)}
          />
          <ToggleRow
            id="privacy-marketing-emails"
            label={t('marketingEmails.label')}
            description={t('marketingEmails.description')}
            checked={values.marketingEmails}
            disabled={isPending}
            onCheckedChange={(checked) => update('marketingEmails', checked)}
          />
          <ToggleRow
            id="privacy-analytics-cookies"
            label={t('analyticsCookies.label')}
            description={t('analyticsCookies.description')}
            checked={values.analyticsCookies}
            disabled={isPending}
            onCheckedChange={(checked) => update('analyticsCookies', checked)}
          />
          <ToggleRow
            id="privacy-personalized-content"
            label={t('personalizedContent.label')}
            description={t('personalizedContent.description')}
            checked={values.personalizedContent}
            disabled={isPending}
            onCheckedChange={(checked) => update('personalizedContent', checked)}
          />
        </div>
      </Field>
    </FieldGroup>
  );
}
