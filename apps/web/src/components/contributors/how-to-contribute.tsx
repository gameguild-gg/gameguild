import React from 'react';

export function HowToContribute() {
  return (
    <section id="contributing-section" className="mb-12">
      <div className="bg-card rounded-lg border border-border p-8">
        <div className="text-center mb-8">
          <h2 className="text-3xl font-bold text-foreground mb-4">How to Contribute</h2>
          <p className="text-muted-foreground text-lg max-w-3xl mx-auto">
            Whether you&apos;re a seasoned developer or just starting out, there are many ways to contribute to Game Guild. Every contribution helps make our
            platform better for the gaming community.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          {/* Code Contributions */}
          <div className="bg-muted/30 rounded-lg p-6 text-center">
            <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl">💻</span>
            </div>
            <h3 className="text-lg font-bold text-foreground mb-2">Code Contributions</h3>
            <p className="text-sm text-muted-foreground">Submit bug fixes, implement new features, or improve existing functionality.</p>
          </div>

          {/* Documentation */}
          <div className="bg-muted/30 rounded-lg p-6 text-center">
            <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl">📚</span>
            </div>
            <h3 className="text-lg font-bold text-foreground mb-2">Documentation</h3>
            <p className="text-sm text-muted-foreground">Help improve our docs, write tutorials, or translate content.</p>
          </div>

          {/* Bug Reports */}
          <div className="bg-muted/30 rounded-lg p-6 text-center">
            <div className="w-12 h-12 bg-primary/10 rounded-lg flex items-center justify-center mx-auto mb-4">
              <span className="text-2xl">🐛</span>
            </div>
            <h3 className="text-lg font-bold text-foreground mb-2">Bug Reports</h3>
            <p className="text-sm text-muted-foreground">Report issues, suggest improvements, or help triage existing bugs.</p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <a
            href="https://github.com/gameguild-gg/website"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors font-medium"
          >
            <span className="mr-2">📦</span>
            View Repository
          </a>
          <a
            href="https://github.com/gameguild-gg/website/issues"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 border border-border text-foreground rounded-md hover:bg-muted transition-colors font-medium"
          >
            <span className="mr-2">🎯</span>
            Browse Issues
          </a>
          <a
            href="https://github.com/gameguild-gg/website/fork"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center px-6 py-3 border border-border text-foreground rounded-md hover:bg-muted transition-colors font-medium"
          >
            <span className="mr-2">🍴</span>
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
