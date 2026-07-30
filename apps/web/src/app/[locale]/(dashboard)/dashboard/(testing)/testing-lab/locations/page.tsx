import { TestingLabConfirmAction } from '@/components/testing-lab/testing-lab-confirm-action';
import { CreateTestingLocationDialog, EditTestingLocationDialog } from '@/components/testing-lab/testing-lab-dialogs';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { deleteTestingLabLocation, restoreTestingLabLocation } from '@/lib/testing-lab/actions';
import { getTestingLabDashboard, normalizeTestingLocationStatus } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { Input } from '@game-guild/ui/components/input';
import { MapPin, Search } from 'lucide-react';

export default async function TestingLabLocationsPage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const params = await searchParams;
  const q = params.q?.trim().toLowerCase() ?? '';
  const directory = await getTestingLabDashboard();
  const locations = directory.locations.filter(
    (location) => !q || `${location.name} ${location.city ?? ''} ${location.country ?? ''}`.toLowerCase().includes(q),
  );

  return (
    <main className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={MapPin}
        title="Testing locations"
        description="Manage physical, hybrid, and remote capacity used to schedule moderated testing sessions."
        actions={<CreateTestingLocationDialog />}
      />
      <TestingLabAccessIssues issues={directory.accessIssues} />
      <form method="get" className="relative max-w-xl">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input name="q" defaultValue={params.q} className="pl-9" placeholder="Search locations" />
      </form>
      {locations.length === 0 ? (
        <TestingLabEmptyState
          title={directory.locations.length === 0 ? 'No testing locations' : 'No matching locations'}
          description="Create a physical room or remote lab before scheduling sessions."
          action={<CreateTestingLocationDialog />}
        />
      ) : (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {locations.map((location) => (
            <article key={location.id} className="rounded-md border p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="font-semibold">{location.name}</h2>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {location.isVirtual
                      ? (location.virtualUrl ?? 'Remote location')
                      : [location.city, location.country].filter(Boolean).join(', ') || 'Physical location'}
                  </p>
                </div>
                <Badge variant="outline">{normalizeTestingLocationStatus(location.status)}</Badge>
              </div>
              <p className="mt-4 text-sm text-muted-foreground">{location.description ?? 'No operating notes.'}</p>
              <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                <div>
                  <dt className="text-muted-foreground">Testers</dt>
                  <dd className="font-medium">{location.maxTestersCapacity ?? location.capacity ?? 0}</dd>
                </div>
                <div>
                  <dt className="text-muted-foreground">Projects</dt>
                  <dd className="font-medium">{location.maxProjectsCapacity ?? 0}</dd>
                </div>
              </dl>
              <div className="mt-4 flex items-center gap-2 border-t pt-3">
                <EditTestingLocationDialog location={location} />
                <TestingLabConfirmAction
                  action={location.isDeleted ? restoreTestingLabLocation : deleteTestingLabLocation}
                  fields={{ locationId: location.id }}
                  label={location.isDeleted ? 'Restore' : 'Archive'}
                  title={location.isDeleted ? 'Restore this location?' : 'Archive this location?'}
                  description={
                    location.isDeleted
                      ? 'The location becomes available to scheduling again.'
                      : 'The location leaves active scheduling and can be restored later.'
                  }
                  confirmLabel={location.isDeleted ? 'Restore location' : 'Archive location'}
                  intent={location.isDeleted ? 'restore' : 'archive'}
                />
              </div>
            </article>
          ))}
        </div>
      )}
    </main>
  );
}
