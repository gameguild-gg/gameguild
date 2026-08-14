import { Card, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';

export default async function LaunchPadSettingsPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ManageSettings')) forbidden();
  return <div className="space-y-6 p-6"><header><h1 className="text-3xl font-bold">Launch Pad settings</h1><p className="text-muted-foreground">Administrative defaults and capabilities for Launch Pad events.</p></header><Card><CardHeader><CardTitle>Capability-driven access</CardTitle><CardDescription>Only LaunchPad.ManageSettings, TenantAdmin, or SystemAdmin may open this surface.</CardDescription></CardHeader></Card></div>;
}
