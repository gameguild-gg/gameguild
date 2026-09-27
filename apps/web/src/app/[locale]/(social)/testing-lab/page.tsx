import { TestingEventsBrowser } from "@/components/testing-lab/landing/testing-sessions";
import { presentTestingEvents } from "@/components/testing-lab/landing/testing-events-presentation";
import { getPublicTestingEventsDirectory } from "@/lib/testing-lab/events-queries";
import { getLocalizationPreference } from "@/lib/user-settings/queries";

function safeTimeZone(timeZone?: string | null) {
  if (!timeZone) return "UTC";
  try {
    new Intl.DateTimeFormat("en-US", { timeZone });
    return timeZone;
  } catch {
    return "UTC";
  }
}

export default async function Page({
  searchParams,
}: {
  searchParams?: Promise<{ projectId?: string }>;
}) {
  const { projectId } = searchParams ? await searchParams : {};
  const [directory, localization] = await Promise.all([
    getPublicTestingEventsDirectory({ take: 100 }),
    getLocalizationPreference().catch(() => null),
  ]);
  const timeZoneId = safeTimeZone(localization?.timezone);
  const dateLocale = localization?.language ?? "en-US";

  return (
    <TestingEventsBrowser
      events={presentTestingEvents(directory.events, new Date(), {
        timeZoneId,
        dateLocale,
        hour12: localization?.timeFormat !== "24h",
      })}
      accessIssues={directory.accessIssues}
      projectId={projectId}
    />
  );
}
