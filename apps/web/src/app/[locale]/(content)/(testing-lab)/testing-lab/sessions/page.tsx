import { getAvailableTestSessions } from '@/lib/api/testing-lab/test-sessions';
import { TestingLabSessions } from '@/components/testing-lab/testing-lab-sessions';

export default async function TestingLabSessionsPage() {
  const testSessions = await getAvailableTestSessions();

  return <TestingLabSessions testSessions={testSessions} />;
}
