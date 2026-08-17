import { createLaunchPadEventForm, createLaunchPadSlotForm, transitionLaunchPadEventForm } from '@/lib/launch-pad/actions';
import { getManagedLaunchPadEvent, getManagedLaunchPadEvents, type LaunchPadEventDetail, type LaunchPadEventStatus } from '@/lib/launch-pad/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { DateTimePicker } from '@/components/ui/date-time-picker';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CalendarDays, Rocket } from 'lucide-react';
import { forbidden } from 'next/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';

const nextStatus: Partial<Record<string, LaunchPadEventStatus>> = {
  Draft: 'ApplicationsOpen',
  ApplicationsOpen: 'ApplicationsClosed',
  ApplicationsClosed: 'Scheduled',
  Scheduled: 'Active',
  Active: 'Completed',
};

function EventCard({ detail }: { detail: LaunchPadEventDetail }) {
  const event = detail.event;
  const next = nextStatus[String(event.status)];
  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div><CardTitle>{event.name}</CardTitle><CardDescription>{event.description || 'No description'}</CardDescription></div>
          <Badge variant="outline">{event.status}</Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground"><CalendarDays className="size-4" />
          {new Date(event.startsAt).toLocaleString()} – {new Date(event.endsAt).toLocaleString()}
        </div>
        <div className="flex flex-wrap gap-2">
          {next ? (
            <form action={transitionLaunchPadEventForm}>
              <input type="hidden" name="eventId" value={event.id} />
              <input type="hidden" name="status" value={String(next)} />
              <Button type="submit" size="sm">Move to {next}</Button>
            </form>
          ) : null}
          {!['Completed', 'Cancelled', 'Archived'].includes(String(event.status)) ? (
            <form action={transitionLaunchPadEventForm}>
              <input type="hidden" name="eventId" value={event.id} />
              <input type="hidden" name="status" value="Cancelled" />
              <Button type="submit" size="sm" variant="destructive">Cancel</Button>
            </form>
          ) : null}
          {['Completed', 'Cancelled'].includes(String(event.status)) ? (
            <form action={transitionLaunchPadEventForm}>
              <input type="hidden" name="eventId" value={event.id} />
              <input type="hidden" name="status" value="Archived" />
              <Button type="submit" size="sm" variant="outline">Archive</Button>
            </form>
          ) : null}
        </div>
        <div className="space-y-3 border-t pt-4">
          <h3 className="text-sm font-medium">Participant slots</h3>
          {detail.slots.map((slot) => <div key={slot.id} className="flex items-center justify-between rounded-md border p-3 text-sm"><span>{slot.name} · {String(slot.role)}</span><Badge variant="secondary">{slot.reservedCount}/{slot.capacity}</Badge></div>)}
          {!['Active', 'Completed', 'Cancelled', 'Archived'].includes(String(event.status)) && <form action={createLaunchPadSlotForm} className="grid gap-3 rounded-md border p-3 md:grid-cols-2">
            <input type="hidden" name="eventId" value={event.id} />
            <div><Label htmlFor={`slot-name-${event.id}`}>Slot name</Label><Input id={`slot-name-${event.id}`} name="name" required /></div>
            <div><Label htmlFor={`slot-role-${event.id}`}>Role</Label><select id={`slot-role-${event.id}`} name="role" className="h-10 w-full rounded-md border bg-background px-3"><option>Participant</option><option>Presenter</option><option>Mentor</option><option>Audience</option></select></div>
            <div><Label htmlFor={`slot-capacity-${event.id}`}>Capacity</Label><Input id={`slot-capacity-${event.id}`} name="capacity" type="number" min="1" required /></div>
            <div />
            <div><Label htmlFor={`slot-start-${event.id}`}>Starts</Label><DateTimePicker id={`slot-start-${event.id}`} name="startsAt" required /></div>
            <div><Label htmlFor={`slot-end-${event.id}`}>Ends</Label><DateTimePicker id={`slot-end-${event.id}`} name="endsAt" required /></div>
            <Button type="submit" size="sm" className="md:col-span-2">Add slot</Button>
          </form>}
        </div>
      </CardContent>
    </Card>
  );
}

export default async function LaunchPadManagementPage() {
  const contexts = await getDashboardContexts();
  if (!hasAnyDashboardCapability(contexts.capabilities, 'LaunchPad.ManageEvents')) forbidden();
  const events = await getManagedLaunchPadEvents();
  const details = (await Promise.all(events.map((event) => getManagedLaunchPadEvent(event.id)))).filter((detail): detail is LaunchPadEventDetail => detail !== null);
  return (
    <div className="space-y-6 p-6">
      <header>
        <h1 className="flex items-center gap-3 text-3xl font-bold"><Rocket className="size-7" /> Launch Pad management</h1>
        <p className="mt-2 text-muted-foreground">Administrative event lifecycle, applications, participants, and approved Launch Plans.</p>
      </header>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <section className="space-y-4">
          {details.length === 0 ? <Card><CardHeader><CardTitle>No managed events</CardTitle><CardDescription>Create the first Launch Pad event or request management access.</CardDescription></CardHeader></Card> : details.map((detail) => <EventCard key={detail.event.id} detail={detail} />)}
        </section>

        <Card className="h-fit">
          <CardHeader><CardTitle>Create Launch Pad event</CardTitle><CardDescription>This is an administrative action. Community members participate outside the dashboard.</CardDescription></CardHeader>
          <CardContent>
            <form action={createLaunchPadEventForm} className="space-y-4">
              <div className="space-y-2"><Label htmlFor="launch-event-name">Name</Label><Input id="launch-event-name" name="name" required /></div>
              <div className="space-y-2"><Label htmlFor="launch-event-description">Description</Label><Textarea id="launch-event-description" name="description" /></div>
              <div className="space-y-2"><Label htmlFor="launch-applications-open">Applications open</Label><DateTimePicker id="launch-applications-open" name="applicationsOpenAt" required /></div>
              <div className="space-y-2"><Label htmlFor="launch-applications-close">Applications close</Label><DateTimePicker id="launch-applications-close" name="applicationsCloseAt" required /></div>
              <div className="space-y-2"><Label htmlFor="launch-event-start">Event starts</Label><DateTimePicker id="launch-event-start" name="startsAt" required /></div>
              <div className="space-y-2"><Label htmlFor="launch-event-end">Event ends</Label><DateTimePicker id="launch-event-end" name="endsAt" required /></div>
              <Button className="w-full" type="submit">Create draft event</Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
