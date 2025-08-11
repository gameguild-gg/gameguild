'use client';

import { Input } from '@/components/ui/input';
import { Search, X } from 'lucide-react';

interface SessionSearchBarProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
}

export function SessionSearchBar({ searchTerm, onSearchChange }: SessionSearchBarProps) {
  return (
    <div className="w-full relative">
      <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4 z-10" />
      <Input
        type="search"
        placeholder="Search sessions..."
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        className={`w-full pl-12 pr-12 h-10 backdrop-blur-md border rounded-xl transition-all duration-200 ${
          searchTerm
            ? 'border-blue-400/40 text-white placeholder:text-white/70 shadow-lg shadow-blue-500/20'
            : 'border-slate-600/30 text-slate-400 placeholder:text-slate-400 hover:text-slate-200'
        } focus:text-white focus:placeholder:text-white focus:outline-none focus:border-blue-400/40 focus-visible:border-blue-400/40 focus:ring-2 focus:ring-blue-500/20 focus-visible:ring-blue-500/20 selection:bg-blue-500/30 selection:text-white`}
        style={
          searchTerm
            ? {
                background: 'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)',
                boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)',
              }
            : {
                background: 'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)',
                boxShadow: 'inset 0 1px 2px rgba(0, 0, 0, 0.05)',
              }
        }
        onFocus={(e) => {
          e.target.style.background =
            'radial-gradient(ellipse 80% 60% at center, rgba(59, 130, 246, 0.4) 0%, rgba(37, 99, 235, 0.3) 50%, rgba(29, 78, 216, 0.2) 100%)';
          e.target.style.boxShadow = 'inset 0 1px 2px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(59, 130, 246, 0.2)';
        }}
        onBlur={(e) => {
          if (!searchTerm) {
            e.target.style.background =
              'radial-gradient(ellipse 80% 60% at center, rgba(51, 65, 85, 0.3) 0%, rgba(30, 41, 59, 0.25) 50%, rgba(15, 23, 42, 0.2) 100%)';
            e.target.style.boxShadow = 'inset 0 1px 2px rgba(0, 0, 0, 0.05)';
          }
        }}
      />
      {searchTerm && (
        <button
          onClick={() => onSearchChange('')}
          className="absolute right-4 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors duration-200 z-10"
        >
          <X className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}
