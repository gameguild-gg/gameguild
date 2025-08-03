'use client';

import { SessionSearchBar } from './session-search-bar';
import { SessionStatusFilter } from './session-status-filter';
import { SessionTypeFilter } from './session-type-filter';
import { SessionViewToggle } from './session-view-toggle';
import { PeriodSelector } from '@/components/common/filters/period-selector';

interface SessionFilterControlsProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  selectedStatuses: string[];
  onToggleStatus: (status: string) => void;
  selectedSessionTypes: string[];
  onToggleSessionType: (type: string) => void;
  selectedPeriod: string;
  onPeriodChange: (period: string) => void;
  viewMode: 'cards' | 'row' | 'table';
  onViewModeChange: (mode: 'cards' | 'row' | 'table') => void;
  hideViewToggle?: boolean;
}

export function SessionFilterControls({
  searchTerm,
  onSearchChange,
  selectedStatuses,
  onToggleStatus,
  selectedSessionTypes,
  onToggleSessionType,
  selectedPeriod,
  onPeriodChange,
  viewMode,
  onViewModeChange,
  hideViewToggle = false,
}: SessionFilterControlsProps) {
  return (
    <div className="space-y-4">
      {/* Desktop Layout (xl and up) - Single Row */}
      <div className="hidden xl:flex xl:items-center xl:justify-between xl:gap-6">
        {/* Left Side - Search Bar */}
        <div className="flex-1 max-w-md">
          <SessionSearchBar searchTerm={searchTerm} onSearchChange={onSearchChange} />
        </div>

        {/* Center - Filter Dropdowns */}
        <div className="flex gap-3 items-center">
          <SessionStatusFilter selectedStatuses={selectedStatuses} onToggleStatus={onToggleStatus} />
          <SessionTypeFilter selectedSessionTypes={selectedSessionTypes} onToggleSessionType={onToggleSessionType} />
        </div>

        {/* Right Side - Period Selector and View Mode Toggle */}
        <div className="flex items-center gap-4">
          <PeriodSelector selectedPeriod={selectedPeriod} onPeriodChange={onPeriodChange} />
          {!hideViewToggle && <SessionViewToggle viewMode={viewMode} onViewModeChange={onViewModeChange} />}
        </div>
      </div>

      {/* Tablet/Mobile Layout (lg and below) - Two Rows */}
      <div className="xl:hidden space-y-4">
        {/* First Row - Period Selector and View Mode Toggle */}
        <div className="flex items-center justify-between gap-4">
          <div className={`${hideViewToggle ? 'flex-1' : 'flex-1 max-w-xs'}`}>
            <PeriodSelector selectedPeriod={selectedPeriod} onPeriodChange={onPeriodChange} />
          </div>
          {!hideViewToggle && <SessionViewToggle viewMode={viewMode} onViewModeChange={onViewModeChange} />}
        </div>

        {/* Second Row - Search and Dropdown Filters */}
        <div className="flex items-center gap-4">
          {/* Search Bar - Full width on mobile, flex-1 on sm+ */}
          <div className="flex-1 min-w-0">
            <SessionSearchBar searchTerm={searchTerm} onSearchChange={onSearchChange} />
          </div>

          {/* Filter Dropdowns */}
          <div className="flex gap-3 items-center flex-shrink-0">
            <SessionStatusFilter selectedStatuses={selectedStatuses} onToggleStatus={onToggleStatus} />
            <SessionTypeFilter selectedSessionTypes={selectedSessionTypes} onToggleSessionType={onToggleSessionType} />
          </div>
        </div>
      </div>
    </div>
  );
}
