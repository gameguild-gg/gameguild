'use client';

import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useTrackFilters } from '@/lib/tracks/use-tracks';

export function TrackFilters() {
  const { area, tool, level, searchTerm, availableTools, setArea, setTool, setLevel, setSearchTerm } = useTrackFilters();

  return (
    <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-4 sm:space-y-0 sm:space-x-4 mb-8">
      <div className="flex flex-wrap gap-2 w-full sm:w-auto">
        <Select value={area} onValueChange={(value) => setArea(value)}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Area" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Areas</SelectItem>
            <SelectItem value="programming">Programming</SelectItem>
            <SelectItem value="art">Art</SelectItem>
            <SelectItem value="design">Design</SelectItem>
          </SelectContent>
        </Select>
        <Select value={tool} onValueChange={(value) => setTool(value)}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Tool" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Tools</SelectItem>
            {availableTools.map((toolItem) => (
              <SelectItem key={toolItem} value={toolItem}>
                {toolItem}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={level} onValueChange={(value) => setLevel(value)}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Level" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Levels</SelectItem>
            <SelectItem value="0">Postulant</SelectItem>
            <SelectItem value="1">Apprentice</SelectItem>
            <SelectItem value="2">Partner</SelectItem>
            <SelectItem value="3">Master</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="w-full sm:w-64">
        <Input type="text" placeholder="Search tracks..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="w-full" />
      </div>
    </div>
  );
}
