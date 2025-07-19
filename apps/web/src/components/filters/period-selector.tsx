'use client';

import * as React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
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
    { label: 'Week', value: 'week' },
    { label: 'Month', value: 'month' },
    { label: 'Quarter', value: 'quarter' },
    { label: 'Year', value: 'year' },
  ];

  const generateWeeks = (start: number): PeriodOption[] => {
    return Array.from({ length: 5 }, (_, index) => {
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
    return Array.from({ length: 5 }, (_, index) => {
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
    return Array.from({ length: 5 }, (_, index) => {
      const year = start + index;
      return {
        label: year.toString(),
        value: year.toString(),
        selected: index === 0,
      };
    });
  };

  const generateDays = (start: Date): PeriodOption[] => {
    return Array.from({ length: 5 }, (_, index) => {
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

  const weeks = React.useMemo(() => generateWeeks(currentWeekStart), [currentWeekStart]);
  const months = React.useMemo(() => generateMonths(currentMonthStart), [currentMonthStart]);
  const years = React.useMemo(() => generateYears(currentYearStart), [currentYearStart]);
  const days = React.useMemo(() => generateDays(currentDayStart), [currentDayStart]);

  const quarters: PeriodOption[] = [
    { label: 'Q1', value: 'Q1', year: '2023', selected: true },
    { label: 'Q2', value: 'Q2', year: '2023' },
    { label: 'Q3', value: 'Q3', year: '2023' },
    { label: 'Q4', value: 'Q4', year: '2023' },
    { label: 'Q1', value: 'Q1', year: '2024' },
  ];

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
      {/* Period Type Selector - Match toggle button style */}
      <div className="flex flex-wrap flex-shrink-0">
        {periods.map((period, index) => (
          <Button
            key={period.value}
            variant="ghost"
            size="default"
            className={`${
              selectedPeriod === period.value
                ? 'bg-gradient-to-r from-blue-500/30 to-blue-600/30 backdrop-blur-md border border-blue-400/40 text-blue-200 shadow-lg shadow-blue-500/20'
                : 'bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
            } transition-all duration-200 h-10 px-3 text-xs ${
              index === 0
                ? 'rounded-l-xl rounded-r-none border-r-0'
                : index === periods.length - 1
                  ? 'rounded-r-xl rounded-l-none border-l-0'
                  : 'rounded-none border-x-0'
            }`}
            onClick={() => {
              onPeriodChange?.(period.value);
            }}
          >
            {period.label}
          </Button>
        ))}
      </div>

      {/* Period Range Selector - Horizontal Layout with overflow control */}
      <div className="flex items-center gap-2 flex-1 flex-wrap">
        {/* Navigation and Range Options in Single Row */}
        {selectedPeriod === 'week' && <PeriodRow options={weeks} onNext={() => handleWeekChange('next')} onPrev={() => handleWeekChange('prev')} />}
        {selectedPeriod === 'month' && <PeriodRow options={months} onNext={() => handleMonthChange('next')} onPrev={() => handleMonthChange('prev')} />}
        {selectedPeriod === 'quarter' && <PeriodRow options={quarters} />}
        {selectedPeriod === 'year' && <PeriodRow options={years} onNext={() => handleYearChange('next')} onPrev={() => handleYearChange('prev')} />}
        {selectedPeriod === 'day' && <PeriodRow options={days} onNext={() => handleDayChange('next')} onPrev={() => handleDayChange('prev')} />}
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

  // Calculate visible options (show only 3 at a time)
  const visibleOptions = React.useMemo(() => {
    // If active index is at the beginning, show first 3
    if (activeIndex === 0) return options.slice(0, 3);
    // If active index is at the end, show last 3
    if (activeIndex === options.length - 1) return options.slice(-3);
    // Otherwise show the active one and one on each side
    return options.slice(Math.max(0, activeIndex - 1), Math.min(options.length, activeIndex + 2));
  }, [activeIndex, options]);

  return (
    <div className="flex items-center gap-1 flex-1">
      {/* Previous Navigation Button - Left Side */}
      {onPrev && (
        <Button
          variant="ghost"
          size="default"
          className="bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-xl flex-shrink-0"
          onClick={handlePrev}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
      )}

      {/* Range Options - Center */}
      <div className="flex items-center gap-1 flex-1">
        {visibleOptions.map((option) => {
          const optionIndex = options.indexOf(option);
          return (
            <Button
              key={optionIndex}
              variant="ghost"
              size="default"
              className={`${
                optionIndex === activeIndex
                  ? 'bg-gradient-to-r from-blue-500/30 to-blue-600/30 backdrop-blur-md border border-blue-400/40 text-blue-200 shadow-lg shadow-blue-500/20'
                  : 'bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
              } transition-all duration-200 flex h-10 min-w-0 flex-1 flex-col rounded-xl py-1 text-xs`}
              onClick={() => setActiveIndex(optionIndex)}
            >
              <span className="text-xs font-medium truncate">{option.label}</span>
              {option.year && <span className="text-[10px] text-slate-500 truncate">{option.year}</span>}
            </Button>
          );
        })}
      </div>

      {/* Next Navigation Button - Right Side */}
      {onNext && (
        <Button
          variant="ghost"
          size="default"
          className="bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 transition-all duration-200 h-10 w-10 p-0 rounded-xl flex-shrink-0"
          onClick={handleNext}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      )}
    </div>
  );
}
