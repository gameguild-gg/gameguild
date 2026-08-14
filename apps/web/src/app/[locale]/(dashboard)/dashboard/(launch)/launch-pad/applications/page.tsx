import { getManagedLaunchPadApplications, getManagedLaunchPadEvents } from '@/lib/launch-pad/queries';
import { reviewLaunchPadApplicationForm } from '@/lib/launch-pad/actions';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';

export default async function LaunchPadApplicationsPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ReviewApplications')) forbidden();
  const events = await getManagedLaunchPadEvents();
  const applications = await getManagedLaunchPadApplications(events.map((event) => event.id));
  const names = new Map(events.map((event) => [event.id, event.name]));
  return <div className="space-y-6 p-6"><header><h1 className="text-3xl font-bold">Launch Pad applications</h1><p className="text-muted-foreground">Review Project-owned applications. Approval creates the event Launch Plan.</p></header><div className="space-y-3">{applications.length === 0 ? <Card><CardHeader><CardTitle>No application</CardTitle></CardHeader></Card> : applications.map((application) => <Card key={application.id}><CardContent className="space-y-4 p-5"><div className="flex flex-wrap items-center justify-between gap-4"><div><p className="font-medium">Project {application.projectId}</p><p className="text-sm text-muted-foreground">{names.get(application.eventId) ?? application.eventId} · version {application.projectVersionId}</p></div><Badge variant="outline">{application.status}</Badge></div>{['Submitted','UnderReview','Waitlisted'].includes(String(application.status)) && <div className="flex flex-wrap gap-2">{['UnderReview','Waitlisted','Approved','Rejected'].map((status) => <form key={status} action={reviewLaunchPadApplicationForm}><input type="hidden" name="applicationId" value={application.id} /><input type="hidden" name="eventId" value={application.eventId} /><input type="hidden" name="status" value={status} /><Button type="submit" size="sm" variant={status === 'Approved' ? 'default' : status === 'Rejected' ? 'destructive' : 'outline'}>{status}</Button></form>)}</div>}</CardContent></Card>)}</div></div>;
}
