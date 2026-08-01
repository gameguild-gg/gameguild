import { getMyLearnerRecords } from '@/lib/learner/records';
import { LearnerCalendar } from '@game-guild/courses/components/learner';

export default async function CalendarPage() {
  return <LearnerCalendar records={await getMyLearnerRecords()} />;
}
