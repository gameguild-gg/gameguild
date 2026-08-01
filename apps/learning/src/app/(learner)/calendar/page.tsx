import { LearnerCalendar } from '@/components/learner-records';
import { getMyLearnerRecords } from '@/lib/learner-data';

export default async function CalendarPage() {
    return <LearnerCalendar records={await getMyLearnerRecords()} />;
}