import React from 'react';
import { numberToAbbreviation } from '@/lib/utils';

interface LeaderboardHeaderProps {
  totalContributors: number;
  totalParticipated: number;
}

export const LeaderboardHeader: React.FC<LeaderboardHeaderProps> = ({ totalContributors, totalParticipated }) => {
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
        <div className="md:col-span-2 bg-gradient-to-r from-primary/10 to-accent/10 rounded-lg p-6 border border-primary/20 relative overflow-hidden">
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="text-xl font-bold text-foreground mb-2">Start Contributing Today!</h3>
                <p className="text-muted-foreground mb-4">
                  Join our amazing community of developers and help make Game Guild even better. Every contribution counts!
                </p>
              </div>
              <div className="text-4xl">🚀</div>
            </div>
            <div className="flex flex-col sm:flex-row gap-3">
              <a
                href="https://github.com/gameguild-gg/website"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors font-medium"
              >
                View Repository
              </a>
              <a
                href="https://github.com/gameguild-gg/website/issues"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center justify-center px-4 py-2 border border-border text-foreground rounded-md hover:bg-muted transition-colors font-medium"
              >
                Browse Issues
              </a>
            </div>
          </div>
          {/* Decorative background pattern */}
          <div className="absolute inset-0 opacity-5">
            <div className="absolute top-4 right-4 text-6xl">💻</div>
            <div className="absolute bottom-4 left-4 text-4xl">⚡</div>
          </div>
        </div>
      </div>
    </div>
  );
};
