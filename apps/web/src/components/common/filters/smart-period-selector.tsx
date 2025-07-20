'use client';

import * as React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';

// Type-safe period configuration
export interface PeriodConfig {
  type: 'day' | 'week' | 'month' | 'quarter' | 'year';
  label: string;
  tooltip: string;
  shortLabel: string;
}

export interface PeriodValue {
  type: 'day' | 'week' | 'month' | 'quarter' | 'year';
  value: string;
  label: string;
  year?: string;
  date?: Date;
}

interface SmartPeriodSelectorProps {
  selectedPeriod: string;
  onPeriodChange?: (period: string) => void;
  onPeriodValueChange?: (value: PeriodValue) => void;
  className?: string;
  showNavigation?: boolean;
  maxVisible?: number;
}

// Default period configurations
const DEFAULT_PERIODS: PeriodConfig[] = [
  { type: 'day', label: 'Day', tooltip: 'Daily view', shortLabel: 'D' },
  { type: 'week', label: 'Week', tooltip: 'Weekly view', shortLabel: 'W' },
  { type: 'month', label: 'Month', tooltip: 'Monthly view', shortLabel: 'M' },
  { type: 'quarter', label: 'Quarter', tooltip: 'Quarterly view', shortLabel: 'Q' },
  { type: 'year', label: 'Year', tooltip: 'Yearly view', shortLabel: 'Y' },
];

