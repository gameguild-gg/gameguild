import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import {
  ArrowRight,
  BookOpen,
  CalendarDays,
  Clock3,
  MessageSquare,
  Users,
} from "lucide-react";
import Link from "next/link";

import {
  defaultLearnerRoutes,
  type LearnerCourse,
  type LearnerCourseContext,
  type LearnerRoutes,
} from "./types";

function formatDate(value?: string | null) {
  if (!value) return "Not scheduled";

  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(new Date(value));
}

export interface CourseLearnerOverviewProps {
  course: LearnerCourse;
  context: LearnerCourseContext;
  routes?: LearnerRoutes;
}

export function CourseLearnerOverview({
  course,
  context,
  routes = defaultLearnerRoutes,
}: CourseLearnerOverviewProps) {
  const nextEvent = [...context.calendar]
    .filter(
      (entry) =>
        entry.status !== "Cancelled" &&
        Boolean(entry.startsAt ?? entry.dueAt ?? entry.availableFrom),
    )
    .sort(
      (left, right) =>
        new Date(
          left.startsAt ?? left.dueAt ?? left.availableFrom ?? 0,
        ).getTime() -
        new Date(
          right.startsAt ?? right.dueAt ?? right.availableFrom ?? 0,
        ).getTime(),
    )[0];
  const nextAssessment = [...context.assessments]
    .filter((assessment) => Boolean(assessment.dueAt))
    .sort(
      (left, right) =>
        new Date(left.dueAt ?? 0).getTime() -
        new Date(right.dueAt ?? 0).getTime(),
    )[0];

  return (
    <div className="space-y-8">
      <header className="border-b pb-6">
        <div className="flex flex-wrap items-start justify-between gap-5">
          <div className="min-w-0">
            <p className="text-sm font-medium text-primary">Course overview</p>
            <h1 className="mt-2 text-3xl font-semibold sm:text-4xl">
              {course.title}
            </h1>
            <p className="mt-3 max-w-3xl text-sm leading-6 text-muted-foreground">
              {course.description}
            </p>
          </div>
          <Button asChild>
            <Link href={routes.content(course.slug)}>
              Continue learning
              <ArrowRight className="ml-2 size-4" />
            </Link>
          </Button>
        </div>
      </header>

      <section
        aria-label="Course progress"
        className="grid border-y sm:grid-cols-2 lg:grid-cols-4"
      >
        <div className="p-5 lg:border-r">
          <p className="text-sm text-muted-foreground">Progress</p>
          <p className="mt-2 text-2xl font-semibold">
            {course.overallProgress}%
          </p>
        </div>
        <div className="border-t p-5 sm:border-l sm:border-t-0 lg:border-l-0 lg:border-r">
          <p className="text-sm text-muted-foreground">Completed</p>
          <p className="mt-2 text-2xl font-semibold">
            {course.completedItems}/{course.totalItems}
          </p>
        </div>
        <div className="border-t p-5 lg:border-r lg:border-t-0">
          <p className="text-sm text-muted-foreground">Remaining</p>
          <p className="mt-2 text-2xl font-semibold">
            {Math.ceil(course.remainingMinutes / 60)}h
          </p>
        </div>
        <div className="border-t p-5 sm:border-l lg:border-l-0 lg:border-t-0">
          <p className="text-sm text-muted-foreground">Certificate</p>
          <p className="mt-2 text-base font-semibold">
            {context.certificates.length ? "Issued" : "In progress"}
          </p>
        </div>
      </section>

      <section className="grid gap-6 lg:grid-cols-[minmax(0,1.5fr)_minmax(18rem,0.8fr)]">
        <div className="space-y-5">
          <div className="flex items-center gap-3">
            <Users className="size-5 text-primary" />
            <div>
              <p className="text-sm text-muted-foreground">Your cohort</p>
              <p className="font-medium">
                {context.cohort?.name ?? "Self-paced course"}
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <CalendarDays className="mt-0.5 size-5 text-primary" />
            <div>
              <p className="text-sm text-muted-foreground">
                Next class or release
              </p>
              <p className="font-medium">
                {nextEvent?.title ?? "No event scheduled"}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                {formatDate(nextEvent?.startsAt ?? nextEvent?.availableFrom)}
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <Clock3 className="mt-0.5 size-5 text-primary" />
            <div>
              <p className="text-sm text-muted-foreground">Next deadline</p>
              <p className="font-medium">
                {nextAssessment?.title ?? "No upcoming deadline"}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                {formatDate(nextAssessment?.dueAt)}
              </p>
            </div>
          </div>
        </div>
        <nav aria-label="Course actions" className="divide-y border-y">
          <Link
            href={routes.content(course.slug)}
            className="flex items-center justify-between py-4 text-sm transition-colors hover:text-primary"
          >
            <span className="flex items-center gap-3">
              <BookOpen className="size-4" />
              Course content
            </span>
            <ArrowRight className="size-4" />
          </Link>
          <Link
            href={routes.activities(course.slug)}
            className="flex items-center justify-between py-4 text-sm transition-colors hover:text-primary"
          >
            <span>View activities</span>
            <ArrowRight className="size-4" />
          </Link>
          <Link
            href={routes.community(course.slug)}
            className="flex items-center justify-between py-4 text-sm transition-colors hover:text-primary"
          >
            <span className="flex items-center gap-3">
              <MessageSquare className="size-4" />
              Open community
            </span>
            <ArrowRight className="size-4" />
          </Link>
        </nav>
      </section>

      {course.currentItem ? (
        <section className="border-l-2 border-primary bg-muted/30 p-5">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <Badge variant="outline">Up next</Badge>
              <h2 className="mt-3 text-xl font-semibold">
                {course.currentItem.title}
              </h2>
              <p className="mt-1 text-sm text-muted-foreground">
                {course.currentItem.duration
                  ? `${course.currentItem.duration} minutes`
                  : "Ready when you are"}
              </p>
            </div>
            <Button asChild variant="outline">
              <Link href={routes.content(course.slug)}>Open lesson</Link>
            </Button>
          </div>
        </section>
      ) : null}
    </div>
  );
}
