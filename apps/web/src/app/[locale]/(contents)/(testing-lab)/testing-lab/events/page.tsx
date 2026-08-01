import { TestingEventsBrowser } from "@/components/testing-lab/landing/testing-sessions";
import { presentTestingEvents } from "@/components/testing-lab/landing/testing-events-presentation";
import { getPublicTestingEventsDirectory } from "@/lib/testing-lab/events-queries";

export default async function TestingLabEventsPage() {
  const directory = await getPublicTestingEventsDirectory({ take: 100 });

  return (
    <TestingEventsBrowser
      events={presentTestingEvents(directory.events)}
      accessIssues={directory.accessIssues}
    />
  );
}
