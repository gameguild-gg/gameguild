import React from 'react';
import { ArrowUpDown, Filter, Search } from 'lucide-react';
import type { FeedFilters } from '@/lib/feed';

interface PostFiltersStaticProps {
  filters: FeedFilters;
  postTypes: string[];
}

export function PostFiltersStatic({ filters, postTypes }: PostFiltersStaticProps) {
  return (
    <div className="mb-6 space-y-4">
      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 w-4 h-4" />
        <input
          type="text"
          placeholder="Search posts..."
          value={filters.searchTerm || ''}
          disabled
          className="w-full pl-10 pr-4 py-3 bg-slate-800/50 border border-slate-700 rounded-xl text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
        />
      </div>

      {/* Filters Row */}
      <div className="flex flex-wrap gap-4">
        {/* Post Type Filter */}
        <div className="flex-1 min-w-[200px]">
          <select
            value={filters.postType || ''}
            disabled
            className="w-full px-4 py-3 bg-slate-800/50 border border-slate-700 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <option value="">All Post Types</option>
            {postTypes.map((type) => (
              <option key={type} value={type}>
                {type.charAt(0).toUpperCase() + type.slice(1).replace('_', ' ')}
              </option>
            ))}
          </select>
        </div>

        {/* Sort Options */}
        <div className="flex-1 min-w-[200px]">
          <div className="flex items-center space-x-2">
            <ArrowUpDown className="w-4 h-4 text-slate-400" />
            <select
              value={filters.orderBy || 'createdAt'}
              disabled
              className="flex-1 px-4 py-3 bg-slate-800/50 border border-slate-700 rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <option value="createdAt">Most Recent</option>
              <option value="likesCount">Most Liked</option>
              <option value="commentsCount">Most Discussed</option>
              <option value="sharesCount">Most Shared</option>
            </select>
          </div>
        </div>

        {/* Additional Filters */}
        <div className="flex items-center space-x-4">
          <button
            disabled
            className="flex items-center space-x-2 px-4 py-3 bg-slate-800/50 border border-slate-700 rounded-xl text-white hover:bg-slate-700/50 focus:outline-none focus:ring-2 focus:ring-purple-500 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Filter className="w-4 h-4" />
            <span>Filters</span>
          </button>
        </div>
      </div>

      {/* Active Filters Display */}
      {(filters.searchTerm || filters.postType) && (
        <div className="flex flex-wrap gap-2">
          {filters.searchTerm && (
            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-purple-900/50 text-purple-200 border border-purple-700">
              Search: "{filters.searchTerm}"
            </span>
          )}
          {filters.postType && (
            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-blue-900/50 text-blue-200 border border-blue-700">
              Type: {filters.postType.charAt(0).toUpperCase() + filters.postType.slice(1).replace('_', ' ')}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
