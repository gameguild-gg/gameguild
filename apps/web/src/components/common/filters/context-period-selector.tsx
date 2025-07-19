'use client';

import React from 'react';
import { useFilterContext } from './filter-context';
import { SmartPeriodSelector, PeriodValue } from './smart-period-selector';

interface ContextPeriodSelectorProps {
  className?: string;
  showNavigation?: boolean;
  maxVisible?: number;
}

/**
 * A context-aware period selector that automatically integrates with the filter context.
 * This component is SSR-safe and uses the smart period selector internally.
 */
export function ContextPeriodSelector({
  className,
  showNavigation = true,
  maxVisible = 3,
}: ContextPeriodSelectorProps) {
  const { state, setPeriod } = useFilterContext();

  const handlePeriodChange = (period: string) => {
    setPeriod(period);
  };

  const handlePeriodValueChange = (value: PeriodValue) => {
    // You can extend this to handle specific period value changes
    // For now, we just update the period type
    setPeriod(value.type);
  };

  return (
    <SmartPeriodSelector
      selectedPeriod={state.selectedPeriod}
      onPeriodChange={handlePeriodChange}
      onPeriodValueChange={handlePeriodValueChange}
      className={className}
      showNavigation={showNavigation}
      maxVisible={maxVisible}
    />
  );
}
