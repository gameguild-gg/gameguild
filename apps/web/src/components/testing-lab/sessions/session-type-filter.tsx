'use client';

import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { ChevronDown, Monitor, Star, Trophy, Users } from 'lucide-react';

interface SessionTypeFilterProps {
  selectedSessionTypes: string[];
  onToggleSessionType: (type: string) => void;
}

const sessionTypeItems = [
  { value: 'gameplay', label: 'Gameplay' },
  { value: 'usability', label: 'Usability' },
  { value: 'bug-testing', label: 'Bug Testing' },
];

export function SessionTypeFilter({ selectedSessionTypes, onToggleSessionType }: SessionTypeFilterProps) {
  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <Monitor className="h-4 w-4 text-blue-400" />;
      case 'usability':
        return <Users className="h-4 w-4 text-purple-400" />;
      case 'bug-testing':
        return <Star className="h-4 w-4 text-orange-400" />;
      default:
        return <Trophy className="h-4 w-4 text-slate-400" />;
    }
  };

  const getDisplayText = (selected: string[]) => {
    if (selected.length === 0) return 'All Types';
    if (selected.length === 1) {
      const item = sessionTypeItems.find((i) => i.value === selected[0]);
      return item?.label || selected[0];
    }
    return `${selected.length} selected`;
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={`${
            selectedSessionTypes.length === 0 ? 'backdrop-blur-md border border-slate-600/30 text-slate-400' : 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
          } rounded-xl px-4 h-10 text-sm focus:outline-none focus:border-blue-400/60 hover:border-blue-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]`}
          style={
            selectedSessionTypes.length === 0
              ? {
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                }
              : {
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                }
          }
        >
          <div className="flex items-center gap-2">
            {selectedSessionTypes.length === 0 ? getSessionTypeIcon('all') : selectedSessionTypes.length === 1 ? getSessionTypeIcon(selectedSessionTypes[0]) : <Trophy className="h-4 w-4 text-slate-400" />}
            <span>{getDisplayText(selectedSessionTypes)}</span>
          </div>
          <ChevronDown className="h-4 w-4 ml-2" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
        <DropdownMenuItem onClick={() => onToggleSessionType('all')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
          <div className="flex items-center gap-2">
            <Trophy className="h-4 w-4 text-slate-400" />
            <span>All Types</span>
            {selectedSessionTypes.length === 0 && <span className="ml-auto text-blue-400">✓</span>}
          </div>
        </DropdownMenuItem>
        {sessionTypeItems.map((item) => (
          <DropdownMenuItem key={item.value} onClick={() => onToggleSessionType(item.value)} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
            <div className="flex items-center gap-2">
              {getSessionTypeIcon(item.value)}
              <span>{item.label}</span>
              {selectedSessionTypes.includes(item.value) && <span className="ml-auto text-blue-400">✓</span>}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
