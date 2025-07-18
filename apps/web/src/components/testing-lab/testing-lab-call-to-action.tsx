import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function TestingLabCallToAction() {
  return (
    <section className="py-16">
      <div className="max-w-4xl mx-auto text-center space-y-8">
        <div>
          <h2 className="text-3xl md:text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">
            Want to Get Involved?
          </h2>
          <p className="text-xl text-slate-300 max-w-2xl mx-auto">
            Join our community of testers and developers to shape the future of gaming
          </p>
        </div>

        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Button asChild size="lg" className="bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0 px-8 py-4 text-lg">
            <Link href="/testing-lab/join">Join as Tester</Link>
          </Button>
          <Button asChild size="lg" variant="outline" className="border-purple-600 bg-purple-800/20 text-purple-300 hover:bg-purple-700/30 px-8 py-4 text-lg">
            <Link href="/testing-lab/submit">Submit Your Game</Link>
          </Button>
        </div>

        {/* Additional Info */}
        <div className="bg-gradient-to-r from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm">
          <h4 className="text-lg font-semibold text-white mb-4">How It Works</h4>
          <div className="grid md:grid-cols-3 gap-6 text-left">
            <div>
              <div className="text-blue-400 font-semibold mb-2">1. Browse Sessions</div>
              <p className="text-slate-400 text-sm">
                Find testing sessions that match your interests and availability
              </p>
            </div>
            <div>
              <div className="text-purple-400 font-semibold mb-2">2. Join & Test</div>
              <p className="text-slate-400 text-sm">
                Participate in structured testing sessions with clear guidelines
              </p>
            </div>
            <div>
              <div className="text-green-400 font-semibold mb-2">3. Earn Rewards</div>
              <p className="text-slate-400 text-sm">
                Get credits, certificates, or free games for your valuable feedback
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
