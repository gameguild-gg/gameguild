import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';
import { getLaunchPadSettings } from '@/lib/launch-pad/queries';
import { LaunchPadSettingsForm } from '@/components/launch-pad/launch-pad-settings-form';

export default async function LaunchPadSettingsPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ManageSettings')) forbidden();
  const settings = await getLaunchPadSettings();
  return <div className="space-y-6 p-6"><header><h1 className="text-3xl font-bold">Launch Pad settings</h1><p className="text-muted-foreground">Tenant-scoped release eligibility for Launch Pad applications.</p></header><LaunchPadSettingsForm settings={settings} /></div>;
}
