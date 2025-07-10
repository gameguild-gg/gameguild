'use client';

import React from 'react';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useCourseFilters, useAvailableTools } from '@/hooks/use-courses';
import { COURSE_AREAS, COURSE_LEVEL_NAMES } from '@/types/courses';

export function CourseFilters() {
  const { filters, setArea, setLevel, setTool, setSearchTerm } = useCourseFilters();
  const availableTools = useAvailableTools();

  return (
    <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-4 sm:space-y-0 sm:space-x-4 mb-8">
      <div className="flex flex-wrap gap-2 w-full sm:w-auto">
        <Select value={filters.area} onValueChange={setArea}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Area" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Areas</SelectItem>
            {COURSE_AREAS.map((area) => (
              <SelectItem key={area} value={area}>
                {area.charAt(0).toUpperCase() + area.slice(1)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={filters.level} onValueChange={setLevel}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Level" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Levels</SelectItem>
            {COURSE_LEVEL_NAMES.map((level) => (
              <SelectItem key={level} value={level}>
                {level}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={filters.tool} onValueChange={setTool}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Tool" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Tools</SelectItem>
            {availableTools.map((tool) => (
              <SelectItem key={tool} value={tool}>
                {tool}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="w-full sm:w-64">
        <Input type="text" placeholder="Search courses..." value={filters.searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="w-full" />
      </div>
    </div>
  );
}
