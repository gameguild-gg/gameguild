import { redirect } from "@/i18n/navigation";
import { getCourse, getContentItem } from "@/lib/learning";
import { getCourseRouteParam } from "@/lib/learning/course-route";
import { notFound } from "next/navigation";

/**
 * The authoring surface was consolidated into the workspace editor. The console
 * content item route is preserved as a redirect so existing deep links (course
 * content tree, resources/tutorials lists, learner "edit" button) all funnel into
 * the single /workspace/learning authoring editor.
 */
export default async function ContentItemRedirectPage({
  params,
}: PageProps<"/[locale]/console/learning/courses/[course]/content/[contentSlug]">): Promise<void> {
  const { locale, course: courseIdentifier, contentSlug } = await params;
  const course = await getCourse(courseIdentifier);
  if (!course) notFound();
  const item = await getContentItem(course.id, contentSlug);
  if (!item) notFound();
  const courseRouteParam = getCourseRouteParam(course);

  redirect({
    href: `/workspace/learning/courses/${encodeURIComponent(courseRouteParam)}/content/${encodeURIComponent(item.slug ?? item.id)}`,
    locale,
  });
}
