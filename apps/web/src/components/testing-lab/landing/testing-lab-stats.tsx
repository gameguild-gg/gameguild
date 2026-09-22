import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Calendar, ClipboardCheck, TestTube, Users } from "lucide-react";

interface TestingLabStatsProps {
  totalEvents: number;
  openEvents: number;
  upcomingEvents: number;
  openTesterSeats: number;
}

export function TestingLabStats({
  totalEvents,
  openEvents,
  upcomingEvents,
  openTesterSeats,
}: TestingLabStatsProps) {
  const stats = [
    {
      label: "Total Events",
      value: totalEvents,
      hint: "Public in the directory",
      icon: TestTube,
    },
    {
      label: "Open Now",
      value: openEvents,
      hint: "Ready to join",
      icon: Users,
    },
    {
      label: "Upcoming",
      value: upcomingEvents,
      hint: "With a published schedule",
      icon: Calendar,
    },
    {
      label: "Tester Seats",
      value: openTesterSeats,
      hint: "Open across schedules",
      icon: ClipboardCheck,
    },
  ] as const;

  return (
    <section className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
      {stats.map(({ label, value, hint, icon: Icon }) => (
        <Card key={label} className="gap-2 py-4">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-0">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {label}
            </CardTitle>
            <Icon className="size-6 text-muted-foreground" />
          </CardHeader>
          <CardContent className="pt-0">
            <div className="text-4xl font-bold">{value}</div>
            <p className="text-xs text-muted-foreground">{hint}</p>
          </CardContent>
        </Card>
      ))}
    </section>
  );
}
