import { TestingEventsBrowser } from "@/components/testing-lab/landing/testing-sessions";
import { presentTestingEvents } from "@/components/testing-lab/landing/testing-events-presentation";
import { getPublicTestingEventsDirectory } from "@/lib/testing-lab/events-queries";

export default async function TestingLabEventsPage({
  searchParams,
}: {
  searchParams?: Promise<{ projectId?: string }>;
}) {
  const { projectId } = searchParams ? await searchParams : {};
  const directory = await getPublicTestingEventsDirectory({ take: 100 });

  return (
    <TestingEventsBrowser
      events={presentTestingEvents(directory.events)}
      accessIssues={directory.accessIssues}
      projectId={projectId}
    />
  );
}
