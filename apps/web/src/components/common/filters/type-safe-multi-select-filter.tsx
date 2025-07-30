'use client';

import React, { useState } from 'react';
import { FilterOption, useFilterContext } from './filter-context';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Checkbox } from '@/components/ui/checkbox';
import { ChevronDown, X } from 'lucide-react';

interface TypeSafeMultiSelectFilterProps<T extends Record<string, unknown>> {
  filterKey: keyof T;
  options: FilterOption[];
  placeholder?: string;
  className?: string;
}

/**
 * Type-safe MultiSelectFilter component that automatically connects to the filter context
 * The filterKey is constrained to actual properties of T, preventing typos and runtime errors
 */
export function TypeSafeMultiSelectFilter<T extends Record<string, unknown>>({
  filterKey,
  options,
  placeholder = 'Select options',
  className = '',
}: TypeSafeMultiSelectFilterProps<T>) {
  const { getFilterValues, toggleFilter, clearFilter } = useFilterContext<T>();
  const [isOpen, setIsOpen] = useState(false);

  const selectedValues = getFilterValues(filterKey);
  const selectedCount = selectedValues.length;

  const handleToggleOption = (value: string) => {
    toggleFilter(filterKey, value);
  };

  const handleClearAll = (e: React.MouseEvent) => {
    e.stopPropagation();
    clearFilter(filterKey);
  };

  const getDisplayText = () => {
    if (selectedCount === 0) return placeholder;
    if (selectedCount === 1) {
      const option = options.find((opt) => opt.value === selectedValues[0]);
      return option?.label || selectedValues[0];
    }
    return `${selectedCount} selected`;
  };

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className={`w-full justify-between ${className}`} onClick={() => setIsOpen(!isOpen)}>
          <span className="truncate">{getDisplayText()}</span>
          <div className="ml-2 flex items-center gap-1">
            {selectedCount > 0 && (
              <button type="button" onClick={handleClearAll} className="rounded-full p-0.5 hover:bg-gray-200">
                <X className="h-3 w-3" />
              </button>
            )}
            <ChevronDown className="h-4 w-4" />
          </div>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-64 p-2" align="start">
        <div className="space-y-2">
          {options.map((option) => (
            <div key={option.value} className="flex items-center space-x-2 rounded p-2 hover:bg-gray-50">
              <Checkbox
                id={`${String(filterKey)}-${option.value}`}
                checked={selectedValues.includes(option.value)}
                onCheckedChange={() => handleToggleOption(option.value)}
              />
              <label htmlFor={`${String(filterKey)}-${option.value}`} className="flex-1 cursor-pointer text-sm">
                <span>{option.label}</span>
                {option.count !== undefined && <span className="ml-1 text-gray-500">({option.count})</span>}
              </label>
            </div>
          ))}
          {selectedCount > 0 && (
            <div className="border-t pt-2">
              <Button variant="ghost" size="sm" onClick={() => clearFilter(filterKey)} className="w-full">
                Clear All
              </Button>
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
