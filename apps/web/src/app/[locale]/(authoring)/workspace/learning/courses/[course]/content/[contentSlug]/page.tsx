import { LessonAuthoringWorkspace } from "@/components/learning/authoring/lesson-authoring-workspace";
import { getCodingAssignmentFull } from "@/lib/coding-assignment/client";
import { getAuthoringDraft } from "@/lib/learning/authoring";
import {
  getContentItem,
  getCourse,
  getCourseAssessments,
  getCourseContent,
} from "@/lib/learning";
import { notFound } from "next/navigation";

export default async function ContentItemAuthoringPage({
  params,
}: PageProps<"/[locale]/workspace/learning/courses/[course]/content/[contentSlug]">) {
  const { course: courseIdentifier, contentSlug } = await params;
  const course = await getCourse(courseIdentifier);
  if (!course) notFound();

  const [item, curriculum, assessments] = await Promise.all([
    getContentItem(course.id, contentSlug),
    getCourseContent(course.id),
    getCourseAssessments(course.id),
  ]);
  if (!item) notFound();

  const [draft, initialCodingAssignment] = await Promise.all([
    getAuthoringDraft(course.id, item.id),
    item.type === "Code"
      ? getCodingAssignmentFull(course.id, item.id)
      : Promise.resolve(null),
  ]);
  if (!draft.success) {
    if (draft.status === 404) notFound();
    throw new Error(draft.error);
  }

  return (
    <LessonAuthoringWorkspace
      courseId={course.id}
      courseSlug={course.slug}
      courseTitle={course.title}
      item={item}
      curriculum={curriculum.items}
      initialDraft={draft.data}
      initialCodingAssignment={initialCodingAssignment}
      linkedAssessment={
        ["Code", "Questionnaire", "Assignment", "Project"].includes(item.type)
          ? assessments.assessments.find(
              (assessment) => assessment.contentId === item.id,
            ) ?? null
          : null
      }
    />
  );
}
