import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { z } from 'zod';
import { cn } from '@/lib/utils';

// Lightweight IANA timezone list (can be expanded or dynamically loaded later)
const TIMEZONES = [
  'UTC', 'America/New_York', 'America/Los_Angeles', 'Europe/London', 'Europe/Berlin', 'Asia/Singapore', 'Asia/Tokyo', 'Australia/Sydney'
] as const;

// Validation schema (can be exported for tests)
export const generalSettingsSchema = z.object({
  labName: z.string().trim().min(3, 'Name must be at least 3 characters').max(80, 'Name must be at most 80 characters'),
  description: z.string().trim().max(500, 'Description must be at most 500 characters').optional().default(''),
  timezone: z.enum(TIMEZONES, { message: 'Select a valid timezone' }),
  defaultSessionDuration: z.number({ invalid_type_error: 'Duration must be a number' })
    .int('Must be an integer')
    .min(15, 'Minimum 15 minutes')
    .max(480, 'Maximum 480 minutes'),
  allowPublicSignups: z.boolean(),
  requireApproval: z.boolean(),
  enableNotifications: z.boolean(),
  maxSimultaneousSessions: z.number({ invalid_type_error: 'Max sessions must be a number' })
    .int('Must be an integer').min(1, 'At least 1').max(100, 'Too large (max 100)'),
});

export interface GeneralSettingsState {
  labName: string;
  description: string;
  timezone: string; // constrained by schema at runtime
  defaultSessionDuration: number;
  allowPublicSignups: boolean;
  requireApproval: boolean;
  enableNotifications: boolean;
  maxSimultaneousSessions: number;
}

interface GeneralSettingsProps {
  generalSettings: GeneralSettingsState;
  setGeneralSettings: (s: GeneralSettingsState) => void;
  saveGeneralSettings: (s: GeneralSettingsState) => Promise<void>;
}

