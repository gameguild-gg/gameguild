import { Badge } from "@game-guild/ui/components/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import {
  Award,
  CalendarDays,
  CheckCircle2,
  Clock3,
  GraduationCap,
} from "lucide-react";

import type { LearnerCertificate, LearnerCourseRecord } from "./types";

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function LearnerCalendar({
  records,
}: {
  records: LearnerCourseRecord[];
}) {
  const events = records
    .flatMap(({ course, context }) => [
      ...context.calendar.flatMap((entry) => {
        const date = entry.startsAt || entry.dueAt || entry.availableFrom;
        return date
          ? [
              {
                id: `cohort-${course.id}-${entry.itemId}`,
                date,
                title: entry.title || "Scheduled class",
                course: course.title,
                meta:
                  entry.cohortName || context.cohort?.name || "Course schedule",
                kind: entry.type || entry.itemType || "Class",
              },
            ]
          : [];
      }),
      ...context.assessments.flatMap((assessment) =>
        assessment.dueAt
          ? [
              {
                id: `assessment-${assessment.id}`,
                date: assessment.dueAt,
                title: assessment.title || "Assessment deadline",
                course: course.title,
                meta: assessment.assessmentGroupName || "Assessment",
                kind: "Deadline",
              },
            ]
          : [],
      ),
    ])
    .sort(
      (left, right) =>
        new Date(left.date).getTime() - new Date(right.date).getTime(),
    );

  return (
    <div className="space-y-6">
      <header>
        <p className="text-sm font-medium text-primary">Your schedule</p>
        <h1 className="mt-2 text-3xl font-semibold">Calendar</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Classes, content releases, and assessment deadlines across every
          active enrollment.
        </p>
      </header>
      {events.length === 0 ? (
        <Card className="rounded-lg bg-card">
          <CardContent className="flex min-h-56 flex-col items-center justify-center text-center">
            <CalendarDays className="size-8 text-muted-foreground" />
            <h2 className="mt-4 font-semibold">No scheduled events</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              Cohort sessions and deadlines will appear here.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {events.map((event) => (
            <Card key={event.id} className="rounded-lg bg-card">
              <CardContent className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center">
                <div className="flex size-11 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                  <CalendarDays className="size-5" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="font-semibold">{event.title}</h2>
                    <Badge variant="outline">{event.kind}</Badge>
                  </div>
                  <p className="mt-1 flex flex-wrap gap-x-2 text-sm text-muted-foreground">
                    <span>{event.course}</span>
                    <span aria-hidden="true">·</span>
                    <span>{event.meta}</span>
                  </p>
                </div>
                <time className="inline-flex items-center gap-2 text-sm text-muted-foreground">
                  <Clock3 className="size-4" />
                  {formatDate(event.date)}
                </time>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

export function LearnerGradebook({
  records,
}: {
  records: LearnerCourseRecord[];
}) {
  const assessments = records.flatMap(({ course, context }) =>
    context.assessments.map((assessment) => ({
      course,
      assessment,
      submission: context.submissions.find(
        (candidate) => candidate.assessmentId === assessment.id,
      ),
    })),
  );
  const graded = assessments.filter((row) => row.submission?.score != null);
  const earned = graded.reduce(
    (total, row) => total + (row.submission?.score ?? 0),
    0,
  );
  const possible = graded.reduce(
    (total, row) => total + (row.assessment.maxScore ?? 0),
    0,
  );

  return (
    <div className="space-y-6">
      <header>
        <p className="text-sm font-medium text-primary">Academic record</p>
        <h1 className="mt-2 text-3xl font-semibold">Grades and feedback</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Scores and instructor feedback come directly from submitted assessment
          records.
        </p>
      </header>
      <div className="grid gap-4 sm:grid-cols-3">
        <Card className="rounded-lg bg-card">
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Graded work
            </CardTitle>
          </CardHeader>
          <CardContent className="text-3xl font-semibold">
            {graded.length}
          </CardContent>
        </Card>
        <Card className="rounded-lg bg-card">
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Current score
            </CardTitle>
          </CardHeader>
          <CardContent className="text-3xl font-semibold">
            {possible > 0 ? Math.round((earned / possible) * 100) : 0}%
          </CardContent>
        </Card>
        <Card className="rounded-lg bg-card">
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Awaiting grades
            </CardTitle>
          </CardHeader>
          <CardContent className="text-3xl font-semibold">
            {
              assessments.filter(
                (row) => row.submission && row.submission.score == null,
              ).length
            }
          </CardContent>
        </Card>
      </div>
      {assessments.length === 0 ? (
        <Card className="rounded-lg bg-card">
          <CardContent className="flex min-h-48 items-center justify-center text-sm text-muted-foreground">
            No assessments are assigned yet.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {assessments.map(({ course, assessment, submission }) => (
            <Card
              key={`${course.id}-${assessment.id}`}
              className="rounded-lg bg-card"
            >
              <CardContent className="p-5">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="font-semibold">
                        {assessment.title || "Untitled assessment"}
                      </h2>
                      {assessment.assessmentGroupName ? (
                        <Badge variant="outline">
                          {assessment.assessmentGroupName}
                        </Badge>
                      ) : null}
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {course.title}
                    </p>
                  </div>
                  <strong className="text-lg">
                    {submission?.score != null
                      ? `${submission.score} / ${assessment.maxScore ?? 0}`
                      : submission
                        ? "Awaiting grade"
                        : "Not submitted"}
                  </strong>
                </div>
                {submission?.feedback ? (
                  <div className="mt-4 border-l-2 border-primary pl-4">
                    <p className="text-xs font-medium uppercase text-primary">
                      Instructor feedback
                    </p>
                    <p className="mt-2 text-sm leading-6">
                      {submission.feedback}
                    </p>
                  </div>
                ) : null}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

export function LearnerCertificates({
  certificates,
}: {
  certificates: LearnerCertificate[];
}) {
  return (
    <div className="space-y-6">
      <header>
        <p className="text-sm font-medium text-primary">
          Verified achievements
        </p>
        <h1 className="mt-2 text-3xl font-semibold">Certificates</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Credentials issued after confirmed course completion.
        </p>
      </header>
      {certificates.length === 0 ? (
        <Card className="rounded-lg bg-card">
          <CardContent className="flex min-h-64 flex-col items-center justify-center text-center">
            <Award className="size-9 text-muted-foreground" />
            <h2 className="mt-4 text-lg font-semibold">
              No certificates issued yet
            </h2>
            <p className="mt-2 max-w-md text-sm text-muted-foreground">
              Complete an eligible course and satisfy its assessment
              requirements to receive a credential.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {certificates.map((certificate) => (
            <Card key={certificate.id} className="rounded-lg bg-card">
              <CardContent className="p-6">
                <div className="flex items-start gap-4">
                  <div className="flex size-12 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                    <GraduationCap className="size-6" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="font-semibold">
                        {certificate.courseName || "Game Guild course"}
                      </h2>
                      <Badge>
                        <CheckCircle2 className="mr-1 size-3" />
                        {certificate.status || "Active"}
                      </Badge>
                    </div>
                    <p className="mt-3 font-mono text-sm">
                      {certificate.certificateNumber || certificate.id}
                    </p>
                    {certificate.issuedAt ? (
                      <p className="mt-2 text-xs text-muted-foreground">
                        Issued {formatDate(certificate.issuedAt)}
                      </p>
                    ) : null}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
