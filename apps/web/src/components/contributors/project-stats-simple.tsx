import React from 'react';

export function ProjectStats() {
  return (
    <div className="relative min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 overflow-hidden">
      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-20 pb-16">
        <div className="text-center mb-16">
          <h1 className="text-5xl md:text-6xl font-bold mb-6">
            <span className="bg-gradient-to-r from-white via-blue-100 to-purple-200 bg-clip-text text-transparent">
              Powered by the community
            </span>
          </h1>
          <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
            GitHub is where the world builds software. Join millions of developers who are already building amazing things together.
          </p>
        </div>
        
        <div className="text-center text-white">
          Project Statistics Coming Soon...
        </div>
      </div>
    </div>
  );
}
