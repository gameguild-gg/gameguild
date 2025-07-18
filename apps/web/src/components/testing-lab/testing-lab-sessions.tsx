import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestSessionCard } from './test-session-card';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Filter, Search } from 'lucide-react';
import Link from 'next/link';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

export function TestingLabSessions({ testSessions }: TestingLabSessionsProps) {
  const openSessions = testSessions.filter((session) => session.status === 'open');
  const fullSessions = testSessions.filter((session) => session.status === 'full');

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <div className="container mx-auto px-4 py-8">
        {/* Navigation */}
        <div className="flex items-center gap-4 mb-8">
          <Button asChild variant="ghost" className="text-slate-400 hover:text-white">
            <Link href="/testing-lab">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Testing Lab
            </Link>
          </Button>
        </div>

        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-5xl md:text-6xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-6">
            Available Sessions
          </h1>
          <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
            Browse and join exciting game testing sessions. Help developers create better gaming experiences while earning rewards.
          </p>
        </div>

        {/* Filters & Search (placeholder for future implementation) */}
        <div className="flex flex-col sm:flex-row gap-4 mb-8">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
            <input
              type="text"
              placeholder="Search sessions..."
              className="w-full pl-10 pr-4 py-3 bg-slate-800/50 border border-slate-600 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:border-blue-500"
              disabled
            />
          </div>
          <Button variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50" disabled>
            <Filter className="h-4 w-4 mr-2" />
            Filter
          </Button>
        </div>

        {/* Open Sessions */}
        {openSessions.length > 0 && (
          <section className="mb-12">
            <div className="flex items-center gap-3 mb-6">
              <h2 className="text-2xl font-bold text-white">Open Sessions</h2>
              <div className="bg-green-600 text-white text-sm px-3 py-1 rounded-full font-medium">
                {openSessions.length} available
              </div>
            </div>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {openSessions.map((session) => (
                <TestSessionCard key={session.id} session={session} />
              ))}
            </div>
          </section>
        )}

        {/* Full Sessions */}
        {fullSessions.length > 0 && (
          <section className="mb-12">
            <div className="flex items-center gap-3 mb-6">
              <h2 className="text-2xl font-bold text-white">Full Sessions</h2>
              <div className="bg-red-600 text-white text-sm px-3 py-1 rounded-full font-medium">
                {fullSessions.length} full
              </div>
            </div>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {fullSessions.map((session) => (
                <TestSessionCard key={session.id} session={session} />
              ))}
            </div>
          </section>
        )}

        {/* All Other Sessions */}
        {testSessions.filter((session) => session.status !== 'open' && session.status !== 'full').length > 0 && (
          <section className="mb-12">
            <div className="flex items-center gap-3 mb-6">
              <h2 className="text-2xl font-bold text-white">Other Sessions</h2>
            </div>
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {testSessions
                .filter((session) => session.status !== 'open' && session.status !== 'full')
                .map((session) => (
                  <TestSessionCard key={session.id} session={session} />
                ))}
            </div>
          </section>
        )}

        {/* Empty State */}
        {testSessions.length === 0 && (
          <div className="text-center py-16">
            <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-12 backdrop-blur-sm max-w-2xl mx-auto">
              <h3 className="text-2xl font-semibold text-white mb-4">No Sessions Available</h3>
              <p className="text-slate-400 mb-6">
                Check back soon for new testing opportunities, or submit your own game for testing!
              </p>
              <div className="flex flex-col sm:flex-row gap-4 justify-center">
                <Button asChild variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50">
                  <Link href="/testing-lab">Back to Testing Lab</Link>
                </Button>
                <Button asChild className="bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
                  <Link href="/testing-lab/submit">Submit Your Game</Link>
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Call to Action */}
        <div className="text-center py-16">
          <div className="bg-gradient-to-r from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm max-w-4xl mx-auto">
            <h3 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">
              Want to Submit Your Game?
            </h3>
            <p className="text-slate-300 mb-6 max-w-2xl mx-auto">
              Get valuable feedback from experienced testers to improve your game before release.
            </p>
            <Button asChild size="lg" className="bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
              <Link href="/testing-lab/submit">Submit Your Game for Testing</Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
