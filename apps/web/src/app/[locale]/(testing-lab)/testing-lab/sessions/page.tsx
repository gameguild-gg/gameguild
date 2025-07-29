import { TestingSessions } from '@/components/testing-lab/landing/testing-sessions';
import { getAvailableTestSessions } from '@/lib/admin';

export default async function TestingLabSessionsPage() {
  const testSessions = await getAvailableTestSessions();

  return <TestingSessions testSessions={testSessions} />;
}
