import React from 'react';
import { Building, Database, Handshake, Heart } from 'lucide-react';

interface ProjectStat {
  icon: React.ReactNode;
  label: string;
  value: string;
  change: string;
}

export function ProjectStats() {
  // Stats matching the reference layout with 8 stats in 2 rows
  const topRowStats: ProjectStat[] = [
    {
      icon: <Heart className="w-4 h-4" />,
      label: 'Sponsors',
      value: '130K',
      change: '+24%',
    },
    {
      icon: <Handshake className="w-4 h-4" />,
      label: 'Sponsorships',
      value: '500K',
      change: '+15%',
    },
    {
      icon: <Building className="w-4 h-4" />,
      label: 'Organizations',
      value: '340M',
      change: '+18%',
    },
    {
      icon: <Database className="w-4 h-4" />,
      label: 'Repositories',
      value: '988M',
      change: '+5%',
    },
    {
      icon: '�',
      label: 'Repositories',
      value: '988M',
      change: '+5%',
    },
  ];

  const bottomRowStats: ProjectStat[] = [
    {
      icon: '�',
      label: 'Code searches',
      value: '391M',
      change: '+200%',
    },
    {
      icon: '�',
      label: 'Teams',
      value: '268K',
      change: '+18%',
    },
    {
      icon: '🎯',
      label: 'Achievements',
      value: '150M',
      change: '+20%',
    },
    {
      icon: '�',
      label: 'Most contributed repo',
      value: '19.8k',
      change: 'merged/month',
    },
  ];

  return (
    <section className="w-full mb-12">
      {/* Background with planet effect */}
      <div className="relative overflow-hidden bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        {/* Planet/sphere background */}
        <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 translate-y-1/2">
          <div className="w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/30 via-purple-500/20 to-transparent blur-3xl"></div>
        </div>

        {/* Additional glow effects */}
        <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-blue-400/50 to-transparent"></div>

        {/* Centered content container */}
        <div className="max-w-7xl mx-auto relative p-8 md:p-12">
          {/* Command line header */}
          <div className="mb-8">
            <div className="text-slate-400 text-sm font-mono mb-6">gh pulse --year 2022 --community-insights</div>
          </div>

          {/* Stats Grid - Top Row */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mb-8">
            {topRowStats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="flex items-center justify-center gap-2 mb-2">
                  <span className="text-slate-400 text-sm">{stat.icon}</span>
                  <span className="text-slate-400 text-sm">{stat.label}</span>
                </div>
                <div className="text-white text-3xl md:text-4xl font-bold mb-1">{stat.value}</div>
                <div className="text-slate-400 text-xs">{stat.change}</div>
              </div>
            ))}
          </div>

          {/* Stats Grid - Bottom Row */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mb-12">
            {bottomRowStats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="flex items-center justify-center gap-2 mb-2">
                  <span className="text-slate-400 text-sm">{stat.icon}</span>
                  <span className="text-slate-400 text-sm">{stat.label}</span>
                </div>
                <div className="text-white text-3xl md:text-4xl font-bold mb-1">{stat.value}</div>
                <div className="text-slate-400 text-xs">{stat.change}</div>
              </div>
            ))}
          </div>

          {/* Call to Action Section */}
          <div className="text-center relative z-10">
            <h2 className="text-white text-4xl md:text-5xl font-bold mb-4">
              Go further, read
              <br />
              the experts report
            </h2>
            <p className="text-slate-400 text-lg mb-8 max-w-2xl mx-auto">Read the Octoverse report to get the human insights</p>
            <button className="bg-white text-black px-8 py-3 rounded-lg font-medium hover:bg-slate-100 transition-colors">Read Octoverse 2022 →</button>
          </div>
        </div>
      </div>
    </section>
  );
}
