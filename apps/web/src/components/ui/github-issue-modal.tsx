'use client';

import { useEffect, useState } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Github, Star, MessageSquare, Plus } from 'lucide-react';
import { usePathname } from 'next/navigation';

interface GitHubIssueModalProps {
  isOpen: boolean;
  onClose: () => void;
  route?: string;
  githubIssue?: string;
}

export function GitHubIssueModal({ isOpen, onClose, route, githubIssue }: GitHubIssueModalProps) {
  const pathname = usePathname();
  const currentRoute = route || pathname;

  const createBugReportUrl = () => {
    const params = new URLSearchParams({
      template: 'bug_report.yml',
      title: `[Bug]: Issue on ${currentRoute}`,
      'route_url': `https://gameguild.gg${currentRoute}`
    });
    return `https://github.com/gameguild-gg/gameguild/issues/new?${params.toString()}`;
  };

  const createFeatureRequestUrl = () => {
    const params = new URLSearchParams({
      template: 'feature_request.yml',
      title: `[Enhancement]: Feature request for ${currentRoute}`,
      'route_url': `https://gameguild.gg${currentRoute}`
    });
    return `https://github.com/gameguild-gg/gameguild/issues/new?${params.toString()}`;
  };

  const getIssueUrl = () => {
    if (githubIssue) {
      // If it's a full URL, use it directly, otherwise construct it
      if (githubIssue.startsWith('http')) {
        return githubIssue;
      }
      return `https://github.com/gameguild-gg/gameguild/issues/${githubIssue}`;
    }
    return 'https://github.com/gameguild-gg/gameguild/issues';
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-md bg-slate-800/95 border-slate-600/50 backdrop-blur-xl">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold text-white flex items-center gap-2">
            <Github className="h-6 w-6 text-blue-400" />
            {githubIssue ? 'Help Us Improve!' : 'Help Us Build Better!'}
          </DialogTitle>
          <DialogDescription className="text-slate-300 text-base leading-relaxed">
            {githubIssue 
              ? 'We\'re working on this feature. Your feedback and contributions can help us build it faster!'
              : 'We\'re working hard to bring you an amazing experience. Help us build this feature faster by getting involved!'
            }
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-4 mt-6">
          <div className="text-sm text-slate-400 font-medium">How you can help:</div>
          
          <div className="space-y-3">
            {githubIssue ? (
              <Button 
                variant="outline" 
                className="w-full justify-start bg-slate-700/50 border-slate-600 hover:bg-slate-600/50 text-white"
                onClick={() => window.open(getIssueUrl(), '_blank')}
              >
                <MessageSquare className="h-4 w-4 mr-2 text-green-400" />
                Comment on this issue
              </Button>
            ) : (
              <>
                <Button 
                  variant="outline" 
                  className="w-full justify-start bg-slate-700/50 border-slate-600 hover:bg-slate-600/50 text-white"
                  onClick={() => {
                    const baseUrl = 'https://github.com/gameguild-gg/gameguild/issues';
                    const searchUrl = currentRoute 
                      ? `${baseUrl}?q=${encodeURIComponent(currentRoute)}`
                      : baseUrl;
                    window.open(searchUrl, '_blank');
                  }}
                >
                  <MessageSquare className="h-4 w-4 mr-2 text-green-400" />
                  Check issues
                </Button>
                
                <Button 
                  variant="outline" 
                  className="w-full justify-start bg-slate-700/50 border-slate-600 hover:bg-slate-600/50 text-white"
                  onClick={() => window.open(createBugReportUrl(), '_blank')}
                >
                  <Plus className="h-4 w-4 mr-2 text-red-400" />
                  Report a bug
                </Button>
                
                <Button 
                  variant="outline" 
                  className="w-full justify-start bg-slate-700/50 border-slate-600 hover:bg-slate-600/50 text-white"
                  onClick={() => window.open(createFeatureRequestUrl(), '_blank')}
                >
                  <Plus className="h-4 w-4 mr-2 text-blue-400" />
                  Request a feature
                </Button>
              </>
            )}
            
            <Button 
              variant="outline" 
              className="w-full justify-start bg-gradient-to-r from-yellow-600/20 to-orange-600/20 border-yellow-500/30 hover:from-yellow-500/30 hover:to-orange-500/30 text-yellow-200"
              onClick={() => window.open('https://github.com/gameguild-gg/gameguild', '_blank')}
            >
              <Star className="h-4 w-4 mr-2 text-yellow-400" />
              Give us a star on GitHub
            </Button>
          </div>
          
          <div className="pt-4 border-t border-slate-600/30">
            <Button 
              onClick={onClose}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white"
            >
              Got it, thanks!
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}