'use client';

import { useFilterContext } from './filter-context';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { LayoutGrid, List, Table } from 'lucide-react';

interface ContextViewModeToggleProps {
  className?: string;
}

/**
 * ViewModeToggle component that automatically connects to the filter context
 * Use this when you have a FilterProvider in your component tree
 */
export function ContextViewModeToggle({ className = '' }: ContextViewModeToggleProps) {
  const { state, setViewMode } = useFilterContext();

  const viewModes = [
    { mode: 'cards' as const, icon: LayoutGrid, label: 'Cards View' },
    { mode: 'row' as const, icon: List, label: 'Row View' },
    { mode: 'table' as const, icon: Table, label: 'Table View' },
  ];

  return (
    <TooltipProvider>
      <div className={`flex rounded-lg border border-gray-300 bg-white ${className}`}>
        {viewModes.map(({ mode, icon: Icon, label }) => (
          <Tooltip key={mode}>
            <TooltipTrigger asChild>
              <button
                type="button"
                onClick={() => setViewMode(mode)}
                className={`flex items-center justify-center px-3 py-2 text-sm font-medium transition-colors ${
                  state.viewMode === mode
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-700 hover:bg-gray-50'
                } ${
                  mode === 'cards' ? 'rounded-l-lg' : mode === 'table' ? 'rounded-r-lg' : ''
                }`}
              >
                <Icon className="h-4 w-4" />
              </button>
            </TooltipTrigger>
            <TooltipContent>
              <p>{label}</p>
            </TooltipContent>
          </Tooltip>
        ))}
      </div>
    </TooltipProvider>
  );
}
