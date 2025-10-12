'use client';

import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { ChevronDown, Star, Zap, Crown, Skull } from 'lucide-react';
import { CourseLevel, CourseLevelName } from '@/lib/courses/course-enhanced.types';

interface CourseLevelFilterProps {
  selectedLevels: CourseLevel[];
  onToggleLevel: (level: CourseLevel) => void;
}

const levelItems: { value: CourseLevel; label: CourseLevelName }[] = [
  { value: 1, label: 'Beginner' },
  { value: 2, label: 'Intermediate' },
  { value: 3, label: 'Advanced' },
  { value: 4, label: 'Arcane' },
];

export function CourseLevelFilter({ selectedLevels, onToggleLevel }: CourseLevelFilterProps) {
  const getLevelIcon = (level: CourseLevel | 'all') => {
    switch (level) {
      case 1:
        return <Star className="h-4 w-4 text-green-400" />;
      case 2:
        return <Zap className="h-4 w-4 text-yellow-400" />;
      case 3:
        return <Crown className="h-4 w-4 text-orange-400" />;
      case 4:
        return <Skull className="h-4 w-4 text-red-400" />;
      default:
        return <Star className="h-4 w-4 text-slate-400" />;
    }
  };

  const getDisplayText = (selected: CourseLevel[]) => {
    if (selected.length === 0) return 'All Levels';
    if (selected.length === 1) {
      const item = levelItems.find((i) => i.value === selected[0]);
      return item?.label || `Level ${selected[0]}`;
    }
    return `${selected.length} selected`;
  };

  const handleToggleLevel = (level: CourseLevel | 'all') => {
    if (level === 'all') {
      // Clear all selections
      selectedLevels.forEach((l) => onToggleLevel(l));
    } else {
      onToggleLevel(level);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={`${
            selectedLevels.length === 0
              ? 'backdrop-blur-md border border-slate-600/30 text-slate-400'
              : 'backdrop-blur-md border border-orange-400/40 text-white shadow-lg shadow-orange-500/20'
          } rounded-xl px-4 h-10 text-sm focus:outline-none focus:border-orange-400/60 hover:border-orange-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]`}
          style={
            selectedLevels.length === 0
              ? {
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                }
              : {
                  background:
                    'radial-gradient(ellipse 80% 60% at center, rgba(251, 146, 60, 0.4) 0%, rgba(245, 101, 37, 0.3) 50%, rgba(234, 88, 12, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(251, 146, 60, 0.2)',
                }
          }
        >
          <div className="flex items-center gap-2">
            {selectedLevels.length === 0 ? (
              getLevelIcon('all')
            ) : selectedLevels.length === 1 ? (
              getLevelIcon(selectedLevels[0]!)
            ) : (
              <Star className="h-4 w-4 text-slate-400" />
            )}
            <span>{getDisplayText(selectedLevels)}</span>
          </div>
          <ChevronDown className="h-4 w-4 ml-2" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
        <DropdownMenuItem
          onClick={() => handleToggleLevel('all')}
          className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5"
        >
          <div className="flex items-center gap-2">
            <Star className="h-4 w-4 text-slate-400" />
            <span>All Levels</span>
            {selectedLevels.length === 0 && <span className="ml-auto text-orange-400">✓</span>}
          </div>
        </DropdownMenuItem>
        {levelItems.map((item) => (
          <DropdownMenuItem
            key={item.value}
            onClick={() => onToggleLevel(item.value)}
            className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5"
          >
            <div className="flex items-center gap-2">
              {getLevelIcon(item.value)}
              <span>{item.label}</span>
              {selectedLevels.includes(item.value) && <span className="ml-auto text-orange-400">✓</span>}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
