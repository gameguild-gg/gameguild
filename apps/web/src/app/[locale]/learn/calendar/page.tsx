import { getMyLearnerRecords } from '@/lib/learner/records';
import { LearnerCalendar } from '@game-guild/courses/components/learner';

export default async function CalendarPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  return <LearnerCalendar records={await getMyLearnerRecords()} locale={locale} />;
}
