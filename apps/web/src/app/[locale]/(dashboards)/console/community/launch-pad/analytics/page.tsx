import { getLaunchPadAnalytics } from '@/lib/launch-pad/queries';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';

export default async function LaunchPadAnalyticsPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ViewAnalytics')) forbidden();
  const analytics = await getLaunchPadAnalytics();
  const metrics = analytics ? [['Events', analytics.events], ['Completed events', analytics.completedEvents], ['Applications', analytics.applications], ['Approved applications', analytics.approvedApplications], ['Registrations', analytics.registrations], ['Completed registrations', analytics.completedRegistrations]] : [];
  return <div className="space-y-6 p-6"><header><h1 className="text-3xl font-bold">Launch Pad analytics</h1><p className="text-muted-foreground">Tenant-scoped event funnel.</p></header><div className="grid gap-4 md:grid-cols-3">{metrics.map(([label, value]) => <Card key={String(label)}><CardHeader><CardTitle className="text-sm text-muted-foreground">{label}</CardTitle></CardHeader><CardContent className="text-3xl font-semibold">{value}</CardContent></Card>)}</div></div>;
}
