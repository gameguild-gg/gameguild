'use client';

import React, { useEffect, useState, useMemo } from 'react';
import { Check, ChevronsUpDown, Search, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useEnhancedFilterContext, FilterOption, EnhancedFilterConfig } from './enhanced-filter-context';

interface TypeSafeEnhancedMultiSelectFilterProps<T extends Record<string, unknown>, K extends keyof T> {
  filterKey: K;
  config: EnhancedFilterConfig<T, K>;
  className?: string;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyText?: string;
  maxDisplayItems?: number;
  showClearAll?: boolean;
}

export function TypeSafeEnhancedMultiSelectFilter<T extends Record<string, unknown>, K extends keyof T>({
  filterKey,
  config,
  className,
  placeholder = 'Select items...',
  searchPlaceholder = 'Search items...',
  emptyText = 'No items found.',
  maxDisplayItems = 3,
  showClearAll = true,
}: TypeSafeEnhancedMultiSelectFilterProps<T, K>) {
  const { getFilterValues, toggleFilter, clearFilter, registerFilterConfig } = useEnhancedFilterContext<T>();
  const [open, setOpen] = useState(false);
  const [searchValue, setSearchValue] = useState('');

  // Register this filter configuration on mount
  useEffect(() => {
    registerFilterConfig(config);
  }, [config, registerFilterConfig]);

  const selectedValues = getFilterValues(filterKey);
  const hasSelection = selectedValues.length > 0;

  // Memoized filtered options for performance
  const filteredOptions = useMemo(() => {
    if (!searchValue) return config.options;
    return config.options.filter((option) =>
      option.label.toLowerCase().includes(searchValue.toLowerCase()) ||
      option.value.toLowerCase().includes(searchValue.toLowerCase())
    );
  }, [config.options, searchValue]);

  // Memoized selected option labels for display
  const selectedLabels = useMemo(() => {
    return selectedValues
      .map((value) => config.options.find((option) => option.value === value)?.label || value)
      .slice(0, maxDisplayItems);
  }, [selectedValues, config.options, maxDisplayItems]);

  const handleSelect = (value: string) => {
    toggleFilter(filterKey, value);
  };

  const handleClearAll = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    clearFilter(filterKey);
  };

  const displayText = useMemo(() => {
    if (!hasSelection) return placeholder;
    
    if (selectedValues.length <= maxDisplayItems) {
      return selectedLabels.join(', ');
    }
    
    return `${selectedLabels.slice(0, maxDisplayItems - 1).join(', ')}, +${selectedValues.length - (maxDisplayItems - 1)} more`;
  }, [hasSelection, selectedValues.length, selectedLabels, maxDisplayItems, placeholder]);

  return (
    <div className={cn('relative', className)}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn(
              'justify-between text-left font-normal',
              hasSelection && 'bg-accent/50 border-primary/20',
              'min-w-[200px] max-w-[300px]'
            )}
          >
            <span className="truncate flex-1">{displayText}</span>
            <div className="flex items-center gap-2 ml-2 flex-shrink-0">
              {hasSelection && showClearAll && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-4 w-4 p-0 hover:bg-destructive/20"
                  onClick={handleClearAll}
                  aria-label="Clear all filters"
                >
                  <X className="h-3 w-3" />
                </Button>
              )}
              <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-50" />
            </div>
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[300px] p-0" align="start">
          <Command>
            <CommandInput
              placeholder={searchPlaceholder}
              value={searchValue}
              onValueChange={setSearchValue}
              className="h-9"
            />
            <CommandList>
              <CommandEmpty>{emptyText}</CommandEmpty>
              <CommandGroup>
                {filteredOptions.map((option) => {
                  const isSelected = selectedValues.includes(option.value);
                  return (
                    <CommandItem
                      key={option.value}
                      value={option.value}
                      onSelect={() => handleSelect(option.value)}
                      className="flex items-center justify-between"
                    >
                      <div className="flex items-center">
                        <Check
                          className={cn(
                            'mr-2 h-4 w-4',
                            isSelected ? 'opacity-100' : 'opacity-0'
                          )}
                        />
                        <span>{option.label}</span>
                      </div>
                      {option.count !== undefined && (
                        <Badge variant="secondary" className="ml-2">
                          {option.count}
                        </Badge>
                      )}
                    </CommandItem>
                  );
                })}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
      
      {/* Selected items display */}
      {hasSelection && (
        <div className="mt-2 flex flex-wrap gap-1">
          {selectedValues.slice(0, 10).map((value) => {
            const option = config.options.find((opt) => opt.value === value);
            return (
              <Badge key={value} variant="secondary" className="text-xs">
                {option?.label || value}
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-3 w-3 p-0 ml-1 hover:bg-destructive/20"
                  onClick={() => handleSelect(value)}
                >
                  <X className="h-2 w-2" />
                </Button>
              </Badge>
            );
          })}
          {selectedValues.length > 10 && (
            <Badge variant="outline" className="text-xs">
              +{selectedValues.length - 10} more
            </Badge>
          )}
        </div>
      )}
    </div>
  );
}
