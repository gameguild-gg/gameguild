'use client';

import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { LayoutGrid, List, Rows } from 'lucide-react';

interface ViewModeToggleProps {
  viewMode: 'cards' | 'row' | 'table';
  onViewModeChange: (mode: 'cards' | 'row' | 'table') => void;
  className?: string;
}

export function ViewModeToggle({ viewMode, onViewModeChange, className = '' }: ViewModeToggleProps) {
  const getButtonStyle = (mode: 'cards' | 'row' | 'table', isActive: boolean) => {
    const baseClasses = 'transition-all duration-200 h-10 px-3';
    const borderClasses =
      mode === 'cards' ? 'rounded-l-xl rounded-r-none border-r-0' : mode === 'row' ? 'rounded-none border-x-0' : 'rounded-r-xl rounded-l-none border-l-0';

    const stateClasses = isActive
      ? mode === 'row'
        ? 'backdrop-blur-md border border-purple-400/40 text-white shadow-lg shadow-purple-500/20'
        : 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
      : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50';

    return `${baseClasses} ${borderClasses} ${stateClasses}`;
  };

  const getButtonBackgroundStyle = (mode: 'cards' | 'row' | 'table', isActive: boolean) => {
    if (isActive) {
      if (mode === 'row') {
        return {
          background: 'radial-gradient(ellipse 80% 60% at center, rgba(147, 51, 234, 0.4) 0%, rgba(126, 34, 206, 0.3) 50%, rgba(107, 33, 168, 0.2) 100%)',
          boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(147, 51, 234, 0.2)',
        };
      }
      return {
        background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
        boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
      };
    }
    return {
      background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
    };
  };

  return (
    <div className={`flex ${className}`}>
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onViewModeChange('cards')}
              className={getButtonStyle('cards', viewMode === 'cards')}
              style={getButtonBackgroundStyle('cards', viewMode === 'cards')}
            >
              <LayoutGrid className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Card view</p>
          </TooltipContent>
        </Tooltip>

        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onViewModeChange('row')}
              className={getButtonStyle('row', viewMode === 'row')}
              style={getButtonBackgroundStyle('row', viewMode === 'row')}
            >
              <Rows className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Row view</p>
          </TooltipContent>
        </Tooltip>

        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onViewModeChange('table')}
              className={getButtonStyle('table', viewMode === 'table')}
              style={getButtonBackgroundStyle('table', viewMode === 'table')}
            >
              <List className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Table view</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </div>
  );
}
