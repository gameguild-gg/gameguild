'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { MapPin, Code, BookOpen, Zap } from 'lucide-react';

interface RoadmapMilestone {
  id: string;
  version: string;
  title: string;
  status: 'completed' | 'current' | 'upcoming';
  progress: number;
  features: string[];
  description: string;
}

export function HorizontalRoadmap() {
  const [selectedMilestone, setSelectedMilestone] = useState('1.21.0');

  const milestones: RoadmapMilestone[] = [
    {
      id: '1.0.0',
      version: '1.0',
      title: 'Foundation',
      status: 'completed',
      progress: 100,
      features: ['Authentication', 'Dashboard', 'Competition System'],
      description: 'Core platform with essential features',
    },
    {
      id: '1.6.0',
      version: '1.6',
      title: 'Community',
      status: 'completed',
      progress: 100,
      features: ['GitHub Integration', 'Contributors', 'Funding'],
      description: 'Community engagement and collaboration tools',
    },
    {
      id: '1.12.0',
      version: '1.12',
      title: 'Learning',
      status: 'completed',
      progress: 100,
      features: ['Code Editor', 'Courses', 'Quiz System'],
      description: 'Educational platform and skill development',
    },
    {
      id: '1.18.0',
      version: '1.18',
      title: 'Advanced',
      status: 'completed',
      progress: 100,
      features: ['Analytics', 'Portfolio', 'Performance'],
      description: 'Enhanced user experience and optimization',
    },
    {
      id: '1.21.0',
      version: '1.21',
      title: 'Current',
      status: 'current',
      progress: 85,
      features: ['Robots.txt', 'UI/UX', 'Code Experience'],
      description: 'Latest improvements and user experience',
    },
    {
      id: '2.0.0',
      version: '2.0',
      title: 'Next Gen',
      status: 'upcoming',
      progress: 25,
      features: ['AI Integration', 'Mobile Apps', 'Real-time'],
      description: 'Revolutionary features and mobile platform',
    },
  ];

  const currentMilestone = milestones.find((m) => m.id === selectedMilestone) || milestones[4];

  return (
    <section className="w-full py-16 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 border-y border-slate-700">
      {/* Centered content container */}
      <div className="max-w-7xl mx-auto px-6">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-purple-500/10 to-blue-500/10 border border-purple-500/20 rounded-full mb-4">
            <MapPin className="w-4 h-4 text-purple-400" />
            <span className="text-purple-400 text-sm font-medium">Development Timeline</span>
          </div>
          <h2 className="text-3xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent mb-4">Development Roadmap</h2>
          <p className="text-slate-400 text-lg max-w-3xl mx-auto">Interactive timeline showing our journey and upcoming milestones</p>
        </div>

        {/* Horizontal Timeline Slider */}
        <div className="mb-8">
          <div className="relative rounded-xl p-6">
            {/* Timeline track */}
            <div className="relative mb-8">
              <div className="absolute top-1/2 left-0 right-0 h-2 bg-muted rounded-full transform -translate-y-1/2"></div>

              {/* Progress fill */}
              <div
                className="absolute top-1/2 left-0 h-2 bg-gradient-to-r from-purple-500 via-blue-500 to-cyan-400 rounded-full transform -translate-y-1/2 transition-all duration-500 shadow-lg"
                style={{ width: `${((milestones.findIndex((m) => m.id === selectedMilestone) + 1) / milestones.length) * 100}%` }}
              ></div>

              {/* Milestone markers */}
              <div className="relative flex justify-between">
                {milestones.map((milestone) => (
                  <button
                    key={milestone.id}
                    onClick={() => setSelectedMilestone(milestone.id)}
                    className={`relative flex flex-col items-center group transition-all duration-300 ${selectedMilestone === milestone.id ? 'z-10' : 'z-0'}`}
                  >
                    {/* Milestone dot */}
                    <div
                      className={`w-8 h-8 rounded-full border-4 flex items-center justify-center text-xs font-bold transition-all duration-300 ${
                        milestone.status === 'completed'
                          ? 'bg-gradient-to-r from-green-500 to-emerald-500 border-green-400 text-white shadow-lg shadow-green-500/30'
                          : milestone.status === 'current'
                            ? 'bg-gradient-to-r from-blue-500 to-purple-500 border-blue-400 text-white shadow-lg shadow-blue-500/30 animate-pulse'
                            : 'bg-gradient-to-r from-slate-600 to-slate-700 border-slate-500 text-slate-300'
                      } ${selectedMilestone === milestone.id ? 'scale-125 shadow-xl' : 'hover:scale-110'}`}
                    >
                      {milestone.version}
                    </div>

                    {/* Version label */}
                    <span
                      className={`mt-2 text-xs font-medium transition-colors ${
                        selectedMilestone === milestone.id ? 'text-foreground' : 'text-muted-foreground group-hover:text-foreground'
                      }`}
                    >
                      v{milestone.version}
                    </span>
                  </button>
                ))}
              </div>
            </div>

            {/* Selected milestone details */}
            <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 border border-slate-700/50 shadow-lg">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">{currentMilestone.title}</h3>
                  <p className="text-slate-300">{currentMilestone.description}</p>
                </div>
                <div className="text-right">
                  <div className="text-3xl font-bold bg-gradient-to-r from-green-400 to-emerald-400 bg-clip-text text-transparent mb-1">{currentMilestone.progress}%</div>
                  <div className="text-xs text-slate-400 uppercase tracking-wide">Complete</div>
                </div>
              </div>

              {/* Progress bar */}
              <div className="mb-4">
                <div className="w-full bg-slate-700 rounded-full h-3">
                  <div
                    className={`h-3 rounded-full transition-all duration-500 ${
                      currentMilestone.status === 'completed' ? 'bg-gradient-to-r from-green-500 to-emerald-500' : currentMilestone.status === 'current' ? 'bg-gradient-to-r from-blue-500 to-purple-500' : 'bg-gradient-to-r from-slate-500 to-slate-600'
                    }`}
                    style={{ width: `${currentMilestone.progress}%` }}
                  ></div>
                </div>
              </div>

              {/* Features */}
              <div>
                <h4 className="text-sm font-medium text-slate-300 mb-3">Key Features</h4>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                  {currentMilestone.features.map((feature, index) => (
                    <div key={index} className="flex items-center text-sm text-slate-300 bg-slate-800/50 rounded-lg p-3 border border-slate-600/30 hover:bg-slate-700/50 transition-colors">
                      <div
                        className={`w-2 h-2 rounded-full mr-3 ${
                          currentMilestone.status === 'completed' ? 'bg-green-400' : currentMilestone.status === 'current' ? 'bg-blue-400' : 'bg-slate-400'
                        }`}
                      ></div>
                      {feature}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Navigation hint */}
        <div className="text-center">
          <p className="text-muted-foreground text-sm">
            Click on any milestone above to explore the details •
            <Link href="/roadmap" className="text-primary hover:underline ml-1">
              View detailed roadmap
            </Link>
          </p>
        </div>
      </div>
    </section>
  );
}
