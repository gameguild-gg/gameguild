import { getAvailableTestSessions } from '@/lib/api/testing-lab/test-sessions';
import { TestingSessions } from '@/components/testing-lab/sessions/landing/testing-sessions';

export default async function TestingLabSessionsPage() {
  const testSessions = await getAvailableTestSessions();

  return <TestingSessions testSessions={testSessions} />;
}
