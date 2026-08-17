import { redirect } from 'next/navigation';

/**
 * L1: Learning Home Redirect
 *
 * Redirects to the overview page which contains the instructor dashboard
 * with KPIs, metrics, and recent activity.
 */
export default async function Page({ params }: PageProps<'/[locale]/console/learning'>): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/console/learning/overview`);
}
