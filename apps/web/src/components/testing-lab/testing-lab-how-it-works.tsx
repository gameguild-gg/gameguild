import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function TestingLabHowItWorks() {
  return (
    <section className="min-h-dvh flex items-center py-24 px-4">
      <div className="w-full text-center space-y-16 max-w-6xl mx-auto">
        <div>
          <h2
            className="text-4xl md:text-6xl font-bold text-white mb-6"
            style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}
          >
            How to Get Involved
          </h2>
          <p className="text-xl md:text-2xl text-slate-300 max-w-3xl mx-auto leading-relaxed drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">
            Join our community of testers and developers to shape the future of gaming
          </p>
        </div>

        {/* How It Works Process */}
        <div className="grid md:grid-cols-3 gap-12 text-center max-w-5xl mx-auto">
          <div className="space-y-4 relative">
            <div className="group relative bg-gradient-to-br from-blue-500/10 via-purple-500/5 to-blue-600/10 border border-blue-500/20 hover:border-blue-400/40 hover:from-blue-500/20 hover:via-purple-500/10 hover:to-blue-600/20 transition-all duration-300 hover:shadow-lg hover:shadow-blue-500/10 rounded-2xl p-8">
              {/* Step Number Inside Container */}
              <div className="relative mx-auto w-16 h-16 mb-6">
                <div className="absolute inset-0 bg-gradient-to-r from-blue-600 to-blue-700 rounded-full blur-lg opacity-50"></div>
                <div className="relative bg-gradient-to-r from-blue-600 to-blue-700 w-16 h-16 rounded-full flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
                  <span className="text-2xl font-bold text-white drop-shadow-lg">1</span>
                </div>
              </div>
              
              <h3 className="text-2xl font-bold text-blue-400 mb-4 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)] group-hover:text-blue-300 transition-colors">
                Find & Join Sessions
              </h3>
              <p className="text-lg text-slate-300 mb-6 leading-relaxed drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">
                Browse available testing sessions for games in development. Choose sessions that match your gaming interests and schedule.
              </p>
              <div className="space-y-3 text-left">
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-blue-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Filter by game genre and type</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-blue-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">View session requirements</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-blue-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Check available time slots</span>
                </div>
              </div>
            </div>
          </div>

          <div className="space-y-4 relative">
            <div className="group relative bg-gradient-to-br from-purple-500/10 via-pink-500/5 to-purple-600/10 border border-purple-500/20 hover:border-purple-400/40 hover:from-purple-500/20 hover:via-pink-500/10 hover:to-purple-600/20 transition-all duration-300 hover:shadow-lg hover:shadow-purple-500/10 rounded-2xl p-8">
              {/* Step Number Inside Container */}
              <div className="relative mx-auto w-16 h-16 mb-6">
                <div className="absolute inset-0 bg-gradient-to-r from-purple-600 to-purple-700 rounded-full blur-lg opacity-50"></div>
                <div className="relative bg-gradient-to-r from-purple-600 to-purple-700 w-16 h-16 rounded-full flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
                  <span className="text-2xl font-bold text-white drop-shadow-lg">2</span>
                </div>
              </div>
              
              <h3 className="text-2xl font-bold text-purple-400 mb-4 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)] group-hover:text-purple-300 transition-colors">
                Test & Provide Feedback
              </h3>
              <p className="text-lg text-slate-300 mb-6 leading-relaxed drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">
                Participate in guided testing sessions with clear objectives. Follow structured protocols to ensure valuable feedback.
              </p>
              <div className="space-y-3 text-left">
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-purple-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Follow testing guidelines</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-purple-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Report bugs and issues</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-purple-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Share gameplay experiences</span>
                </div>
              </div>
            </div>
          </div>

          <div className="space-y-4 relative">
            <div className="group relative bg-gradient-to-br from-green-500/10 via-emerald-500/5 to-green-600/10 border border-green-500/20 hover:border-green-400/40 hover:from-green-500/20 hover:via-emerald-500/10 hover:to-green-600/20 transition-all duration-300 hover:shadow-lg hover:shadow-green-500/10 rounded-2xl p-8">
              {/* Step Number Inside Container */}
              <div className="relative mx-auto w-16 h-16 mb-6">
                <div className="absolute inset-0 bg-gradient-to-r from-green-600 to-green-700 rounded-full blur-lg opacity-50"></div>
                <div className="relative bg-gradient-to-r from-green-600 to-green-700 w-16 h-16 rounded-full flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
                  <span className="text-2xl font-bold text-white drop-shadow-lg">3</span>
                </div>
              </div>
              
              <h3 className="text-2xl font-bold text-green-400 mb-4 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)] group-hover:text-green-300 transition-colors">
                Earn Recognition
              </h3>
              <p className="text-lg text-slate-300 mb-6 leading-relaxed drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">
                Get rewarded for your contributions with credits, certificates, early access to games, and community recognition.
              </p>
              <div className="space-y-3 text-left">
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-green-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Testing credits and points</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-green-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Achievement certificates</span>
                </div>
                <div className="flex items-center gap-3 text-slate-400">
                  <div className="w-2 h-2 bg-green-400 rounded-full shadow-lg"></div>
                  <span className="drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Free game copies</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row gap-6 justify-center pt-8">
          <div className="relative">
            <div className="absolute inset-0 bg-blue-500/20 shadow-[0_0_30px_rgba(59,130,246,0.8)] animate-pulse rounded-lg"></div>
            <Button
              asChild
              size="lg"
              className="relative bg-gradient-to-r from-blue-600/50 to-blue-700/50 backdrop-blur-md border border-blue-400/60 text-white hover:from-blue-600/90 hover:to-blue-700/90 hover:border-blue-300/90 px-12 py-6 text-xl font-semibold shadow-2xl transition-all duration-200 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]"
            >
              <Link href="/testing-lab/sessions">Browse Sessions</Link>
            </Button>
          </div>
          <Button
            asChild
            size="lg"
            variant="outline"
            className="bg-purple-800/20 backdrop-blur-md border border-purple-600/60 text-purple-300 hover:bg-purple-700/30 hover:border-purple-500/80 px-12 py-6 text-xl drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]"
          >
            <Link href="/testing-lab/submit">Submit Your Game</Link>
          </Button>
        </div>
      </div>
    </section>
  );
}
