import { TestingLabCalendar } from "@/components/testing-lab/testing-lab-calendar";
import { Link } from "@/i18n/navigation";
import {
  getTestingLabAnalytics,
  getTestingLabSettings,
} from "@/lib/testing-lab";
import {
  getTestingEventTemplates,
  getTestingEventsDirectory,
} from "@/lib/testing-lab/events-queries";
import { buttonVariants } from "@game-guild/ui/components/button-variants";
import { CalendarDays, ExternalLink } from "lucide-react";

export default async function WorkspaceCalendarPage(): Promise<React.JSX.Element> {
  const [analytics, events, labSettings, templates] = await Promise.all([
    getTestingLabAnalytics(),
    getTestingEventsDirectory({ take: 100 }),
    getTestingLabSettings(),
    getTestingEventTemplates(),
  ]);

  return (
    <div className="-m-4 flex h-[calc(100dvh-4rem)] min-h-[38rem] flex-col overflow-hidden sm:-m-6">
      <div className="min-h-0 flex-1">
        <TestingLabCalendar
          events={events.events}
          eventAnalytics={analytics.events}
          templates={templates.templates}
          defaultTimeZone={labSettings.settings?.timezone ?? "UTC"}
          toolbarStart={
            <div className="flex items-center gap-2">
              <div className="flex size-8 items-center justify-center rounded-md bg-muted/60">
                <CalendarDays className="size-4" aria-hidden="true" />
              </div>
              <h1 className="text-lg font-semibold">Calendar</h1>
            </div>
          }
          toolbarEnd={
            <>
              <Link
                href="/testing-lab"
                aria-label="Open public Testing Lab"
                title="Open public Testing Lab"
                className={buttonVariants({ variant: "outline", size: "icon" })}
              >
                <ExternalLink aria-hidden="true" />
              </Link>
            </>
          }
        />
      </div>
    </div>
  );
}
