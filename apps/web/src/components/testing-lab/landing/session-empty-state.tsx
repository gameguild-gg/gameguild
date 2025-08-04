'use client';

import { Button } from '@/components/ui/button';
import Link from 'next/link';
import { Search } from 'lucide-react';

interface SessionEmptyStateProps {
  hasFilters: boolean;
  hasSessions: boolean;
}

export function SessionEmptyState({ hasFilters, hasSessions }: SessionEmptyStateProps) {
  if (hasFilters && hasSessions) {
    return (
      <div className="text-center py-16">
        <div
          className="backdrop-blur-md border border-slate-600/30 rounded-2xl p-12 max-w-2xl mx-auto relative overflow-hidden"
          style={{
            background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
            boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05), 0 8px 32px rgba(0, 0, 0, 0.3)',
          }}
        >
          {/* Decorative elements */}
          <div className="absolute top-4 left-4 w-16 h-16 bg-blue-500/10 rounded-full blur-xl"></div>
          <div className="absolute bottom-4 right-4 w-12 h-12 bg-purple-500/10 rounded-full blur-lg"></div>
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-32 h-32 bg-slate-500/5 rounded-full blur-2xl"></div>

          {/* Content */}
          <div className="relative z-10">
            <div className="mb-6">
              <div
                className="w-16 h-16 mx-auto mb-4 rounded-full border border-blue-400/30 flex items-center justify-center"
                style={{
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.2) 0%, rgba(37, 99, 235, 0.15) 50%, rgba(29, 78, 216, 0.1) 100%)',
                }}
              >
                <Search className="h-6 w-6 text-blue-400" />
              </div>
              <h3 className="text-2xl font-semibold text-white mb-3">No sessions match your filters</h3>
              <p className="text-slate-400 leading-relaxed">Try adjusting your search terms or filters to find the testing sessions you're looking for. New sessions are added regularly!</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!hasSessions) {
    return (
      <div className="text-center py-16">
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-12 backdrop-blur-sm max-w-2xl mx-auto">
          <h3 className="text-2xl font-semibold text-white mb-4">No Sessions Available</h3>
          <p className="text-slate-400 mb-6">Check back soon for new testing opportunities, or submit your own game for testing!</p>
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
    );
  }

  return null;
}
