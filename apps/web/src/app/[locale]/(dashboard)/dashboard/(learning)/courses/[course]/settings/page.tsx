import { redirect } from 'next/navigation';

/**
 * Settings Index Redirect
 * /settings → /settings/access
 *
 * Settings are now organized into subroutes:
 * - /settings/access - Visibility, enrollment rules
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings'>): Promise<never> {
  const { locale, course: courseId } = await params;
  redirect(`/${locale}/dashboard/learning/courses/${courseId}/settings/access`);
}
