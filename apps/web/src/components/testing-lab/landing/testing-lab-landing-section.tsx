import {TestingLabHowItWorks} from '@/components/testing-lab';
import {SESSION_STATUS, TestSession} from '@/lib/admin';
import {FloatingIcons} from '../common/ui/floating-icons';
import {TestingLabHero} from './testing-lab-hero';
import {TestingLabLearnMore} from './testing-lab-learn-more';
import {TestingLabStats} from './testing-lab-stats';

interface TestingLabLandingProps {
  testSessions: TestSession[];
}

export function TestingLabLandingSection({testSessions}: TestingLabLandingProps) {
  // Filter sessions by status using constants
  const openSessions = testSessions.filter((session) => session.status === SESSION_STATUS.SCHEDULED || session.status === SESSION_STATUS.ACTIVE);
  const upcomingSessions = testSessions.filter((session) => new Date(session.sessionDate) > new Date());

  return (
    <div className="flex flex-col flex-1 relative">
      <FloatingIcons/>
      <div className="flex flex-col flex-1 items-center justify-center relative">
        <main className="container">
          {/* Hero Section */}
          <TestingLabHero/>
          {/* Statistics Section */}
          <TestingLabStats totalSessions={testSessions.length} openSessions={openSessions.length}
                           upcomingSessions={upcomingSessions.length}/>
          {/* Learn More Section */}
          <TestingLabLearnMore/>
          {/* How It Works Section */}
        </main>
        <aside id="learn-more" className="flex flex-col flex-1 items-center justify-center container">
          <TestingLabHowItWorks/>
        </aside>
      </div>
    </div>
  );
}
