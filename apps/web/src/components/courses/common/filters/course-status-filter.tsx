'use client';

import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { ChevronDown, FileText, Eye, Archive } from 'lucide-react';
import { ContentStatus as CourseStatus } from '@/lib/api/generated/types.gen';

interface CourseStatusFilterProps {
  selectedStatuses: CourseStatus[];
  onToggleStatus: (status: CourseStatus) => void;
}

const statusItems: { value: CourseStatus; label: string }[] = [
  { value: CourseStatus.DRAFT, label: 'Draft' },
  { value: CourseStatus.PUBLISHED, label: 'Published' },
  { value: CourseStatus.ARCHIVED, label: 'Archived' },
];

export function CourseStatusFilter({ selectedStatuses, onToggleStatus }: CourseStatusFilterProps) {
  const getStatusIcon = (status: CourseStatus | 'all') => {
    switch (status) {
      case CourseStatus.DRAFT:
        return <FileText className="h-4 w-4 text-yellow-400" />;
      case CourseStatus.PUBLISHED:
        return <Eye className="h-4 w-4 text-green-400" />;
      case CourseStatus.ARCHIVED:
        return <Archive className="h-4 w-4 text-red-400" />;
      default:
        return <FileText className="h-4 w-4 text-slate-400" />;
    }
  };

  const getDisplayText = (selected: CourseStatus[]) => {
    if (selected.length === 0) return 'All Status';
    if (selected.length === 1) {
      const item = statusItems.find((i) => i.value === selected[0]);
      return item?.label || 'Status';
    }
    return `${selected.length} selected`;
  };

  const handleToggleStatus = (status: CourseStatus | 'all') => {
    if (status === 'all') {
      // Clear all selections
      selectedStatuses.forEach((s) => onToggleStatus(s));
    } else {
      onToggleStatus(status);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={`${
            selectedStatuses.length === 0 ? 'backdrop-blur-md border border-slate-600/30 text-slate-400' : 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
          } rounded-xl px-4 h-10 text-sm focus:outline-none focus:border-blue-400/60 hover:border-blue-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]`}
          style={
            selectedStatuses.length === 0
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
            {selectedStatuses.length === 0 ? getStatusIcon('all') : selectedStatuses.length === 1 ? getStatusIcon(selectedStatuses[0]!) : <FileText className="h-4 w-4 text-slate-400" />}
            <span>{getDisplayText(selectedStatuses)}</span>
          </div>
          <ChevronDown className="h-4 w-4 ml-2" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
  <DropdownMenuItem onClick={() => handleToggleStatus('all')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
          <div className="flex items-center gap-2">
            <FileText className="h-4 w-4 text-slate-400" />
            <span>All Status</span>
            {selectedStatuses.length === 0 && <span className="ml-auto text-blue-400">✓</span>}
          </div>
        </DropdownMenuItem>
        {statusItems.map((item) => (
          <DropdownMenuItem key={item.value} onClick={() => onToggleStatus(item.value)} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
            <div className="flex items-center gap-2">
              {getStatusIcon(item.value)}
              <span>{item.label}</span>
              {selectedStatuses.includes(item.value) && <span className="ml-auto text-blue-400">✓</span>}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
