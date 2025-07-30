'use client';

import React from 'react';
import { ArrowUpDown, Filter, Search } from 'lucide-react';
import type { FeedFilters } from '@/lib/feed';

interface PostFiltersProps {
  filters: FeedFilters;
  onFiltersChange: (filters: FeedFilters) => void;
  postTypes: string[];
}

export function PostFilters({ filters, onFiltersChange, postTypes }: PostFiltersProps) {
  const handleFilterChange = (key: keyof FeedFilters, value: string | boolean | undefined) => {
    onFiltersChange({
      ...filters,
      [key]: value === '' ? undefined : value,
    });
  };

  return (
    <div className="mb-6 space-y-4">
      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 w-4 h-4" />
        <input
          type="text"
          placeholder="Search posts..."
          value={filters.searchTerm || ''}
          onChange={(e) => handleFilterChange('searchTerm', e.target.value)}
          className="w-full pl-10 pr-4 py-3 bg-slate-800/50 border border-slate-700/50 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent"
        />
      </div>

      {/* Filter Controls */}
      <div className="flex flex-wrap gap-4 items-center">
        {/* Post Type Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <select
            value={filters.postType || ''}
            onChange={(e) => handleFilterChange('postType', e.target.value)}
            className="bg-slate-800/50 border border-slate-700/50 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50"
          >
            <option value="">All Types</option>
            {postTypes.map((type) => (
              <option key={type} value={type}>
                {type.replace('_', ' ').replace(/\b\w/g, (l) => l.toUpperCase())}
              </option>
            ))}
          </select>
        </div>

        {/* Pinned Filter */}
        <label className="flex items-center gap-2 text-sm text-slate-400 cursor-pointer">
          <input
            type="checkbox"
            checked={filters.isPinned === true}
            onChange={(e) => handleFilterChange('isPinned', e.target.checked ? true : undefined)}
            className="rounded border-slate-600 bg-slate-800 text-blue-500 focus:ring-blue-500/50"
          />
          Pinned Only
        </label>

        {/* Sort Controls */}
        <div className="flex items-center gap-2 ml-auto">
          <ArrowUpDown className="w-4 h-4 text-slate-400" />
          <select
            value={filters.orderBy || 'createdAt'}
            onChange={(e) => handleFilterChange('orderBy', e.target.value)}
            className="bg-slate-800/50 border border-slate-700/50 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50"
          >
            <option value="createdAt">Date Created</option>
            <option value="updatedAt">Last Updated</option>
            <option value="likesCount">Most Liked</option>
            <option value="commentsCount">Most Commented</option>
          </select>
          <button
            onClick={() => handleFilterChange('descending', !filters.descending)}
            className={`px-3 py-2 text-sm rounded-lg border transition-colors ${
              filters.descending === false
                ? 'bg-blue-500/20 border-blue-500/50 text-blue-400'
                : 'bg-slate-800/50 border-slate-700/50 text-slate-400 hover:text-white'
            }`}
          >
            {filters.descending === false ? 'Asc' : 'Desc'}
          </button>
        </div>
      </div>
    </div>
  );
}
