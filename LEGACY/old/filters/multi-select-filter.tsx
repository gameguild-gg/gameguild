'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Check, ChevronDown } from 'lucide-react';
import { useState } from 'react';

export interface FilterOption {
  value: string;
  label: string;
  count?: number;
}

interface MultiSelectFilterProps {
  options: FilterOption[];
  selectedValues: string[];
  onToggle: (value: string) => void;
  placeholder: string;
  emptyText?: string;
  searchPlaceholder?: string;
  className?: string;
  maxSelectedDisplay?: number;
}

export function MultiSelectFilter({
  options,
  selectedValues,
  onToggle,
  placeholder,
  emptyText = 'No options found.',
  searchPlaceholder = 'Search...',
  className = '',
  maxSelectedDisplay = 2,
}: MultiSelectFilterProps) {
  const [open, setOpen] = useState(false);

  const getDisplayText = () => {
    if (selectedValues.length === 0) {
      return placeholder;
    }
    if (selectedValues.length <= maxSelectedDisplay) {
      return selectedValues.map((value) => options.find((opt) => opt.value === value)?.label || value).join(', ');
    }
    return `${selectedValues.length} selected`;
  };

  return (
    <div className={className}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className="justify-between backdrop-blur-md border rounded-xl transition-all duration-200 border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50 focus:text-white focus:border-blue-400/40 focus:ring-2 focus:ring-blue-500/20 h-10 px-4"
            style={{
              background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
              boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
            }}
          >
            <span className="truncate">{getDisplayText()}</span>
            <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[250px] p-0 bg-slate-800/95 backdrop-blur-md border-slate-700">
          <Command className="bg-transparent">
            <CommandInput placeholder={searchPlaceholder} className="h-9 border-none text-white placeholder:text-slate-400" />
            <CommandList>
              <CommandEmpty className="text-slate-400 py-6 text-center text-sm">{emptyText}</CommandEmpty>
              <CommandGroup>
                <CommandItem value="all" onSelect={() => onToggle('all')} className="text-slate-200 hover:bg-slate-700/50 cursor-pointer">
                  <Check className={`mr-2 h-4 w-4 ${selectedValues.length === 0 ? 'opacity-100' : 'opacity-0'}`} />
                  All
                </CommandItem>
                {options.map((option) => (
                  <CommandItem
                    key={option.value}
                    value={option.value}
                    onSelect={() => onToggle(option.value)}
                    className="text-slate-200 hover:bg-slate-700/50 cursor-pointer"
                  >
                    <Check className={`mr-2 h-4 w-4 ${selectedValues.includes(option.value) ? 'opacity-100' : 'opacity-0'}`} />
                    <span className="flex-1">{option.label}</span>
                    {option.count !== undefined && (
                      <Badge variant="secondary" className="ml-2 bg-slate-700/50 text-slate-300">
                        {option.count}
                      </Badge>
                    )}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
}
