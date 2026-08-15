import { redirect } from 'next/navigation';

/**
 * Settings/General now redirects to Course Info/Info.
 * All course identity fields (title, slug, description, category, difficulty,
 * estimated hours, skills) have been consolidated into the Course Info tab.
 */
export default async function GeneralSettingsPage({ params }: { params: Promise<{ locale: string; course: string }> }) {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/platform/learning/courses/${courseId}/listing/info`);
}
