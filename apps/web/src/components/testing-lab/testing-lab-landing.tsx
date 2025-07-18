import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestingLabHero } from './testing-lab-hero';
import { TestSessionCard } from './test-session-card';
import { TestingLabStats } from './testing-lab-stats';
import { TestingLabCallToAction } from './testing-lab-call-to-action';

interface TestingLabLandingProps {
  testSessions: TestSession[];
}

export function TestingLabLanding({ testSessions }: TestingLabLandingProps) {
  const openSessions = testSessions.filter(session => session.status === 'open');
  const upcomingSessions = testSessions.filter(session => 
    new Date(session.sessionDate) > new Date()
  );

  return (
    <div className="container mx-auto px-4 py-8 space-y-12">
      {/* Hero Section */}
      <TestingLabHero />

      {/* Statistics Section */}
      <TestingLabStats 
        totalSessions={testSessions.length}
        openSessions={openSessions.length}
        upcomingSessions={upcomingSessions.length}
      />

      {/* Available Sessions */}
      <section className="space-y-6">
        <div className="text-center">
          <h2 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">
            Available Test Sessions
          </h2>
          <p className="text-slate-400 text-lg max-w-2xl mx-auto">
            Join exciting game testing sessions and help developers create better gaming experiences
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {openSessions.map((session) => (
            <TestSessionCard 
              key={session.id} 
              session={session} 
            />
          ))}
        </div>

        {openSessions.length === 0 && (
          <div className="text-center py-12">
            <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm">
              <h3 className="text-xl font-semibold text-white mb-2">No Open Sessions</h3>
              <p className="text-slate-400">
                Check back soon for new testing opportunities!
              </p>
            </div>
          </div>
        )}
      </section>

      {/* Call to Action */}
      <TestingLabCallToAction />
    </div>
  );
}
