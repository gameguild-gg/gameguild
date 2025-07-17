import React from 'react';
import { numberToAbbreviation } from '@/lib/utils';

interface ContributorsHeaderProps {
  totalContributors: number;
  totalParticipated: number;
}

export function ContributorsHeader({ totalContributors, totalParticipated }: ContributorsHeaderProps) {
  return (
    <div className="mb-8">
      <h1 className="text-4xl font-bold text-foreground mb-6">Contributors Leaderboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Total Contributors */}
        <div className="bg-card rounded-lg p-6 border border-border text-center">
          <div className="text-6xl font-bold text-foreground mb-2">{numberToAbbreviation(totalContributors)}</div>
          <p className="text-sm text-muted-foreground">Total Contributors</p>
        </div>

        {/* Active Contributors */}
        <div className="bg-card rounded-lg p-6 border border-border text-center">
          <div className="text-6xl font-bold text-foreground mb-2">{numberToAbbreviation(totalParticipated)}</div>
          <p className="text-sm text-muted-foreground">Active Contributors</p>
        </div>

        {/* Call to Action - Contribute */}
        <a
          href="#contributing-section"
          className="md:col-span-2 bg-gradient-to-r from-primary/10 to-accent/10 rounded-lg p-6 border border-primary/20 relative overflow-hidden hover:border-primary/30 transition-all hover:scale-[1.02] cursor-pointer block"
        >
          <div className="relative z-10">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-xl font-bold text-foreground mb-2">Start Contributing Today!</h3>
                <p className="text-muted-foreground">
                  Join our amazing community of developers and help make Game Guild even better. Every contribution counts!
                </p>
                <div className="flex items-center gap-2 mt-3 text-sm text-primary font-medium">
                  <span>Learn how to contribute</span>
                  <span>↓</span>
                </div>
              </div>
              <div className="text-4xl">🚀</div>
            </div>
          </div>
          {/* Decorative background pattern */}
          <div className="absolute inset-0 opacity-5">
            <div className="absolute top-4 right-4 text-6xl">💻</div>
            <div className="absolute bottom-4 left-4 text-4xl">⚡</div>
          </div>
        </a>
      </div>
    </div>
  );
}
