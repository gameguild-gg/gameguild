import React from 'react';
import { Code, BookOpen, Bug, Github, Target, GitFork } from 'lucide-react';

export function HowToContribute() {
  return (
    <section id="contributing-section" className="w-full py-16 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 border-t border-slate-700">
      {/* Centered content container */}
      <div className="max-w-7xl mx-auto px-6">
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-green-500/10 to-blue-500/10 border border-green-500/20 rounded-full mb-4">
            <Code className="w-4 h-4 text-green-400" />
            <span className="text-green-400 text-sm font-medium">Open Source Contribution</span>
          </div>
          <h2 className="text-3xl font-bold bg-gradient-to-r from-green-400 to-blue-400 bg-clip-text text-transparent mb-4">How to Contribute</h2>
          <p className="text-slate-400 text-lg max-w-3xl mx-auto">
            Whether you&apos;re a seasoned developer or just starting out, there are many ways to contribute to Game Guild. Every contribution helps make our
            platform better for the gaming community.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {/* Code Contributions */}
          <div className="group relative bg-gradient-to-br from-blue-500/10 via-purple-500/5 to-blue-600/10 rounded-xl p-6 text-center border border-blue-500/20 hover:border-blue-400/40 transition-all duration-300 hover:shadow-lg hover:shadow-blue-500/10">
            <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-blue-600 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:scale-110 transition-transform duration-300">
              <Code className="w-6 h-6 text-white" />
            </div>
            <h3 className="text-lg font-bold text-white mb-2 group-hover:text-blue-400 transition-colors">Code Contributions</h3>
            <p className="text-sm text-slate-400">Submit bug fixes, implement new features, or improve existing functionality.</p>
          </div>

          {/* Documentation */}
          <div className="group relative bg-gradient-to-br from-green-500/10 via-emerald-500/5 to-green-600/10 rounded-xl p-6 text-center border border-green-500/20 hover:border-green-400/40 transition-all duration-300 hover:shadow-lg hover:shadow-green-500/10">
            <div className="w-12 h-12 bg-gradient-to-br from-green-500 to-green-600 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:scale-110 transition-transform duration-300">
              <BookOpen className="w-6 h-6 text-white" />
            </div>
            <h3 className="text-lg font-bold text-white mb-2 group-hover:text-green-400 transition-colors">Documentation</h3>
            <p className="text-sm text-slate-400">Help improve our docs, write tutorials, or translate content.</p>
          </div>

          {/* Bug Reports */}
          <div className="group relative bg-gradient-to-br from-orange-500/10 via-red-500/5 to-orange-600/10 rounded-xl p-6 text-center border border-orange-500/20 hover:border-orange-400/40 transition-all duration-300 hover:shadow-lg hover:shadow-orange-500/10">
            <div className="w-12 h-12 bg-gradient-to-br from-orange-500 to-orange-600 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:scale-110 transition-transform duration-300">
              <Bug className="w-6 h-6 text-white" />
            </div>
            <h3 className="text-lg font-bold text-white mb-2 group-hover:text-orange-400 transition-colors">Bug Reports</h3>
            <p className="text-sm text-slate-400">Report issues, suggest improvements, or help triage existing bugs.</p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <a
            href="https://github.com/gameguild-gg/website"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-lg hover:from-blue-700 hover:to-purple-700 transition-all duration-300 font-medium shadow-lg"
          >
            <Github className="mr-2 w-4 h-4" />
            View Repository
          </a>
          <a
            href="https://github.com/gameguild-gg/website/issues"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 border border-slate-600 text-slate-300 rounded-lg hover:bg-slate-800 hover:border-slate-500 transition-all duration-300 font-medium"
          >
            <Target className="mr-2 w-4 h-4" />
            Browse Issues
          </a>
          <a
            href="https://github.com/gameguild-gg/website/fork"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 border border-slate-600 text-slate-300 rounded-lg hover:bg-slate-800 hover:border-slate-500 transition-all duration-300 font-medium"
          >
            <GitFork className="mr-2 w-4 h-4" />
            Fork Repository
          </a>
        </div>

        {/* Quick Start Guide */}
        <div className="mt-8 pt-8 border-t border-border">
          <h3 className="text-xl font-bold text-foreground mb-4 text-center">Quick Start Guide</h3>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="text-center">
              <div className="w-8 h-8 bg-primary text-primary-foreground rounded-full flex items-center justify-center mx-auto mb-2 text-sm font-bold">1</div>
              <p className="text-sm font-medium text-foreground">Fork the repo</p>
            </div>
            <div className="text-center">
              <div className="w-8 h-8 bg-primary text-primary-foreground rounded-full flex items-center justify-center mx-auto mb-2 text-sm font-bold">2</div>
              <p className="text-sm font-medium text-foreground">Create a branch</p>
            </div>
            <div className="text-center">
              <div className="w-8 h-8 bg-primary text-primary-foreground rounded-full flex items-center justify-center mx-auto mb-2 text-sm font-bold">3</div>
              <p className="text-sm font-medium text-foreground">Make changes</p>
            </div>
            <div className="text-center">
              <div className="w-8 h-8 bg-primary text-primary-foreground rounded-full flex items-center justify-center mx-auto mb-2 text-sm font-bold">4</div>
              <p className="text-sm font-medium text-foreground">Submit PR</p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
