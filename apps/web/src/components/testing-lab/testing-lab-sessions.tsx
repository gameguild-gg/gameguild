'use client';

import { useState } from 'react';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestSessionGrid } from './test-session-grid';
import { TestSessionRow } from './test-session-row';
import { TestSessionTable } from './test-session-table';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Filter, LayoutGrid, List, Rows, Search } from 'lucide-react';
import Link from 'next/link';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

export function TestingLabSessions({ testSessions }: TestingLabSessionsProps) {
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>('cards');

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
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
          <div className="mb-6">
            {testSessions.length > 0 && (
              <div className="flex justify-center mb-6">
                <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 backdrop-blur-sm border border-blue-400/30 rounded-full px-4 py-2 flex items-center gap-2">
                  <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse"></div>
                  <span className="text-blue-300 font-semibold text-sm">
                    {testSessions.length} Open {testSessions.length === 1 ? 'Session' : 'Sessions'} • Join Now!
                  </span>
                </div>
              </div>
            )}
            <h1
              className="text-5xl md:text-6xl font-bold text-white my-8"
              style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}
            >
              Test. Play. Earn.
            </h1>
          </div>
          <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
            Join exclusive game testing sessions and shape the future of gaming. Get early access, provide valuable feedback, and earn amazing rewards while
            playing the latest titles before anyone else.
          </p>
        </div>

        {/* Filters, Search & View Toggle */}
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

          {/* View Mode Toggle */}
          <div className="flex gap-1 bg-slate-800/50 border border-slate-600 rounded-lg p-1">
            <Button
              variant={viewMode === 'cards' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('cards')}
              className={viewMode === 'cards' ? 'bg-blue-600 hover:bg-blue-700' : 'hover:bg-slate-700/50'}
            >
              <LayoutGrid className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === 'row' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('row')}
              className={viewMode === 'row' ? 'bg-blue-600 hover:bg-blue-700' : 'hover:bg-slate-700/50'}
            >
              <Rows className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === 'table' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('table')}
              className={viewMode === 'table' ? 'bg-blue-600 hover:bg-blue-700' : 'hover:bg-slate-700/50'}
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
        {/* Sessions Display */}
        {testSessions.length > 0 ? (
          <section className="mb-12">
            {viewMode === 'cards' && <TestSessionGrid sessions={testSessions} />}
            {viewMode === 'row' && <TestSessionRow sessions={testSessions} />}
            {viewMode === 'table' && <TestSessionTable sessions={testSessions} />}
          </section>
        ) : (
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
        )}
        {/* Call to Action */}
        <div className="text-center py-16">
          <div className="bg-gradient-to-r from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm max-w-4xl mx-auto">
            <h3 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">Want to Submit Your Game?</h3>
            <p className="text-slate-300 mb-6 max-w-2xl mx-auto">Get valuable feedback from experienced testers to improve your game before release.</p>
            <Button asChild size="lg" className="bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
              <Link href="/testing-lab/submit">Submit Your Game for Testing</Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
