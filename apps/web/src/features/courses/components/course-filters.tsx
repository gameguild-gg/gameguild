import React from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Search, Filter, SlidersHorizontal } from 'lucide-react';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

interface CourseFiltersProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  selectedCategory: string;
  onCategoryChange: (category: string) => void;
  selectedLevel: string;
  onLevelChange: (level: string) => void;
  sortBy: string;
  onSortChange: (sortBy: string) => void;
  categories: string[];
  levels: string[];
  sortOptions: { value: string; label: string }[];
  totalResults?: number;
  isFiltered?: boolean;
  onResetFilters?: () => void;
}

export function CourseFilters({
  searchQuery,
  onSearchChange,
  selectedCategory,
  onCategoryChange,
  selectedLevel,
  onLevelChange,
  sortBy,
  onSortChange,
  categories,
  levels,
  sortOptions,
  totalResults,
  isFiltered = false,
  onResetFilters,
}: CourseFiltersProps) {
  return (
    <div className="space-y-4">
      {/* Search Bar */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input type="text" placeholder="Search courses..." value={searchQuery} onChange={(e) => onSearchChange(e.target.value)} className="pl-9" />
      </div>

      {/* Filter Controls */}
      <div className="flex flex-wrap items-center gap-4">
        {/* Category Filter */}
        <div className="flex gap-2">
          {categories.map((category) => (
            <Button key={category} variant={selectedCategory === category ? 'default' : 'outline'} size="sm" onClick={() => onCategoryChange(category)}>
              {category}
            </Button>
          ))}
        </div>

        {/* Level Filter */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm">
              <Filter className="h-4 w-4 mr-2" />
              Level: {selectedLevel}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            {levels.map((level) => (
              <DropdownMenuItem key={level} onClick={() => onLevelChange(level)}>
                {level}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Sort Options */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm">
              <SlidersHorizontal className="h-4 w-4 mr-2" />
              Sort: {sortOptions.find((opt) => opt.value === sortBy)?.label}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            {sortOptions.map((option) => (
              <DropdownMenuItem key={option.value} onClick={() => onSortChange(option.value)}>
                {option.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Reset Filters */}
        {isFiltered && onResetFilters && (
          <Button variant="ghost" size="sm" onClick={onResetFilters}>
            Clear Filters
          </Button>
        )}
      </div>

      {/* Results Summary */}
      {totalResults !== undefined && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            {totalResults} course{totalResults !== 1 ? 's' : ''} found
          </p>
          {isFiltered && <Badge variant="secondary">Filtered</Badge>}
        </div>
      )}
    </div>
  );
}
