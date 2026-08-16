import React from "react";
import { notFound } from "next/navigation";
import { getAssessment } from "@/lib/learning";
import { getCodingAssignmentFull } from "@/lib/coding-assignment/client";
import { CodingDefinitionEditor } from "./coding-definition-editor";

/**
 * Instructor-only coding-definition authoring route.
 *
 * Translates the Next.js route param `assessmentId` into the linked
 * ProgramContent's `(programId, contentId)` server-side, then calls the
 * Task 4 wrapper `getCodingAssignmentFull(programId, contentId)` — the v1
 * endpoint lives on `ProgramContentController`, not `AssessmentsController`.
 *
 * The lookup goes through the existing `getAssessment` fetcher (which returns
 * `courseId` + `contentId`); `courseId` IS `programId` in this stack.
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

  const assessment = await getAssessment(assessmentId);
  if (!assessment) {
    notFound();
  }
  // The linked ProgramContent is the v1 storage home. The Task 3 PUT is
  // upsert (Metis #26), so the editor handles "no content yet" client-side
  // by seeding from the assignment's sample workspace.
  // `assessment.courseId` IS the Program GUID (Assessment.CourseId =
  // Program.Id) — the URL `course` param is only a slug, the v1 API
  // validates programId as a GUID (same pattern as grade/page.tsx).
  const programId = assessment.courseId;
  const contentId = assessment.contentId;
  const initialContent = contentId
    ? await getCodingAssignmentFull(programId, contentId)
    : null;

  return (
    <CodingDefinitionEditor
      courseId={courseId}
      assessmentId={assessmentId}
      programId={programId}
      contentId={contentId}
      assessmentTitle={assessment.title}
      initialContent={initialContent}
    />
  );
}
