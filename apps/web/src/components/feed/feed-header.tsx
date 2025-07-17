import React from 'react';
import { MessageSquare, Users, Zap, TrendingUp } from 'lucide-react';
import { numberToAbbreviation } from '@/lib/utils';

interface FeedHeaderProps {
  totalPosts: number;
  activeUsers: number;
  engagementRate?: number;
}

export function FeedHeader({ totalPosts, activeUsers, engagementRate = 0 }: FeedHeaderProps) {
  return (
    <div className="mb-8">
      <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-6">
        Community Feed
      </h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Total Posts */}
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
          <div className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">
            {numberToAbbreviation(totalPosts)}
          </div>
          <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
            <MessageSquare className="w-4 h-4" />
            Total Posts
          </div>
        </div>

        {/* Active Community */}
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
          <div className="text-4xl font-bold bg-gradient-to-r from-green-400 to-emerald-400 bg-clip-text text-transparent mb-2">
            {numberToAbbreviation(activeUsers)}
          </div>
          <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
            <Users className="w-4 h-4" />
            Active Members
          </div>
        </div>

        {/* Engagement Rate */}
        <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
          <div className="text-4xl font-bold bg-gradient-to-r from-orange-400 to-red-400 bg-clip-text text-transparent mb-2">
            {engagementRate.toFixed(1)}%
          </div>
          <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
            <TrendingUp className="w-4 h-4" />
            Engagement
          </div>
        </div>
      </div>

      {/* Community Description */}
      <div className="mt-6 text-center">
        <p className="text-slate-400 max-w-2xl mx-auto leading-relaxed">
          Stay connected with the latest updates, achievements, and discussions from our vibrant gaming community.
          Discover new projects, celebrate milestones, and engage with fellow developers.
        </p>
      </div>
    </div>
  );
}
