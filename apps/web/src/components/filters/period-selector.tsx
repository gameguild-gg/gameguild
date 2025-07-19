'use client';

import * as React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';

interface PeriodOption {
  label: string;
  value: string;
  year?: string;
  selected?: boolean;
}

interface PeriodSelectorProps {
  startDate?: string;
  endDate?: string;
  selectedPeriod: string;
  onPeriodChange?: (period: string) => void;
  onDateRangeChange?: (startDate: string, endDate: string) => void;
  className?: string;
}

export function PeriodSelector({
  startDate = '24-04-2023',
  endDate = '01-01-2024',
  selectedPeriod,
  onPeriodChange,
  onDateRangeChange,
  className,
}: PeriodSelectorProps) {
  const [currentWeekStart, setCurrentWeekStart] = React.useState(32);
  const [currentMonthStart, setCurrentMonthStart] = React.useState(0);
  const [currentYearStart, setCurrentYearStart] = React.useState(2023);
  const [currentDayStart, setCurrentDayStart] = React.useState(new Date(2023, 3, 24));

  const periods = [
    { label: 'W', value: 'week', tooltip: 'Week' },
    { label: 'M', value: 'month', tooltip: 'Month' },
    { label: 'Q', value: 'quarter', tooltip: 'Quarter' },
    { label: 'Y', value: 'year', tooltip: 'Year' },
  ];

  const generateWeeks = (start: number): PeriodOption[] => {
    return Array.from({ length: 3 }, (_, index) => {
      const weekNumber = start + index;
      return {
        label: `Week ${weekNumber}`,
        value: weekNumber.toString(),
        year: '2023',
        selected: index === 0,
      };
    });
  };

  const allMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  const generateMonths = (start: number): PeriodOption[] => {
    return Array.from({ length: 3 }, (_, index) => {
      const monthIndex = (start + index) % 12;
      const year = 2023 + Math.floor((start + index) / 12);
      return {
        label: allMonths[monthIndex],
        value: (monthIndex + 1).toString().padStart(2, '0'),
        year: year.toString(),
        selected: index === 0,
      };
    });
  };

  const generateYears = (start: number): PeriodOption[] => {
    return Array.from({ length: 3 }, (_, index) => {
      const year = start + index;
      return {
        label: year.toString(),
        value: year.toString(),
        selected: index === 0,
      };
    });
  };

  const generateDays = (start: Date): PeriodOption[] => {
    return Array.from({ length: 3 }, (_, index) => {
      const day = new Date(start);
      day.setDate(day.getDate() + index);
      return {
        label: day.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }),
        value: day.toISOString(),
        year: day.toLocaleDateString('en-US', { weekday: 'long' }),
        selected: index === 0,
      };
    });
  };

  const generateQuarters = (startYear: number): PeriodOption[] => {
    const quarters = [];
    for (let yearOffset = 0; yearOffset < 2; yearOffset++) {
      const year = startYear + yearOffset;
      for (let q = 1; q <= 4; q++) {
        quarters.push({
          label: `Q${q}`,
          value: `Q${q}`,
          year: year.toString(),
          selected: yearOffset === 0 && q === 1,
        });
      }
    }
    return quarters.slice(0, 6); // Show 6 quarters (1.5 years worth)
  };

  const weeks = React.useMemo(() => generateWeeks(currentWeekStart), [currentWeekStart]);
  const months = React.useMemo(() => generateMonths(currentMonthStart), [currentMonthStart]);
  const years = React.useMemo(() => generateYears(currentYearStart), [currentYearStart]);
  const days = React.useMemo(() => generateDays(currentDayStart), [currentDayStart]);
  const quarters = React.useMemo(() => generateQuarters(currentYearStart), [currentYearStart]);

  const handleWeekChange = (direction: 'next' | 'prev') => {
    setCurrentWeekStart((prevStart) => {
      if (direction === 'next') {
        return prevStart + 1;
      } else {
        return Math.max(1, prevStart - 1);
      }
    });
  };

  const handleMonthChange = (direction: 'next' | 'prev') => {
    setCurrentMonthStart((prevStart) => {
      if (direction === 'next') {
        return prevStart + 1;
      } else {
        return Math.max(0, prevStart - 1);
      }
    });
  };

  const handleYearChange = (direction: 'next' | 'prev') => {
    setCurrentYearStart((prevStart) => {
      if (direction === 'next') {
        return prevStart + 1;
      } else {
        return prevStart - 1;
      }
    });
  };

  const handleQuarterChange = (direction: 'next' | 'prev') => {
    setCurrentYearStart((prevStart) => {
      if (direction === 'next') {
        return prevStart + 1;
      } else {
        return prevStart - 1;
      }
    });
  };

  const handleDayChange = (direction: 'next' | 'prev') => {
    setCurrentDayStart((prevStart) => {
      const newDate = new Date(prevStart);
      if (direction === 'next') {
        newDate.setDate(newDate.getDate() + 1);
      } else {
        newDate.setDate(newDate.getDate() - 1);
      }
      return newDate;
    });
  };

  return (
    <div className={cn('flex items-center gap-2 min-w-0', className)}>
      {/* Period Range Selector - First - Horizontal Layout with overflow control */}
      <div className="flex items-center gap-2 flex-1 flex-wrap">
        {/* Navigation and Range Options in Single Row */}
        {selectedPeriod === 'week' && <PeriodRow options={weeks} onNext={() => handleWeekChange('next')} onPrev={() => handleWeekChange('prev')} />}
        {selectedPeriod === 'month' && <PeriodRow options={months} onNext={() => handleMonthChange('next')} onPrev={() => handleMonthChange('prev')} />}
        {selectedPeriod === 'quarter' && <PeriodRow options={quarters} onNext={() => handleQuarterChange('next')} onPrev={() => handleQuarterChange('prev')} />}
        {selectedPeriod === 'year' && <PeriodRow options={years} onNext={() => handleYearChange('next')} onPrev={() => handleYearChange('prev')} />}
        {selectedPeriod === 'day' && <PeriodRow options={days} onNext={() => handleDayChange('next')} onPrev={() => handleDayChange('prev')} />}
      </div>

      {/* Period Type Selector - Second - Match toggle button style */}
      <div className="flex flex-wrap flex-shrink-0">
        <TooltipProvider>
          {periods.map((period, index) => (
            <Tooltip key={period.value}>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="default"
                  className={`${
                    selectedPeriod === period.value
                      ? 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
                      : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
                  } transition-all duration-200 h-10 w-10 p-0 ${
                    index === 0
                      ? 'rounded-l-xl rounded-r-none border-r-0'
                      : index === periods.length - 1
                        ? 'rounded-r-xl rounded-l-none border-l-0'
                        : 'rounded-none border-x-0'
                  }`}
                  style={
                    selectedPeriod === period.value
                      ? {
                          background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                          boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                        }
                      : {
                          background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                          boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                        }
                  }
                  onClick={() => {
                    onPeriodChange?.(period.value);
                  }}
                >
                  {period.label}
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

function PeriodRow({ options, onNext, onPrev }: { options: PeriodOption[]; onNext?: () => void; onPrev?: () => void }) {
  const [activeIndex, setActiveIndex] = React.useState(0);

  const handleNext = () => {
    if (activeIndex < options.length - 1) {
      setActiveIndex(activeIndex + 1);
    } else if (onNext) {
      onNext();
      setActiveIndex(0);
    }
  };

  const handlePrev = () => {
    if (activeIndex > 0) {
      setActiveIndex(activeIndex - 1);
    } else if (onPrev) {
      onPrev();
      setActiveIndex(options.length - 1);
    }
  };

  // Calculate visible options (show 3 at a time)
  const visibleOptions = React.useMemo(() => {
    // If active index is at the beginning, show first 3
    if (activeIndex <= 0) return options.slice(0, 3);
    // If active index is near the end, show last 3
    if (activeIndex >= options.length - 1) return options.slice(-3);
    // Otherwise show the active one with 1 on each side
    return options.slice(Math.max(0, activeIndex - 1), Math.min(options.length, activeIndex + 2));
  }, [activeIndex, options]);

  return (
    <div className="flex items-center gap-2 flex-1">
      {/* Connected Range Navigation and Options - Match toggle button style */}
      <div className="flex">
        {/* Previous Navigation Button */}
        {onPrev && (
          <Button
            variant="ghost"
            size="default"
            className="bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-l-xl rounded-r-none border-r-0"
            onClick={handlePrev}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
        )}

        {/* Range Options - Connected */}
        {visibleOptions.map((option, index) => {
          const optionIndex = options.indexOf(option);
          const isFirst = index === 0 && !onPrev;
          const isLast = index === visibleOptions.length - 1 && !onNext;
          const isOnlyButton = visibleOptions.length === 1 && !onPrev && !onNext;
          
          return (
            <Button
              key={optionIndex}
              variant="ghost"
              size="default"
              className={`${
                optionIndex === activeIndex
                  ? 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20'
                  : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
              } transition-all duration-200 flex h-10 w-20 flex-col p-0 items-center justify-center gap-0.5 ${
                isOnlyButton
                  ? 'rounded-xl'
                  : isFirst
                    ? 'rounded-l-xl rounded-r-none border-r-0'
                    : isLast
                      ? 'rounded-r-xl rounded-l-none border-l-0'
                      : 'rounded-none border-x-0'
              }`}
              style={
                optionIndex === activeIndex
                  ? {
                      background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                    }
                  : {
                      background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                    }
              }
              onClick={() => setActiveIndex(optionIndex)}
            >
              {option.year && (
                <span className={`text-[9px] truncate leading-none ${optionIndex === activeIndex ? 'text-white' : 'text-slate-500'}`}>{option.year}</span>
              )}
              <span className="text-sm font-medium truncate leading-none">{option.label}</span>
            </Button>
          );
        })}

        {/* Next Navigation Button */}
        {onNext && (
          <Button
            variant="ghost"
            size="default"
            className="bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-r-xl rounded-l-none border-l-0"
            onClick={handleNext}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  );
}