export function GeneralSettings({ generalSettings, setGeneralSettings, saveGeneralSettings }: GeneralSettingsProps) {
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isDirty, setIsDirty] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const autosaveTimer = useRef<NodeJS.Timeout | null>(null);
  const liveRegionRef = useRef<HTMLDivElement | null>(null);

  const safeParse = useCallback((state: GeneralSettingsState) => {
    return generalSettingsSchema.safeParse({ ...state });
  }, []);

  // Validate whenever state changes
  useEffect(() => {
    const result = safeParse(generalSettings);
    if (!result.success) {
      const fieldErrors: Record<string, string> = {};
      result.error.issues.forEach(issue => {
        const key = issue.path[0];
        if (typeof key === 'string' && !fieldErrors[key]) fieldErrors[key] = issue.message;
      });
      setErrors(fieldErrors);
    } else {
      setErrors({});
    }
  }, [generalSettings, safeParse]);

  const handleChange = (field: keyof GeneralSettingsState, value: any) => {
    setGeneralSettings({ ...generalSettings, [field]: value });
    setTouched(prev => ({ ...prev, [field]: true }));
    setIsDirty(true);
    // Debounced autosave (only if form currently passes validation)
    if (autosaveTimer.current) clearTimeout(autosaveTimer.current);
    autosaveTimer.current = setTimeout(() => {
      const result = safeParse({ ...generalSettings, [field]: value });
      if (result.success) {
        void handleSave(result.data, true);
      }
    }, 1200);
  };

  const handleSave = async (state: GeneralSettingsState, silent = false) => {
    const result = safeParse(state);
    if (!result.success) {
      if (!silent) announce('Cannot save. Please fix validation errors.');
      return;
    }
    try {
      setIsSaving(true);
      await saveGeneralSettings(result.data);
      setIsDirty(false);
      if (!silent) announce('Settings saved successfully');
      else announce('Autosaved');
    } catch (e) {
      announce('Failed to save settings');
    } finally {
      setIsSaving(false);
    }
  };

  const announce = (msg: string) => {
    if (liveRegionRef.current) {
      liveRegionRef.current.textContent = msg;
    }
  };

  const hasError = (field: keyof GeneralSettingsState) => !!errors[field as string];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold mb-2">General Settings</h2>
        <p className="text-muted-foreground">
          Configure the core operational settings for your Testing Lab.
        </p>
        <div className="mt-2 flex items-center gap-3 text-sm">
          <span className={ cn('px-2 py-0.5 rounded-md border text-xs', isDirty ? 'bg-amber-50 border-amber-300 text-amber-700' : 'bg-muted text-muted-foreground') }>
            { isDirty ? 'Unsaved changes' : 'All changes saved' }
          </span>
          { isSaving && <span className="text-xs text-muted-foreground animate-pulse">Saving…</span> }
        </div>
        <div ref={liveRegionRef} aria-live="polite" className="sr-only" />
      </div>

      <Separator />

      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Lab Information</CardTitle>
            <CardDescription>
              Basic information about your testing laboratory
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="labName">Lab Name</Label>
                <Input
                  id="labName"
                  aria-invalid={hasError('labName')}
                  aria-describedby={hasError('labName') ? 'labName-error' : undefined}
                  value={generalSettings.labName}
                  onChange={(e) => handleChange('labName', e.target.value)}
                  onBlur={() => setTouched(p => ({ ...p, labName: true }))}
                />
                { touched.labName && hasError('labName') && (
                  <p id="labName-error" className="text-xs text-destructive">{ errors.labName }</p>
                ) }
              </div>
              <div className="space-y-2">
                <Label htmlFor="timezone">Timezone</Label>
                <Select
                  value={TIMEZONES.includes(generalSettings.timezone as any) ? generalSettings.timezone : 'UTC'}
                  onValueChange={(val) => handleChange('timezone', val)}
                >
                  <SelectTrigger id="timezone" aria-invalid={hasError('timezone')} aria-describedby={hasError('timezone') ? 'timezone-error' : undefined}>
                    <SelectValue placeholder="Select timezone" />
                  </SelectTrigger>
                  <SelectContent className="max-h-64">
                    { TIMEZONES.map(tz => <SelectItem key={tz} value={tz}>{tz}</SelectItem>) }
                  </SelectContent>
                </Select>
                { touched.timezone && hasError('timezone') && (
                  <p id="timezone-error" className="text-xs text-destructive">{ errors.timezone }</p>
                ) }
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                aria-invalid={hasError('description')}
                aria-describedby={hasError('description') ? 'description-error' : undefined}
                value={generalSettings.description}
                onChange={(e) => handleChange('description', e.target.value)}
                placeholder="Describe your testing lab..."
              />
              { touched.description && hasError('description') && (
                <p id="description-error" className="text-xs text-destructive">{ errors.description }</p>
              ) }
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Session Settings</CardTitle>
            <CardDescription>
              Default settings for testing sessions
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="defaultDuration">Default Session Duration (minutes)</Label>
                <Input
                  id="defaultDuration"
                  type="number"
                  aria-invalid={hasError('defaultSessionDuration')}
                  aria-describedby={hasError('defaultSessionDuration') ? 'defaultSessionDuration-error' : undefined}
                  value={generalSettings.defaultSessionDuration}
                  onChange={(e) => handleChange('defaultSessionDuration', Number(e.target.value))}
                  onBlur={() => setTouched(p => ({ ...p, defaultSessionDuration: true }))}
                />
                { touched.defaultSessionDuration && hasError('defaultSessionDuration') && (
                  <p id="defaultSessionDuration-error" className="text-xs text-destructive">{ errors.defaultSessionDuration }</p>
                ) }
              </div>
              <div className="space-y-2">
                <Label htmlFor="maxSessions">Max Simultaneous Sessions</Label>
                <Input
                  id="maxSessions"
                  type="number"
                  aria-invalid={hasError('maxSimultaneousSessions')}
                  aria-describedby={hasError('maxSimultaneousSessions') ? 'maxSimultaneousSessions-error' : undefined}
                  value={generalSettings.maxSimultaneousSessions}
                  onChange={(e) => handleChange('maxSimultaneousSessions', Number(e.target.value))}
                  onBlur={() => setTouched(p => ({ ...p, maxSimultaneousSessions: true }))}
                />
                { touched.maxSimultaneousSessions && hasError('maxSimultaneousSessions') && (
                  <p id="maxSimultaneousSessions-error" className="text-xs text-destructive">{ errors.maxSimultaneousSessions }</p>
                ) }
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Access Control</CardTitle>
            <CardDescription>
              Control how users can access and join testing sessions
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Allow Public Signups</Label>
                <p className="text-sm text-muted-foreground">
                  Allow anyone to sign up for testing sessions
                </p>
              </div>
              <Switch
                checked={generalSettings.allowPublicSignups}
                onCheckedChange={(value) => handleChange('allowPublicSignups', value)}
                aria-label="Allow Public Signups"
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Require Approval</Label>
                <p className="text-sm text-muted-foreground">
                  Require manager approval for new testing participants
                </p>
              </div>
              <Switch
                checked={generalSettings.requireApproval}
                onCheckedChange={(value) => handleChange('requireApproval', value)}
                aria-label="Require Approval"
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Enable Notifications</Label>
                <p className="text-sm text-muted-foreground">
                  Send email notifications for session updates
                </p>
              </div>
              <Switch
                checked={generalSettings.enableNotifications}
                onCheckedChange={(value) => handleChange('enableNotifications', value)}
                aria-label="Enable Notifications"
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button
            onClick={() => void handleSave(generalSettings)}
            disabled={Object.keys(errors).length > 0 || isSaving || !isDirty}
          >
            { isSaving ? 'Saving…' : 'Save Settings' }
          </Button>
        </div>
      </div>
    </div>
  );
}
