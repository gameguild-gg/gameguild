import { getManagedLaunchPadEvents, getManagedLaunchPadRegistrations } from '@/lib/launch-pad/queries';
import { transitionLaunchPadRegistrationForm } from '@/lib/launch-pad/actions';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';

export default async function LaunchPadParticipantsPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ManageParticipants')) forbidden();
  const events = await getManagedLaunchPadEvents();
  const registrations = await getManagedLaunchPadRegistrations(events.map((event) => event.id));
  const next: Record<string, string[]> = { Waitlisted: ['Registered','Cancelled'], Registered: ['CheckedIn','NoShow','Cancelled'], CheckedIn: ['Attended','NoShow'], Attended: ['Completed'] };
  return <div className="space-y-6 p-6"><header><h1 className="text-3xl font-bold">Launch Pad participants</h1><p className="text-muted-foreground">Individual registration, waitlist, attendance, and completion.</p></header><div className="space-y-3">{registrations.length === 0 ? <Card><CardHeader><CardTitle>No participant</CardTitle></CardHeader></Card> : registrations.map((registration) => <Card key={registration.id}><CardContent className="space-y-4 p-5"><div className="flex items-center justify-between gap-4"><div><p className="font-medium">User {registration.userId}</p><p className="text-sm text-muted-foreground">Slot {registration.slotId}</p></div><Badge variant="outline">{registration.status}</Badge></div><div className="flex flex-wrap gap-2">{(next[String(registration.status)] ?? []).map((status) => <form key={status} action={transitionLaunchPadRegistrationForm}><input type="hidden" name="registrationId" value={registration.id} /><input type="hidden" name="status" value={status} /><Button type="submit" size="sm" variant={status === 'Cancelled' || status === 'NoShow' ? 'destructive' : 'outline'}>{status}</Button></form>)}</div></CardContent></Card>)}</div></div>;
}
