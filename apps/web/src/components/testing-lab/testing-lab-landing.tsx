import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestingLabHero } from './testing-lab-hero';
import { TestingLabStats } from './testing-lab-stats';

interface TestingLabLandingProps {
  testSessions: TestSession[];
}

export function TestingLabLanding({ testSessions }: TestingLabLandingProps) {
  const openSessions = testSessions.filter((session) => session.status === 'open');
  const upcomingSessions = testSessions.filter((session) => new Date(session.sessionDate) > new Date());

  return (
    <div className="flex flex-col flex-1">
      <div className="flex flex-col flex-1 items-center justify-center">
        <div className="container ">
          {/* Hero Section */}
          <TestingLabHero />

          {/* Statistics Section */}
          <TestingLabStats totalSessions={testSessions.length} openSessions={openSessions.length} upcomingSessions={upcomingSessions.length} />

          {/* Call to Action */}
          {/*<TestingLabCallToAction />*/}
        </div>
      </div>
    </div>
  );
}
