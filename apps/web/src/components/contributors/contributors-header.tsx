import React from 'react';
import { numberToAbbreviation } from '@/lib/utils';
import { Code, Rocket, UserCheck, Users, Zap } from 'lucide-react';

interface ContributorsHeaderProps {
  totalContributors: number;
  totalParticipated: number;
}

export function ContributorsHeader({ totalContributors, totalParticipated }: ContributorsHeaderProps) {
  return (
    <div className="mb-8">
      <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-6">Contributors Leaderboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Total Contributors */}
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
          <div className="text-6xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">
            {numberToAbbreviation(totalContributors)}
          </div>
          <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
            <Users className="w-4 h-4" />
            Total Contributors
          </div>
        </div>

        {/* Active Contributors */}
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
          <div className="text-6xl font-bold bg-gradient-to-r from-green-400 to-emerald-400 bg-clip-text text-transparent mb-2">
            {numberToAbbreviation(totalParticipated)}
          </div>
          <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
            <UserCheck className="w-4 h-4" />
            Active Contributors
          </div>
        </div>

        {/* Call to Action - Contribute */}
        <a
          href="#contributing-section"
          className="md:col-span-2 bg-gradient-to-r from-purple-500/10 via-blue-500/5 to-green-500/10 rounded-lg p-6 relative overflow-hidden hover:bg-purple-500/20 transition-all hover:scale-[1.02] cursor-pointer block shadow-lg"
        >
          <div className="relative z-10">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-xl font-bold text-white mb-2">Start Contributing Today!</h3>
                <p className="text-slate-400">Join our amazing community of developers and help make Game Guild even better. Every contribution counts!</p>
                <div className="flex items-center gap-2 mt-3 text-sm text-purple-400 font-medium">
                  <span>Learn how to contribute</span>
                  <span>↓</span>
                </div>
              </div>
              <div className="p-3 bg-gradient-to-br from-purple-500 to-blue-500 rounded-lg">
                <Rocket className="w-8 h-8 text-white" />
              </div>
            </div>
          </div>
          {/* Decorative background pattern */}
          <div className="absolute inset-0 opacity-10">
            <div className="absolute top-4 right-4">
              <Code className="w-12 h-12 text-purple-400" />
            </div>
            <div className="absolute bottom-4 left-4">
              <Zap className="w-8 h-8 text-blue-400" />
            </div>
          </div>
        </a>
      </div>
    </div>
  );
}
