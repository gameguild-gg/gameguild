import { TestTube, Users, Gamepad2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function TestingLabHero() {
  return (
    <section className="text-center py-16 px-4">
      <div className="max-w-4xl mx-auto">
        {/* Hero Icon */}
        <div className="flex justify-center mb-8">
          <div className="relative">
            <div className="absolute inset-0 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full blur-xl opacity-50"></div>
            <div className="relative bg-gradient-to-r from-blue-600 to-purple-600 p-6 rounded-full">
              <TestTube className="h-12 w-12 text-white" />
            </div>
          </div>
        </div>

        {/* Main Headline */}
        <h1 className="text-5xl md:text-6xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-6">Game Testing Lab</h1>

        {/* Subheadline */}
        <p className="text-xl md:text-2xl text-slate-300 mb-8 leading-relaxed">
          Help developers create amazing games by joining testing sessions
          <br />
          <span className="text-blue-400">Earn rewards while gaming</span>
        </p>

        {/* Key Features */}
        <div className="grid md:grid-cols-3 gap-8 mb-12">
          <div className="flex flex-col items-center space-y-3">
            <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 p-4 rounded-full border border-slate-600">
              <Gamepad2 className="h-8 w-8 text-blue-400" />
            </div>
            <h3 className="text-lg font-semibold text-white">Test Latest Games</h3>
            <p className="text-slate-400 text-center">Get early access to upcoming games and provide valuable feedback</p>
          </div>

          <div className="flex flex-col items-center space-y-3">
            <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 p-4 rounded-full border border-slate-600">
              <Users className="h-8 w-8 text-purple-400" />
            </div>
            <h3 className="text-lg font-semibold text-white">Join Community</h3>
            <p className="text-slate-400 text-center">Connect with other testers and developers in collaborative sessions</p>
          </div>

          <div className="flex flex-col items-center space-y-3">
            <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 p-4 rounded-full border border-slate-600">
              <TestTube className="h-8 w-8 text-green-400" />
            </div>
            <h3 className="text-lg font-semibold text-white">Earn Rewards</h3>
            <p className="text-slate-400 text-center">Get credits, certificates, and free games for your contributions</p>
          </div>
        </div>

        {/* Call to Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <div className="relative">
            <div className="absolute inset-0 bg-purple-500/20 shadow-[0_0_20px_rgba(168,85,247,0.8)] animate-pulse rounded-lg"></div>
            <Button
              asChild
              size="lg"
              className="relative bg-gradient-to-r from-purple-600/30 to-purple-500/30 backdrop-blur-md border border-purple-400/40 text-white hover:from-purple-600/90 hover:to-purple-500/90 hover:border-purple-300/90 px-8 py-4 text-lg font-semibold shadow-lg transition-all duration-200"
            >
              <Link href="/testing-lab/sessions">Browse Sessions</Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