export function SmartPeriodSelector({
  selectedPeriod,
  onPeriodChange,
  onPeriodValueChange,
  className,
  showNavigation = true,
  maxVisible = 3,
}: SmartPeriodSelectorProps) {
  const currentDate = new Date();
  const [currentOffset, setCurrentOffset] = React.useState(0);

  // Generate period values based on current selection and offset
  const periodValues = React.useMemo(() => {
    const selectedConfig = DEFAULT_PERIODS.find((p) => p.type === selectedPeriod) || DEFAULT_PERIODS[2]; // Default to month

    return generatePeriodValues(selectedConfig.type, currentOffset, maxVisible);
  }, [selectedPeriod, currentOffset, maxVisible]);

  const selectedConfig = DEFAULT_PERIODS.find((p) => p.type === selectedPeriod) || DEFAULT_PERIODS[2];

  const handleNext = () => {
    setCurrentOffset((prev) => prev + 1);
  };

  const handlePrev = () => {
    setCurrentOffset((prev) => prev - 1);
  };

  const handlePeriodTypeChange = (newPeriod: string) => {
    setCurrentOffset(0); // Reset offset when changing period type
    onPeriodChange?.(newPeriod);
  };

  const handlePeriodValueSelect = (value: PeriodValue) => {
    onPeriodValueChange?.(value);
  };

  return (
    <div className={cn('flex items-center gap-2 min-w-0', className)}>
      {/* Period Values Navigation */}
      {showNavigation && (
        <div className="flex items-center gap-2 flex-1">
          <PeriodValueRow values={periodValues} onNext={handleNext} onPrev={handlePrev} onSelect={handlePeriodValueSelect} />
        </div>
      )}

      {/* Period Type Selector */}
      <div className="flex flex-shrink-0">
        <TooltipProvider>
          {DEFAULT_PERIODS.map((period, index) => (
            <Tooltip key={period.type}>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="default"
                  className={`${
                    selectedPeriod === period.type
                      ? 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
                      : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
                  } transition-all duration-200 h-10 w-10 p-0 ${
                    index === 0
                      ? 'rounded-l-xl rounded-r-none border-r-0'
                      : index === DEFAULT_PERIODS.length - 1
                        ? 'rounded-r-xl rounded-l-none border-l-0'
                        : 'rounded-none border-x-0'
                  }`}
                  style={
                    selectedPeriod === period.type
                      ? {
                          background:
                            'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                          boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                        }
                      : {
                          background:
                            'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                          boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                        }
                  }
                  onClick={() => handlePeriodTypeChange(period.type)}
                >
                  {period.shortLabel}
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>{period.tooltip}</p>
              </TooltipContent>
            </Tooltip>
          ))}
        </TooltipProvider>
      </div>
    </div>
  );
}

interface PeriodValueRowProps {
  values: PeriodValue[];
  onNext: () => void;
  onPrev: () => void;
  onSelect: (value: PeriodValue) => void;
}

function PeriodValueRow({ values, onNext, onPrev, onSelect }: PeriodValueRowProps) {
  const [activeIndex, setActiveIndex] = React.useState(0);

  // Reset active index when values change
  React.useEffect(() => {
    setActiveIndex(0);
  }, [values]);

  const handleNext = () => {
    if (activeIndex < values.length - 1) {
      setActiveIndex(activeIndex + 1);
    } else {
      onNext();
    }
  };

  const handlePrev = () => {
    if (activeIndex > 0) {
      setActiveIndex(activeIndex - 1);
    } else {
      onPrev();
      React.startTransition(() => {
        setActiveIndex(Math.max(0, values.length - 1));
      });
    }
  };

  const handleSelect = (index: number) => {
    setActiveIndex(index);
    onSelect(values[index]);
  };

  return (
    <div className="flex items-center gap-2 flex-1">
      <div className="flex">
        {/* Previous Button */}
        <Button
          variant="ghost"
          size="default"
          className="backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-l-xl rounded-r-none border-r-0"
          style={{
            background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
            boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
          }}
          onClick={handlePrev}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>

        {/* Period Values */}
        {values.map((value, index) => {
          const isFirst = index === 0;
          const isLast = index === values.length - 1;
          const isActive = index === activeIndex;

          return (
            <Button
              key={`${value.type}-${value.value}-${value.year || ''}`}
              variant="ghost"
              size="default"
              className={`${
                isActive
                  ? 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
                  : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
              } transition-all duration-200 flex h-10 w-20 flex-col p-0 items-center justify-center gap-0.5 ${
                isFirst && isLast
                  ? 'rounded-none border-x-0'
                  : isFirst
                    ? 'rounded-none border-r-0'
                    : isLast
                      ? 'rounded-none border-l-0'
                      : 'rounded-none border-x-0'
              }`}
              style={
                isActive
                  ? {
                      background:
                        'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                    }
                  : {
                      background:
                        'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                    }
              }
              onClick={() => handleSelect(index)}
            >
              {value.year && <span className={`text-[9px] truncate leading-none ${isActive ? 'text-white' : 'text-slate-500'}`}>{value.year}</span>}
              <span className="text-sm font-medium truncate leading-none">{value.label}</span>
            </Button>
          );
        })}

        {/* Next Button */}
        <Button
          variant="ghost"
          size="default"
          className="backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-r-xl rounded-l-none border-l-0"
          style={{
            background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
            boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
          }}
          onClick={handleNext}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

// Utility function to generate period values
function generatePeriodValues(type: PeriodValue['type'], offset: number, count: number): PeriodValue[] {
  const currentDate = new Date();
  const values: PeriodValue[] = [];

  for (let i = 0; i < count; i++) {
    const index = offset + i;
    let value: PeriodValue;

    switch (type) {
      case 'day': {
        const date = new Date(currentDate);
        date.setDate(date.getDate() + index);
        value = {
          type: 'day',
          value: date.toISOString().split('T')[0],
          label: date.toLocaleDateString('en-US', { weekday: 'short', day: 'numeric' }),
          year: date.toLocaleDateString('en-US', { month: 'short' }),
          date,
        };
        break;
      }
      case 'week': {
        const weekNumber = getCurrentWeek(currentDate) + index;
        const year = currentDate.getFullYear();
        value = {
          type: 'week',
          value: `${year}-W${weekNumber.toString().padStart(2, '0')}`,
          label: `Week ${weekNumber}`,
          year: year.toString(),
        };
        break;
      }
      case 'month': {
        const date = new Date(currentDate);
        date.setMonth(date.getMonth() + index);
        value = {
          type: 'month',
          value: `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}`,
          label: date.toLocaleDateString('en-US', { month: 'short' }),
          year: date.getFullYear().toString(),
          date,
        };
        break;
      }
      case 'quarter': {
        const currentQuarter = Math.floor(currentDate.getMonth() / 3) + 1;
        const quarterIndex = currentQuarter + index;
        const year = currentDate.getFullYear() + Math.floor((quarterIndex - 1) / 4);
        const quarter = ((quarterIndex - 1) % 4) + 1;
        value = {
          type: 'quarter',
          value: `${year}-Q${quarter}`,
          label: `Q${quarter}`,
          year: year.toString(),
        };
        break;
      }
      case 'year': {
        const year = currentDate.getFullYear() + index;
        value = {
          type: 'year',
          value: year.toString(),
          label: year.toString(),
        };
        break;
      }
      default:
        throw new Error(`Unsupported period type: ${type}`);
    }

    values.push(value);
  }

  return values;
}

// Utility function to get current week number
function getCurrentWeek(date: Date): number {
  const firstDayOfYear = new Date(date.getFullYear(), 0, 1);
  const pastDaysOfYear = (date.getTime() - firstDayOfYear.getTime()) / 86400000;
  return Math.ceil((pastDaysOfYear + firstDayOfYear.getDay() + 1) / 7);
}

// Export the legacy component for backward compatibility
export { PeriodSelector } from '../../filters/period-selector';
