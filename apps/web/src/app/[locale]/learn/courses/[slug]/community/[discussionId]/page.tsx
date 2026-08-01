import { CourseAccessGate } from "@/components/learning/course-access-gate";
import { CourseDiscussionThread } from "@/components/learning/course-discussion-thread";
import { getCourseAccessData } from "@/lib/learner/courses";
import { getCourseDiscussionThread } from "@/lib/learner/records";
import { notFound } from "next/navigation";

export default async function CourseDiscussionPage({
  params,
}: {
  params: Promise<{ slug: string; discussionId: string }>;
}) {
  const { slug, discussionId } = await params;
  const access = await getCourseAccessData(slug);

  if (access.kind === "not-found") notFound();
  if (access.kind !== "ready") return <CourseAccessGate access={access} />;

  const thread = await getCourseDiscussionThread(discussionId);
  if (!thread || thread.discussion.courseId !== access.course.id) notFound();

  return (
    <CourseDiscussionThread
      courseSlug={slug}
      courseTitle={access.course.title}
      discussion={thread.discussion}
      replies={thread.replies}
    />
  );
}
