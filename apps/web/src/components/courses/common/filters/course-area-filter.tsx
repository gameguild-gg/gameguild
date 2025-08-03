'use client';

import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { ChevronDown, Code, Palette, Layers, Headphones } from 'lucide-react';
import { CourseArea } from '@/lib/courses/course-enhanced.types';

interface CourseAreaFilterProps {
  selectedAreas: CourseArea[];
  onToggleArea: (area: CourseArea) => void;
}

const areaItems: { value: CourseArea; label: string }[] = [
  { value: 'programming', label: 'Programming' },
  { value: 'art', label: 'Art' },
  { value: 'design', label: 'Design' },
  { value: 'audio', label: 'Audio' },
];

export function CourseAreaFilter({ selectedAreas, onToggleArea }: CourseAreaFilterProps) {
  const getAreaIcon = (area: CourseArea | 'all') => {
    switch (area) {
      case 'programming':
        return <Code className="h-4 w-4 text-blue-400" />;
      case 'art':
        return <Palette className="h-4 w-4 text-purple-400" />;
      case 'design':
        return <Layers className="h-4 w-4 text-green-400" />;
      case 'audio':
        return <Headphones className="h-4 w-4 text-yellow-400" />;
      default:
        return <Code className="h-4 w-4 text-slate-400" />;
    }
  };

  const getDisplayText = (selected: CourseArea[]) => {
    if (selected.length === 0) return 'All Areas';
    if (selected.length === 1) {
      const item = areaItems.find((i) => i.value === selected[0]);
      return item?.label || selected[0];
    }
    return `${selected.length} selected`;
  };

  const handleToggleArea = (area: CourseArea | 'all') => {
    if (area === 'all') {
      // Clear all selections
      selectedAreas.forEach((a) => onToggleArea(a));
    } else {
      onToggleArea(area);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={`${
            selectedAreas.length === 0 ? 'backdrop-blur-md border border-slate-600/30 text-slate-400' : 'backdrop-blur-md border border-purple-400/40 text-white shadow-lg shadow-purple-500/20'
          } rounded-xl px-4 h-10 text-sm focus:outline-none focus:border-purple-400/60 hover:border-purple-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]`}
          style={
            selectedAreas.length === 0
              ? {
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                }
              : {
                  background: 'radial-gradient(ellipse 80% 60% at center, rgba(147, 51, 234, 0.4) 0%, rgba(126, 34, 206, 0.3) 50%, rgba(107, 33, 168, 0.2) 100%)',
                  boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(147, 51, 234, 0.2)',
                }
          }
        >
          <div className="flex items-center gap-2">
            {selectedAreas.length === 0 ? getAreaIcon('all') : selectedAreas.length === 1 ? getAreaIcon(selectedAreas[0]!) : <Code className="h-4 w-4 text-slate-400" />}
            <span>{getDisplayText(selectedAreas)}</span>
          </div>
          <ChevronDown className="h-4 w-4 ml-2" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
        <DropdownMenuItem onClick={() => handleToggleArea('all')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
          <div className="flex items-center gap-2">
            <Code className="h-4 w-4 text-slate-400" />
            <span>All Areas</span>
            {selectedAreas.length === 0 && <span className="ml-auto text-purple-400">✓</span>}
          </div>
        </DropdownMenuItem>
        {areaItems.map((item) => (
          <DropdownMenuItem key={item.value} onClick={() => onToggleArea(item.value)} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
            <div className="flex items-center gap-2">
              {getAreaIcon(item.value)}
              <span>{item.label}</span>
              {selectedAreas.includes(item.value) && <span className="ml-auto text-purple-400">✓</span>}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
