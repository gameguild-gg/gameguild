import React from "react";
import { notFound } from "next/navigation";
import { getToken } from "@/auth";
import {
  createServerClient,
  GeneratedApi,
  type LearningAssessmentsGradingQueue,
} from "@game-guild/client";
import { Link } from "@/i18n/navigation";
import { SpeedgraderShell } from "./speedgrader-shell";

/**
 * SpeedGrader page — server-fetches the grading queue bundle and hands it to
 * the client shell as props (no client-side fetching).
 *
 * Route: `/[locale]/speedgrader/assessments/[assessmentId]?course=<slug>&nav=<index>`
 *
 * The route has no course segment, so the back link's `courseSlug` arrives via
 * the `?course=` searchParam.
 */
export default async function SpeedgraderAssessmentPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string; assessmentId: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}): Promise<React.JSX.Element> {
  const { assessmentId } = await params;
  const query = await searchParams;

  const courseParam = typeof query.course === "string" ? query.course : undefined;
  if (!courseParam) {
    notFound();
  }

  const queue = await fetchGradingQueue(assessmentId);
  if (!queue.ok) {
    if (queue.status === 404) {
      notFound();
    }
    return (
      <main className="grid flex-1 place-items-center p-6">
        <div data-testid="speedgrader-error" className="max-w-md text-center">
          <h1 className="text-lg font-semibold text-foreground">
            Grading queue unavailable
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">{queue.message}</p>
          <Link
            href={`/dashboard/learning/courses/${courseParam}/assessments/${assessmentId}/submissions`}
            className="mt-4 inline-block text-sm text-primary underline-offset-4 hover:underline"
          >
            Back to submissions
          </Link>
        </div>
      </main>
    );
  }

  const data = queue.data;
  const navParam =
    typeof query.nav === "string" ? Number.parseInt(query.nav, 10) : Number.NaN;
  const initialIndex = Number.isFinite(navParam) ? navParam : 0;

  return (
    <SpeedgraderShell
      assessmentTitle={data.assessment?.title ?? "Assessment"}
      assessmentId={assessmentId}
      courseSlug={courseParam}
      items={data.items ?? []}
      needsGrading={data.needsGrading ?? 0}
      initialIndex={initialIndex}
    />
  );
}

type QueueResult =
  | { ok: true; data: LearningAssessmentsGradingQueue }
  | { ok: false; status?: number; message: string };

/** Fetch the grading-queue bundle (instructor-only endpoint). */
async function fetchGradingQueue(assessmentId: string): Promise<QueueResult> {
  const apiUrl =
    process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  const assessments = new GeneratedApi.LearningAssessmentsModule(client);
  try {
    const result = await assessments.getAssessmentsGradingQueue(assessmentId);
    if (result.ok) {
      return { ok: true, data: result.data };
    }
    return {
      ok: false,
      status: result.error?.status,
      message:
        result.error?.status === 403
          ? "You do not have permission to grade this assessment."
          : "The grading queue could not be loaded. Try again in a moment.",
    };
  } catch (err) {
    console.error("Error fetching grading queue:", err);
    return {
      ok: false,
      message: "The grading queue could not be loaded. Try again in a moment.",
    };
  }
}
