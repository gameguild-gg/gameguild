'use client';

import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function SubmitGameCTA() {
  return (
    <div className="text-center py-16">
      <div className="bg-gradient-to-r from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm max-w-4xl mx-auto">
        <h3 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">Want to Submit Your Game?</h3>
        <p className="text-slate-300 mb-6 max-w-2xl mx-auto">Get valuable feedback from experienced testers to improve your game before release.</p>
        <Button asChild size="lg" className="bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
          <Link href="/testing-lab/submit">Submit Your Game for Testing</Link>
        </Button>
      </div>
    </div>
  );
}
