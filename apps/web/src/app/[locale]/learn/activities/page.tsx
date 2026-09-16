import { getMyLearnerRecords } from '@/lib/learner/records';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { LearnerActivityCenter } from '@game-guild/courses/components/learner';

export default async function ActivitiesPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const routes = createLearnerRoutes(locale);
  return (
    <LearnerActivityCenter
      records={await getMyLearnerRecords()}
      courseBasePath={routes.courses}
    />
  );
}
