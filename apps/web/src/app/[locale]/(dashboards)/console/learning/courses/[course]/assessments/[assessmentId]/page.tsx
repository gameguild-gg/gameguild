import React from "react";
import { notFound } from "next/navigation";
import {
  getAssessment,
  getCourseAssessmentGroups,
  getCourseContent,
} from "@/lib/learning";
import { AssessmentEditor } from "@/components/learning/console/courses/[course]/assessments/[assessmentId]/assessment-editor";

/**
 * Assessment Detail/Editor Page
 *
 * Route: /courses/[course]/assessments/[assessmentId]
 */
export default async function AssessmentDetailPage({
  params,
}: PageProps<"/[locale]/console/learning/courses/[course]/assessments/[assessmentId]">): Promise<React.JSX.Element> {
  const { course: courseId, assessmentId } = await params;

  const [assessment, assessmentGroups, courseContent] = await Promise.all([
    getAssessment(assessmentId),
    getCourseAssessmentGroups(courseId),
    getCourseContent(courseId),
  ]);

  if (!assessment) {
    notFound();
  }

  return (
    <AssessmentEditor
      courseId={courseId}
      assessment={assessment}
      assessmentGroups={assessmentGroups}
      courseContent={courseContent.items}
    />
  );
}
