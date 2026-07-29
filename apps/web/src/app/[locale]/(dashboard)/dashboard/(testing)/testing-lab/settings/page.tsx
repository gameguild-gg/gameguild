import { TestingLabActionForm } from '@/components/testing-lab/testing-lab-action-form';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues } from '@/components/testing-lab/testing-lab-state';
import { resetTestingLabSettings, updateTestingLabSettings } from '@/lib/testing-lab/actions';
import { getTestingLabAdministration } from '@/lib/testing-lab';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Settings } from 'lucide-react';

export default async function TestingLabSettingsPage() {
  const administration = await getTestingLabAdministration();
  const settings = administration.settings;
  return (
    <main className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={Settings}
        title="Testing Lab settings"
        description="Configure tenant-wide operating defaults for public registration, approvals, notifications, duration, and concurrent sessions."
      />
      <TestingLabAccessIssues issues={administration.accessIssues} />
      <TestingLabActionForm
        action={updateTestingLabSettings}
        secondaryAction={resetTestingLabSettings}
        submitLabel="Save settings"
        secondaryLabel="Reset defaults"
        className="max-w-4xl space-y-6 rounded-md border p-5"
        actionsClassName="flex flex-wrap justify-between gap-3 border-t pt-4"
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="lab-name">Lab name</Label>
            <Input id="lab-name" name="labName" defaultValue={settings?.labName ?? 'GameGuild Testing Lab'} required />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="lab-description">Description</Label>
            <Textarea id="lab-description" name="description" rows={3} defaultValue={settings?.description ?? ''} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lab-timezone">Timezone</Label>
            <Input id="lab-timezone" name="timezone" defaultValue={settings?.timezone ?? 'UTC'} required />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lab-duration">Default session duration (minutes)</Label>
            <Input id="lab-duration" name="defaultSessionDuration" type="number" min="15" defaultValue={settings?.defaultSessionDuration ?? 120} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lab-concurrency">Maximum simultaneous sessions</Label>
            <Input id="lab-concurrency" name="maxSimultaneousSessions" type="number" min="1" defaultValue={settings?.maxSimultaneousSessions ?? 4} />
          </div>
        </div>
        <fieldset className="grid gap-3 sm:grid-cols-3">
          <legend className="mb-3 text-sm font-medium">Operating controls</legend>
          {[
            ['allowPublicSignups', 'Allow public signups', settings?.allowPublicSignups],
            ['requireApproval', 'Require request approval', settings?.requireApproval],
            ['enableNotifications', 'Enable notifications', settings?.enableNotifications],
          ].map(([name, label, enabled]) => (
            <label key={String(name)} className="flex items-center gap-3 rounded-md border p-3 text-sm">
              <input type="checkbox" name={String(name)} defaultChecked={Boolean(enabled)} className="size-4" />
              {String(label)}
            </label>
          ))}
        </fieldset>
      </TestingLabActionForm>
    </main>
  );
}
