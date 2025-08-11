'use client';

import React from 'react';
import { ChevronDown } from 'lucide-react';

interface LoadMoreProps {
  onLoadMore: () => void;
  loading?: boolean;
}

export function LoadMore({ onLoadMore, loading = false }: LoadMoreProps) {
  return (
    <div className="flex justify-center py-6">
      <button
        onClick={onLoadMore}
        disabled={loading}
        className="flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-blue-500/20 to-purple-500/20 border border-blue-500/30 rounded-lg text-blue-400 hover:from-blue-500/30 hover:to-purple-500/30 hover:border-blue-500/50 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {loading ? (
          <>
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-400"></div>
            Loading...
          </>
        ) : (
          <>
            <ChevronDown className="w-4 h-4" />
            Load More Posts
          </>
        )}
      </button>
    </div>
  );
}
