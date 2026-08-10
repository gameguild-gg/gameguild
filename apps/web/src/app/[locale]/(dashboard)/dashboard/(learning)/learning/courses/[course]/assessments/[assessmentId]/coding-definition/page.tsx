import React from "react";
import { notFound } from "next/navigation";
import { getAssessment } from "@/lib/learning";
import { getCodingDefinitionFull } from "@/lib/emception/get-coding-definition-full";
import { CodingDefinitionEditor } from "./coding-definition-editor";

/**
 * Instructor-only coding-definition authoring route.
 *
 * The fetcher uses the `/v1.0/assessments/{id}/coding-definition/full`
 * endpoint which is gated by `CanReviewCourseAsync` server-side — students
 * and unenrolled users receive 403, which the fetcher surfaces as `null`.
 */
export default async function CodingDefinitionPage({
  params,
}: {
  params: Promise<{
    locale: string;
    course: string;
    assessmentId: string;
  }>;
}): Promise<React.JSX.Element> {
  const { course: courseId, assessmentId } = await params;

  const [assessment, initialDefinition] = await Promise.all([
    getAssessment(assessmentId),
    getCodingDefinitionFull(assessmentId),
  ]);

  if (!assessment) {
    notFound();
  }

  return (
    <CodingDefinitionEditor
      courseId={courseId}
      assessmentId={assessmentId}
      assessmentTitle={assessment.title}
      initialDefinition={initialDefinition}
    />
  );
}
