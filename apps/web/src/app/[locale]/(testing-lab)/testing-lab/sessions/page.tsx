import { getAvailableTestSessions } from '@/lib/admin';
import { TestingSessions } from '@/components/testing-lab/landing/testing-sessions';

export default async function TestingLabSessionsPage() {
  const testSessions = await getAvailableTestSessions();

  return <TestingSessions testSessions={testSessions} />;
}
