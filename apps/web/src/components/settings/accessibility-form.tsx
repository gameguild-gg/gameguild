'use client';

import { updateAccessibilityPreferenceAction } from '@/lib/user-settings/actions';
import { applyAccessibilityPreferences } from '@/components/settings/accessibility-sync-initializer';
import {
  MAX_FONT_SIZE,
  MIN_FONT_SIZE,
  type AccessibilityPreferenceData,
} from '@/lib/user-settings/preferences-mappers';
import { Field, FieldDescription, FieldGroup, FieldLabel } from '@game-guild/ui/components/field';
import { Label } from '@game-guild/ui/components/label';
import { Slider } from '@game-guild/ui/components/slider';
import { Switch } from '@game-guild/ui/components/switch';
import { useTranslations } from 'next-intl';
import * as React from 'react';
import { toast } from 'sonner';

interface AccessibilityFormProps {
  defaultValues: AccessibilityPreferenceData;
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

export function AccessibilityForm({ defaultValues }: AccessibilityFormProps) {
  const t = useTranslations('settings.accessibility');
  const [values, setValues] = React.useState<AccessibilityPreferenceData>(defaultValues);
  const [isPending, startTransition] = React.useTransition();

  function persist(next: AccessibilityPreferenceData) {
    setValues(next);
    applyAccessibilityPreferences(next);
    startTransition(async () => {
      const result = await updateAccessibilityPreferenceAction(next);
      if (!result.success) {
        applyAccessibilityPreferences(values);
        toast.error(t('saveFailed'), { description: result.error });
      }
    });
  }

  function update<K extends keyof AccessibilityPreferenceData>(
    key: K,
    value: AccessibilityPreferenceData[K],
  ) {
    persist({ ...values, [key]: value });
  }

  const fontSize = Math.min(MAX_FONT_SIZE, Math.max(MIN_FONT_SIZE, values.fontSize));

  return (
    <FieldGroup>
      <Field>
        <FieldLabel>{t('displayLabel')}</FieldLabel>
        <div className="flex flex-col gap-3">
          <ToggleRow
            id="accessibility-high-contrast"
            label={t('highContrast.label')}
            description={t('highContrast.description')}
            checked={values.highContrast}
            disabled={isPending}
            onCheckedChange={(checked) => update('highContrast', checked)}
          />
          <ToggleRow
            id="accessibility-large-text"
            label={t('largeText.label')}
            description={t('largeText.description')}
            checked={values.largeText}
            disabled={isPending}
            onCheckedChange={(checked) => update('largeText', checked)}
          />
          <ToggleRow
            id="accessibility-reduced-motion"
            label={t('reducedMotion.label')}
            description={t('reducedMotion.description')}
            checked={values.reducedMotion}
            disabled={isPending}
            onCheckedChange={(checked) => update('reducedMotion', checked)}
          />
        </div>
      </Field>

      <Field>
        <FieldLabel htmlFor="accessibility-font-size">
          {t('fontSize.label')} <span aria-hidden="true">({fontSize}px)</span>
        </FieldLabel>
        <Slider
          id="accessibility-font-size"
          min={MIN_FONT_SIZE}
          max={MAX_FONT_SIZE}
          step={1}
          value={[fontSize]}
          // Persist on commit (pointer/key release) rather than on every
          // intermediate tick to avoid a PATCH per pixel.
          onValueCommit={(next) => update('fontSize', next[0] ?? fontSize)}
          disabled={isPending}
          aria-label={t('fontSize.label')}
        />
        <FieldDescription>
          {t('fontSize.description', { min: MIN_FONT_SIZE, max: MAX_FONT_SIZE })}
        </FieldDescription>
      </Field>
    </FieldGroup>
  );
}
