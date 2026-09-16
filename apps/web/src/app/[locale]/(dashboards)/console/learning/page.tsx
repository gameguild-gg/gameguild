import { redirect } from '@/i18n/navigation';

/**
 * L1: Learning Home Redirect
 *
 * Redirects to the overview page which contains the instructor dashboard
 * with KPIs, metrics, and recent activity.
 */
export default async function Page({ params }: PageProps<'/[locale]/console/learning'>): Promise<void> {
  const { locale } = await params;
  return redirect({ href: '/console/learning/overview', locale });
}
