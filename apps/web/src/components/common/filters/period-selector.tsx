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
  selectedPeriod: string;
  onPeriodChange?: (period: string) => void;
  className?: string;
}

export function PeriodSelector({ selectedPeriod, onPeriodChange, className }: PeriodSelectorProps) {
  // Get current date and calculate proper defaults
  const currentDate = new Date();
  const currentYear = currentDate.getFullYear();
  const currentMonth = currentDate.getMonth();
  const currentWeek = Math.ceil(currentDate.getDate() / 7); // Simple week calculation

  const [currentWeekStart, setCurrentWeekStart] = React.useState(currentWeek);
  const [currentMonthStart, setCurrentMonthStart] = React.useState(currentMonth);
  const [currentYearStart, setCurrentYearStart] = React.useState(currentYear);
  const [currentDayStart, setCurrentDayStart] = React.useState(currentDate);

  const periods = [
    { label: 'W', value: 'week', tooltip: 'Week' },
    { label: 'M', value: 'month', tooltip: 'Month' },
    { label: 'Q', value: 'quarter', tooltip: 'Quarter' },
    { label: 'Y', value: 'year', tooltip: 'Year' },
  ];

  const generateWeeks = React.useCallback(
    (start: number): PeriodOption[] => {
      return Array.from({ length: 3 }, (_, index) => {
        const weekNumber = start + index;
        return {
          label: `Week ${weekNumber}`,
          value: weekNumber.toString(),
          year: currentYear.toString(),
          selected: index === 0,
        };
      });
    },
    [currentYear],
  );

  const allMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  const generateMonths = React.useCallback(
    (start: number): PeriodOption[] => {
      return Array.from({ length: 3 }, (_, index) => {
        const monthIndex = (start + index) % 12;
        const year = currentYear + Math.floor((start + index) / 12);
        return {
          label: allMonths[monthIndex],
          value: (monthIndex + 1).toString().padStart(2, '0'),
          year: year.toString(),
          selected: index === 0,
        };
      });
    },
    [currentYear],
  );

  const generateYears = React.useCallback((start: number): PeriodOption[] => {
    return Array.from({ length: 3 }, (_, index) => {
      const year = start + index;
      return {
        label: year.toString(),
        value: year.toString(),
        selected: index === 0,
      };
    });
  }, []);

  const generateDays = React.useCallback((start: Date): PeriodOption[] => {
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
  }, []);

  const generateQuarters = React.useCallback((startYear: number): PeriodOption[] => {
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
  }, []);

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
                  } transition-all duration-200 h-10 w-10 p-0 ${index === 0 ? 'rounded-l-xl rounded-r-none border-r-0' : index === periods.length - 1 ? 'rounded-r-xl rounded-l-none border-l-0' : 'rounded-none border-x-0'}`}
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
  // Always select the first option by default
  const [activeIndex, setActiveIndex] = React.useState(0);

  // Reset active index when options change (important for navigation)
  React.useEffect(() => {
    setActiveIndex(0);
  }, [options]);

  const handleNext = () => {
    if (activeIndex < options.length - 1) {
      setActiveIndex(activeIndex + 1);
    } else if (onNext) {
      onNext(); // This will generate new options
      // activeIndex will be reset to 0 by useEffect when options change
    }
  };

  const handlePrev = () => {
    if (activeIndex > 0) {
      setActiveIndex(activeIndex - 1);
    } else if (onPrev) {
      onPrev(); // This will generate new options
      // We need to set activeIndex to the last option of the new set
      // This will be handled by useEffect, but we need to set it after options update
      React.startTransition(() => {
        setActiveIndex(2); // Since we show 3 options, last one is index 2
      });
    }
  };

  // Calculate visible options (show all available options, up to 3)
  const visibleOptions = React.useMemo(() => {
    return options.slice(0, Math.min(3, options.length));
  }, [options]);

  return (
    <div className="flex items-center gap-2 flex-1">
      {/* Connected Range Navigation and Options - Match toggle button style */}
      <div className="flex">
        {/* Previous Navigation Button */}
        {onPrev && (
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
        )}

        {/* Range Options - Connected */}
        {visibleOptions.map((option, index) => {
          const isFirst = index === 0 && !onPrev;
          const isLast = index === visibleOptions.length - 1 && !onNext;
          const isOnlyButton = visibleOptions.length === 1 && !onPrev && !onNext;

          return (
            <Button
              key={`${option.value}-${option.year || ''}`}
              variant="ghost"
              size="default"
              className={`${
                index === activeIndex ? 'backdrop-blur-md border border-blue-400/40 text-white shadow-lg shadow-blue-500/20' : 'backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
              } transition-all duration-200 flex h-10 w-20 flex-col p-0 items-center justify-center gap-0.5 ${
                isOnlyButton ? 'rounded-xl' : isFirst ? 'rounded-l-xl rounded-r-none border-r-0' : isLast ? 'rounded-r-xl rounded-l-none border-l-0' : 'rounded-none border-x-0'
              }`}
              style={
                index === activeIndex
                  ? {
                      background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
                    }
                  : {
                      background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                      boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
                    }
              }
              onClick={() => setActiveIndex(index)}
            >
              {option.year && <span className={`text-[9px] truncate leading-none ${index === activeIndex ? 'text-white' : 'text-slate-500'}`}>{option.year}</span>}
              <span className="text-sm font-medium truncate leading-none">{option.label}</span>
            </Button>
          );
        })}

        {/* Next Navigation Button */}
        {onNext && (
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
        )}
      </div>
    </div>
  );
}
