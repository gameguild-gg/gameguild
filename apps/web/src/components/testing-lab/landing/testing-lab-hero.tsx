import { Gamepad2, TestTube, Users } from 'lucide-react';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function TestingLabHero() {
  return (
    <section className="text-center py-16 px-4">
      <div className="max-w-6xl mx-auto relative">
        <div className="relative z-10 p-8">
          {/* Status Chip */}
          <div className="flex justify-center mb-10">
            <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 backdrop-blur-sm border border-blue-400/30 rounded-full px-4 py-2 flex items-center gap-2">
              <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse"></div>
              <span className="text-blue-300 font-semibold text-sm">Now Live</span>
            </div>
          </div>

          {/* Main Headline with Icon */}
          <div className="flex items-center justify-center gap-6 mb-12">
            {/* Hero Icon */}
            <div className="relative">
              <div className="absolute inset-0 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full blur-xl opacity-50"></div>
              <div className="relative bg-gradient-to-r from-blue-600 to-purple-600 p-4 rounded-full">
                <TestTube className="h-8 w-8 text-white" />
              </div>
            </div>

            {/* Title */}
            <h1 className="text-5xl md:text-6xl font-bold text-white" style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}>
              Game Testing Lab
            </h1>
          </div>

          {/* Subheadline */}
          <p className="text-xl md:text-2xl text-slate-300 mb-16 leading-relaxed drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">
            Help developers create amazing games by joining testing sessions
            <br />
            <span className="text-blue-400">Earn rewards while gaming</span>
          </p>

          {/* Key Features */}
          <div className="grid md:grid-cols-3 gap-12 mb-16 max-w-5xl mx-auto">
            <div className="flex flex-col items-center space-y-3">
              <div className="bg-gradient-to-br from-slate-800/70 to-slate-700/70 backdrop-blur-md p-4 rounded-full border border-slate-600/50 shadow-lg">
                <Gamepad2 className="h-8 w-8 text-blue-400" />
              </div>
              <h3 className="text-lg font-semibold text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">Test Latest Games</h3>
              <p className="text-slate-400 text-center drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Get early access to upcoming games and provide valuable feedback</p>
            </div>

            <div className="flex flex-col items-center space-y-3">
              <div className="bg-gradient-to-br from-slate-800/70 to-slate-700/70 backdrop-blur-md p-4 rounded-full border border-slate-600/50 shadow-lg">
                <Users className="h-8 w-8 text-purple-400" />
              </div>
              <h3 className="text-lg font-semibold text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">Join Community</h3>
              <p className="text-slate-400 text-center drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Connect with other testers and developers in collaborative sessions</p>
            </div>

            <div className="flex flex-col items-center space-y-3">
              <div className="bg-gradient-to-br from-slate-800/70 to-slate-700/70 backdrop-blur-md p-4 rounded-full border border-slate-600/50 shadow-lg">
                <TestTube className="h-8 w-8 text-green-400" />
              </div>
              <h3 className="text-lg font-semibold text-white drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]">Earn Rewards</h3>
              <p className="text-slate-400 text-center drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">Get credits, certificates, and free games for your contributions</p>
            </div>
          </div>

          {/* Call to Action Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <div className="relative">
              <div className="absolute inset-0 bg-purple-500/20 shadow-[0_0_20px_rgba(168,85,247,0.8)] animate-pulse rounded-lg"></div>
              <Button
                asChild
                size="lg"
                className="relative bg-gradient-to-r from-purple-600/50 to-purple-500/50 backdrop-blur-md border border-purple-400/60 text-white hover:from-purple-600/90 hover:to-purple-500/90 hover:border-purple-300/90 px-8 py-4 text-lg font-semibold shadow-lg transition-all duration-200 drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)]"
              >
                <Link href="/testing-lab/sessions">Browse Sessions</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
