import { ContributorsHeader } from '@/components/contributors/contributors-header';
import { GitHubProjectStats } from '@/components/contributors/git-hub-project-stats';
import { GlobalRankingTable } from '@/components/contributors/global-ranking-table';
import { HorizontalRoadmap } from '@/components/contributors/horizontal-roadmap';
import { HowToContribute } from '@/components/contributors/how-to-contribute';
import { TopContributorsSection } from '@/components/contributors/top-contributors-section';
import { getContributors, getGitHubRepositoryData } from '@/lib/integrations/github';
import { Metadata } from 'next';
import React from 'react';

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: {
      template: '%s | Contributors',
      default: 'Contributors',
    },
    description: 'List of contributors to the Game Guild website',
    openGraph: {
      title: 'Game Guild Contributors',
      description: 'Meet the amazing people who contribute to Game Guild',
      type: 'website',
    },
  };
}

export default async function Page(): Promise<React.JSX.Element> {
  const [contributors, repositoryData] = await Promise.all([getContributors(), getGitHubRepositoryData()]);

  // Calculate real stats from contributors data
  const totalContributors = contributors.length;
  const totalParticipated = contributors.filter((contributor) => (contributor.contributions || 0) > 0).length;

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      {/* Project Stats Overview - Full Width */}
      <GitHubProjectStats repositoryData={repositoryData} />

      {/* Other sections with centered content */}
      <div className="container mx-auto px-4">
        {/* Header with stats */}
        <ContributorsHeader totalContributors={totalContributors} totalParticipated={totalParticipated} />

        {/* Top 3 Contributors */}
        <TopContributorsSection contributors={contributors} />

        {/* Global Ranking Table */}
        <div className="py-8">
          <GlobalRankingTable contributors={contributors} />
        </div>
      </div>

      {/* Interactive Roadmap - Full Width */}
      <HorizontalRoadmap />

      {/* How to Contribute Section - Full Width */}
      <HowToContribute />

      {/* Gource Video Section - Full Width */}
      <div className="py-8 sm:py-12 lg:py-16 bg-slate-900">
        <div className="container mx-auto px-4">
          <div className="text-center mb-6 sm:mb-8">
            <h2 className="text-2xl sm:text-3xl font-bold text-white mb-2 sm:mb-4">Project Evolution</h2>
            <p className="text-slate-400 text-base sm:text-lg">Watch the visual history of our project&apos;s development</p>
          </div>
          <div className="max-w-4xl mx-auto">
            <div className="aspect-video w-full overflow-hidden rounded-lg shadow-2xl bg-slate-800">
              <video
                id="gource-video"
                controls
                muted
                playsInline
                className="w-full h-full object-cover"
                poster="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 675'%3E%3Crect width='1200' height='675' fill='%23334155'/%3E%3Ctext x='50%25' y='50%25' dominant-baseline='middle' text-anchor='middle' fill='%23cbd5e1' font-size='24' font-family='system-ui'%3EProject Evolution Video%3C/text%3E%3C/svg%3E"
              >
                <source src="https://gameguild-gg.github.io/gameguild/gource.mp4" type="video/mp4" />
                Your browser does not support the video tag.
              </video>
            </div>
          </div>
        </div>
      </div>

      {/* Auto-play script */}
      <script
        dangerouslySetInnerHTML={{
          __html: `
            if (typeof window !== 'undefined') {
              const video = document.getElementById('gource-video');
              if (video) {
                const observer = new IntersectionObserver((entries) => {
                  entries.forEach((entry) => {
                    if (entry.isIntersecting && entry.intersectionRatio > 0.5) {
                      video.play().catch(() => {
                        // Auto-play failed, user interaction required
                      });
                    } else {
                      video.pause();
                    }
                  });
                }, {
                  threshold: 0.5
                });
                observer.observe(video);
              }
            }
          `,
        }}
      />
    </div>
  );
}
