import { FloatingIcons } from "@/components/testing-lab/common/ui/floating-icons";
import { TestingLabHero } from "@/components/testing-lab/landing/testing-lab-hero";
import { TestingLabHowItWorks } from "@/components/testing-lab/landing/testing-lab-how-it-works";
import { TestingLabLearnMore } from "@/components/testing-lab/landing/testing-lab-learn-more";
import { TestingLabStats } from "@/components/testing-lab/landing/testing-lab-stats";
import { getPublicTestingEventsDirectory } from "@/lib/testing-lab/events-queries";

export default async function TestingLabPage() {
  const directory = await getPublicTestingEventsDirectory({ take: 100 });
  const openEvents = directory.events.filter((event) =>
    ["ApplicationsOpen", "Scheduled", "Active"].includes(
      String(event.status ?? ""),
    ),
  ).length;
  const upcomingEvents = directory.events.filter(
    (event) =>
      Boolean(event.startsAt) &&
      ["ApplicationsOpen", "Scheduled"].includes(String(event.status ?? "")),
  ).length;
  const openTesterSeats = directory.events.reduce(
    (eventTotal, event) =>
      eventTotal +
      (event.slots ?? []).reduce(
        (slotTotal, slot) => slotTotal + (slot.availableTesterCount ?? 0),
        0,
      ),
    0,
  );

  return (
    <div className="relative flex min-h-screen flex-1 flex-col overflow-hidden bg-gradient-to-b from-slate-950 via-slate-900 to-slate-950 text-white">
      <FloatingIcons />
      <div className="relative z-10 flex flex-1 flex-col items-center justify-center">
        <main className="mx-auto flex w-full max-w-7xl flex-1 flex-col px-4 sm:px-6 lg:px-8">
          <TestingLabHero />
          <TestingLabStats
            totalEvents={directory.events.length}
            openEvents={openEvents}
            upcomingEvents={upcomingEvents}
            openTesterSeats={openTesterSeats}
          />
          <TestingLabLearnMore />
        </main>
        <aside id="learn-more" className="w-full border-t border-slate-800">
          <TestingLabHowItWorks />
        </aside>
      </div>
    </div>
  );
}
