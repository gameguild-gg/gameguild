import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Card, CardContent } from "@game-guild/ui/components/card";
import {
  ArrowRight,
  CalendarClock,
  CheckCircle2,
  ClipboardList,
  MessageSquareText,
} from "lucide-react";
import Link from "next/link";

import {
  defaultLearnerRoutes,
  type LearnerContentItem,
  type LearnerCourse,
  type LearnerCourseContext,
  type LearnerRoutes,
} from "./types";

function formatDate(value?: string | null) {
  if (!value) return null;
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function contentKind(item: LearnerContentItem) {
  switch (item.contentType) {
    case "Discussion":
      return "Discussion";
    case "Reflection":
      return "Reflection";
    case "Survey":
      return "Survey";
    default:
      return "Activity";
  }
}

export interface LearnerActivitiesProps {
  course: LearnerCourse;
  context: LearnerCourseContext;
  routes?: LearnerRoutes;
}

export function LearnerActivities({
  course,
  context,
  routes = defaultLearnerRoutes,
}: LearnerActivitiesProps) {
  const contentActivities = course.modules
    .flatMap((module) => module.items)
    .filter(
      (item) =>
        item.contentType === "Discussion" ||
        item.contentType === "Reflection" ||
        item.contentType === "Survey",
    );

  return (
    <div className="space-y-8">
      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-primary">{course.title}</p>
          <h1 className="mt-2 text-3xl font-semibold">
            Assignments and activities
          </h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Complete graded work and course participation in one place.
            Attempts, files, responses, grades, and feedback are stored in your
            enrollment record.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href={routes.content(course.slug)}>Course content</Link>
        </Button>
      </header>

      {context.assessments.length === 0 && contentActivities.length === 0 ? (
        <Card className="rounded-lg bg-card">
          <CardContent className="flex min-h-56 flex-col items-center justify-center text-center">
            <ClipboardList className="size-8 text-muted-foreground" />
            <h2 className="mt-4 text-lg font-semibold">
              No activities assigned
            </h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Your instructor has not published graded or participatory work
              yet.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {context.assessments.map((assessment) => {
            const submission = context.submissions.find(
              (candidate) => candidate.assessmentId === assessment.id,
            );
            const status =
              submission?.status ??
              (assessment.isAvailable === false ? "Not available" : "Ready");
            const title = assessment.title || "Untitled assessment";
            const assessmentId = assessment.id ?? "";

            return (
              <Card
                key={assessmentId}
                className="rounded-lg bg-card transition-colors hover:border-foreground/20"
              >
                <CardContent className="flex flex-col gap-4 p-5 md:flex-row md:items-center">
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <ClipboardList className="size-5" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="font-semibold">{title}</h2>
                      <Badge variant="outline">
                        {assessment.type === "Exam"
                          ? "Quiz"
                          : assessment.type || "Assessment"}
                      </Badge>
                      <Badge
                        variant={
                          submission?.status === "Graded"
                            ? "default"
                            : "secondary"
                        }
                      >
                        {status}
                      </Badge>
                    </div>
                    <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                      {assessment.description ||
                        "Review the instructions and submit using the configured response method."}
                    </p>
                    <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-xs text-muted-foreground">
                      {assessment.dueAt ? (
                        <span className="inline-flex items-center gap-1.5">
                          <CalendarClock className="size-3.5" />
                          Due {formatDate(assessment.dueAt)}
                        </span>
                      ) : (
                        <span>No deadline</span>
                      )}
                      <span>{assessment.maxScore ?? 0} points</span>
                      {submission?.score != null ? (
                        <strong className="font-medium text-foreground">
                          {submission.score} / {assessment.maxScore ?? 0}
                        </strong>
                      ) : null}
                    </div>
                  </div>
                  {assessmentId && assessment.isAvailable !== false ? (
                    <Button asChild>
                      <Link
                        href={routes.activity(
                          course.slug,
                          `assessment-${assessmentId}`,
                        )}
                      >
                        {submission?.status === "Graded"
                          ? "Review grade"
                          : submission
                            ? "View submission"
                            : "Start"}
                        <ArrowRight className="size-4" />
                      </Link>
                    </Button>
                  ) : (
                    <Button disabled>Unavailable</Button>
                  )}
                </CardContent>
              </Card>
            );
          })}

          {contentActivities.map((item) => (
            <Card
              key={item.id}
              className="rounded-lg bg-card transition-colors hover:border-foreground/20"
            >
              <CardContent className="flex flex-col gap-4 p-5 md:flex-row md:items-center">
                <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                  <MessageSquareText className="size-5" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="font-semibold">{item.title}</h2>
                    <Badge variant="outline">{contentKind(item)}</Badge>
                    {item.status === "completed" ? (
                      <Badge>
                        <CheckCircle2 className="mr-1 size-3" />
                        Completed
                      </Badge>
                    ) : (
                      <Badge variant="secondary">
                        {item.status === "locked" ? "Locked" : "Ready"}
                      </Badge>
                    )}
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {item.description ||
                      "Participate and preserve your response in the course record."}
                  </p>
                </div>
                {item.status === "locked" ? (
                  <Button variant="outline" disabled>
                    Locked
                  </Button>
                ) : (
                  <Button asChild variant="outline">
                    <Link
                      href={routes.activity(course.slug, `content-${item.id}`)}
                    >
                      {item.status === "completed" ? "Review" : "Open"}
                      <ArrowRight className="size-4" />
                    </Link>
                  </Button>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
