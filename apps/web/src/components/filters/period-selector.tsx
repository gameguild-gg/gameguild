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
    <div className={cn('space-y-1', className)}>
      {/* Period Type Selector */}
      <div className="flex rounded-md border border-gray-200 bg-white">
        {periods.map((period) => (
          <Button
            key={period.value}
            variant="ghost"
            className={cn('flex-1 rounded-none h-8 text-xs', selectedPeriod === period.value && 'bg-blue-50 text-blue-600 hover:bg-blue-100')}
            onClick={() => {
              onPeriodChange?.(period.value);
            }}
          >
            {period.label}
          </Button>
        ))}
      </div>

      {/* Period Options */}
      <div className="space-y-1">
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
    <div className="flex items-center gap-1 rounded-md border border-gray-200 bg-white p-0.5">
      <Button variant="ghost" className="h-7 w-7 p-0 rounded-md hover:bg-gray-100" onClick={handlePrev}>
        <ChevronLeft className="h-3 w-3" />
      </Button>
      {visibleOptions.map((option, index) => {
        const optionIndex = options.indexOf(option);
        return (
          <Button
            key={optionIndex}
            variant="ghost"
            className={cn('flex h-7 flex-1 flex-col rounded-md py-1 text-xs', optionIndex === activeIndex && 'bg-blue-50 text-blue-600')}
            onClick={() => setActiveIndex(optionIndex)}
          >
            <span className="text-xs">{option.label}</span>
            {option.year && <span className="text-[10px] text-gray-500">{option.year}</span>}
          </Button>
        );
      })}
      <Button variant="ghost" className="h-7 w-7 p-0 rounded-md hover:bg-gray-100" onClick={handleNext}>
        <ChevronRight className="h-3 w-3" />
      </Button>
    </div>
  );
}
