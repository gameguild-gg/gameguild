import React from 'react';
import { numberToAbbreviation } from '@/lib/utils';

interface Milestone {
  version: string;
  date: string;
  title: string;
  status: 'completed' | 'current' | 'upcoming';
  features: string[];
  progress: number;
}

export function ProjectRoadmap() {
  // Based on the actual changelog data from the GitHub repository
  const milestones: Milestone[] = [
    {
      version: '1.0.0',
      date: '2024-10-17',
      title: 'Platform Foundation',
      status: 'completed',
      features: ['Core authentication', 'Basic dashboard', 'Initial competition system', 'User profiles'],
      progress: 100,
    },
    {
      version: '1.6.0',
      date: '2025-01-08',
      title: 'Community Features',
      status: 'completed',
      features: ['GitHub issues integration', 'Contributors page', 'Funding system', 'Profile enhancements'],
      progress: 100,
    },
    {
      version: '1.12.0',
      date: '2025-01-31',
      title: 'Learning Platform',
      status: 'completed',
      features: ['Code editor', 'Courses API', 'Quiz system', 'Portfolio features'],
      progress: 100,
    },
    {
      version: '1.18.0',
      date: '2025-04-08',
      title: 'Advanced Features',
      status: 'completed',
      features: ['Enhanced courses', 'Better analytics', 'Portfolio improvements', 'Performance optimizations'],
      progress: 100,
    },
    {
      version: '1.21.0',
      date: '2025-05-15',
      title: 'Current Release',
      status: 'current',
      features: ['Robots.txt support', 'Unified coding experience', 'Enhanced UI/UX', 'Performance improvements'],
      progress: 85,
    },
    {
      version: '2.0.0',
      date: '2025-08-01',
      title: 'Next Generation',
      status: 'upcoming',
      features: ['Advanced AI integration', 'Mobile apps', 'Enhanced competition system', 'Real-time collaboration'],
      progress: 25,
    },
  ];

  const totalReleases = 21; // Based on current version 1.21.0
  const completedMilestones = milestones.filter((m) => m.status === 'completed').length;
  const currentProgress = Math.round((completedMilestones / milestones.length) * 100);

  return (
    <section className="mb-12">
      <div className="bg-card rounded-lg border border-border p-8">
        {/* Header */}
        <div className="text-center mb-8">
          <h2 className="text-3xl font-bold text-foreground mb-4">Project Roadmap</h2>
          <p className="text-muted-foreground text-lg max-w-3xl mx-auto">Track our journey from initial launch to becoming the premier gaming community platform. Each milestone represents significant features and improvements.</p>
        </div>

        {/* Progress Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
          <div className="text-center p-6 bg-background rounded-lg border border-border">
            <div className="text-4xl font-bold text-primary mb-2">{numberToAbbreviation(totalReleases)}</div>
            <div className="text-sm text-muted-foreground uppercase tracking-wide">Total Releases</div>
          </div>
          <div className="text-center p-6 bg-background rounded-lg border border-border">
            <div className="text-4xl font-bold text-accent mb-2">{currentProgress}%</div>
            <div className="text-sm text-muted-foreground uppercase tracking-wide">Roadmap Progress</div>
          </div>
          <div className="text-center p-6 bg-background rounded-lg border border-border">
            <div className="text-4xl font-bold text-foreground mb-2">{completedMilestones}</div>
            <div className="text-sm text-muted-foreground uppercase tracking-wide">Major Milestones</div>
          </div>
        </div>

        {/* Roadmap Timeline */}
        <div className="relative">
          {/* Timeline line */}
          <div className="absolute left-8 top-0 bottom-0 w-0.5 bg-border"></div>

          {/* Milestones */}
          <div className="space-y-8">
            {milestones.map((milestone) => (
              <div key={milestone.version} className="relative flex items-start">
                {/* Timeline dot */}
                <div
                  className={`relative z-10 flex items-center justify-center w-16 h-16 rounded-full border-4 ${
                    milestone.status === 'completed'
                      ? 'bg-primary border-primary text-primary-foreground'
                      : milestone.status === 'current'
                        ? 'bg-accent border-accent text-accent-foreground'
                        : 'bg-background border-border text-muted-foreground'
                  }`}
                >
                  <span className="text-sm font-bold">{milestone.version}</span>
                </div>

                {/* Content */}
                <div className="ml-8 flex-1">
                  <div className="bg-background rounded-lg border border-border p-6">
                    <div className="flex items-center justify-between mb-4">
                      <div>
                        <h3 className="text-xl font-semibold text-foreground">{milestone.title}</h3>
                        <p className="text-sm text-muted-foreground">{milestone.date}</p>
                      </div>
                      <div className="flex items-center space-x-2">
                        <span
                          className={`px-3 py-1 rounded-full text-xs font-medium ${
                            milestone.status === 'completed' ? 'bg-primary/10 text-primary' : milestone.status === 'current' ? 'bg-accent/10 text-accent' : 'bg-muted text-muted-foreground'
                          }`}
                        >
                          {milestone.status === 'completed' ? 'Released' : milestone.status === 'current' ? 'In Progress' : 'Planned'}
                        </span>
                      </div>
                    </div>

                    {/* Progress bar */}
                    <div className="mb-4">
                      <div className="flex items-center justify-between text-sm mb-2">
                        <span className="text-muted-foreground">Progress</span>
                        <span className="text-foreground font-medium">{milestone.progress}%</span>
                      </div>
                      <div className="w-full bg-muted rounded-full h-2">
                        <div
                          className={`h-2 rounded-full transition-all duration-300 ${milestone.status === 'completed' ? 'bg-primary' : milestone.status === 'current' ? 'bg-accent' : 'bg-muted-foreground'}`}
                          style={{ width: `${milestone.progress}%` }}
                        ></div>
                      </div>
                    </div>

                    {/* Features */}
                    <div>
                      <h4 className="text-sm font-medium text-foreground mb-2">Key Features</h4>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                        {milestone.features.map((feature, featureIndex) => (
                          <div key={featureIndex} className="flex items-center text-sm text-muted-foreground">
                            <div className={`w-1.5 h-1.5 rounded-full mr-2 ${milestone.status === 'completed' ? 'bg-primary' : milestone.status === 'current' ? 'bg-accent' : 'bg-muted-foreground'}`}></div>
                            {feature}
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Footer */}
        <div className="mt-10 text-center">
          <p className="text-muted-foreground text-sm">
            Stay tuned for more exciting features and improvements. Follow our{' '}
            <a href="https://github.com/gameguild-gg/website" className="text-primary hover:underline" target="_blank" rel="noopener noreferrer">
              GitHub repository
            </a>{' '}
            for the latest updates.
          </p>
        </div>
      </div>
    </section>
  );
}
