'use client';

import { useState } from 'react';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestSessionGrid } from './test-session-grid';
import { TestSessionRow } from './test-session-row';
import { TestSessionTable } from './test-session-table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ArrowLeft, LayoutGrid, List, Rows, Search, X, ChevronDown, Shield, Clock, Users, Monitor, Star, Trophy } from 'lucide-react';
import Link from 'next/link';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

export function TestingLabSessions({ testSessions }: TestingLabSessionsProps) {
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>('cards');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [selectedSessionTypes, setSelectedSessionTypes] = useState<string[]>([]);

  // Helper functions for multi-select
  const toggleStatus = (status: string) => {
    if (status === 'all') {
      setSelectedStatuses([]);
    } else {
      setSelectedStatuses(prev => 
        prev.includes(status) 
          ? prev.filter(s => s !== status)
          : [...prev, status]
      );
    }
  };

  const toggleSessionType = (type: string) => {
    if (type === 'all') {
      setSelectedSessionTypes([]);
    } else {
      setSelectedSessionTypes(prev => 
        prev.includes(type) 
          ? prev.filter(t => t !== type)
          : [...prev, type]
      );
    }
  };

  const getDisplayText = (selected: string[], allText: string, items: { value: string; label: string }[]) => {
    if (selected.length === 0) return allText;
    if (selected.length === 1) {
      const item = items.find(i => i.value === selected[0]);
      return item?.label || selected[0];
    }
    return `${selected.length} selected`;
  };

  // Helper functions for icons
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'open':
        return <Shield className="h-4 w-4 text-green-400" />;
      case 'full':
        return <Users className="h-4 w-4 text-red-400" />;
      case 'in-progress':
        return <Clock className="h-4 w-4 text-blue-400" />;
      default:
        return <Shield className="h-4 w-4 text-slate-400" />;
    }
  };

  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <Monitor className="h-4 w-4 text-blue-400" />;
      case 'usability':
        return <Users className="h-4 w-4 text-purple-400" />;
      case 'bug-testing':
        return <Star className="h-4 w-4 text-orange-400" />;
      default:
        return <Trophy className="h-4 w-4 text-slate-400" />;
    }
  };

  // Filter sessions based on search and filters
  const filteredSessions = testSessions.filter((session) => {
    const matchesSearch = session.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         session.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = selectedStatuses.length === 0 || selectedStatuses.includes(session.status);
    const matchesType = selectedSessionTypes.length === 0 || selectedSessionTypes.includes(session.sessionType);
    
    return matchesSearch && matchesStatus && matchesType;
  });

  const clearFilters = () => {
    setSearchTerm('');
    setSelectedStatuses([]);
    setSelectedSessionTypes([]);
  };

  const hasActiveFilters = searchTerm || selectedStatuses.length > 0 || selectedSessionTypes.length > 0;

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <div className="container mx-auto px-4 py-8">
        {/* Navigation */}
        <div className="flex items-center gap-4 mb-8">
          <Button asChild variant="ghost" className="text-slate-400 hover:text-white">
            <Link href="/testing-lab">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Testing Lab
            </Link>
          </Button>
        </div>

        {/* Header */}
        <div className="text-center mb-12">
          <div className="mb-6">
            {testSessions.length > 0 && (
              <div className="flex justify-center mb-6">
                <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 backdrop-blur-sm border border-blue-400/30 rounded-full px-4 py-2 flex items-center gap-2">
                  <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse"></div>
                  <span className="text-blue-300 font-semibold text-sm">
                    {testSessions.length} Open {testSessions.length === 1 ? 'Session' : 'Sessions'} • Join Now!
                  </span>
                </div>
              </div>
            )}
            <h1
              className="text-5xl md:text-6xl font-bold text-white my-8"
              style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}
            >
              Test. Play. Earn.
            </h1>
          </div>
          <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
            Join exclusive game testing sessions and shape the future of gaming. Get early access, provide valuable feedback, and earn amazing rewards while
            playing the latest titles before anyone else.
          </p>
        </div>

        {/* Filters, Search & View Toggle */}
        <div className="space-y-6 mb-8">
          {/* Search and Filter Controls */}
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search Bar */}
            <div className="flex-1 max-w-md relative">
              <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
              <Input
                type="text"
                placeholder="Search sessions..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-12 pr-4 h-10 bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/50 rounded-xl text-white placeholder-slate-400 focus:outline-none focus:border-blue-400/60 focus:ring-2 focus:ring-blue-400/20 transition-all duration-200"
              />
            </div>
            
            {/* Inline Filter Options */}
            <div className="flex flex-wrap gap-3 items-center">
              {/* Status Filter */}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    className="bg-slate-900/20 backdrop-blur-md border border-slate-600/50 rounded-xl px-4 h-10 text-slate-200 text-sm focus:outline-none focus:border-blue-400/60 hover:border-blue-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]"
                  >
                    <div className="flex items-center gap-2">
                      {selectedStatuses.length === 0 ? (
                        getStatusIcon('all')
                      ) : selectedStatuses.length === 1 ? (
                        getStatusIcon(selectedStatuses[0])
                      ) : (
                        <Shield className="h-4 w-4 text-slate-400" />
                      )}
                      <span>{getDisplayText(selectedStatuses, 'All Status', [
                        { value: 'open', label: 'Open' },
                        { value: 'full', label: 'Full' },
                        { value: 'in-progress', label: 'In Progress' }
                      ])}</span>
                    </div>
                    <ChevronDown className="h-4 w-4 ml-2" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
                  <DropdownMenuItem onClick={() => toggleStatus('all')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Shield className="h-4 w-4 text-slate-400" />
                      <span>All Status</span>
                      {selectedStatuses.length === 0 && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleStatus('open')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Shield className="h-4 w-4 text-green-400" />
                      <span>Open</span>
                      {selectedStatuses.includes('open') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleStatus('full')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-red-400" />
                      <span>Full</span>
                      {selectedStatuses.includes('full') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleStatus('in-progress')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4 text-blue-400" />
                      <span>In Progress</span>
                      {selectedStatuses.includes('in-progress') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              {/* Session Type Filter */}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    className="bg-slate-900/20 backdrop-blur-md border border-slate-600/50 rounded-xl px-4 h-10 text-slate-200 text-sm focus:outline-none focus:border-blue-400/60 hover:border-blue-400/60 hover:bg-white/5 transition-all duration-200 justify-between min-w-[140px]"
                  >
                    <div className="flex items-center gap-2">
                      {selectedSessionTypes.length === 0 ? (
                        getSessionTypeIcon('all')
                      ) : selectedSessionTypes.length === 1 ? (
                        getSessionTypeIcon(selectedSessionTypes[0])
                      ) : (
                        <Trophy className="h-4 w-4 text-slate-400" />
                      )}
                      <span>{getDisplayText(selectedSessionTypes, 'All Types', [
                        { value: 'gameplay', label: 'Gameplay' },
                        { value: 'usability', label: 'Usability' },
                        { value: 'bug-testing', label: 'Bug Testing' }
                      ])}</span>
                    </div>
                    <ChevronDown className="h-4 w-4 ml-2" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="bg-slate-900/80 backdrop-blur-xl border border-slate-600/30 rounded-xl shadow-2xl shadow-black/50">
                  <DropdownMenuItem onClick={() => toggleSessionType('all')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Trophy className="h-4 w-4 text-slate-400" />
                      <span>All Types</span>
                      {selectedSessionTypes.length === 0 && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleSessionType('gameplay')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Monitor className="h-4 w-4 text-blue-400" />
                      <span>Gameplay</span>
                      {selectedSessionTypes.includes('gameplay') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleSessionType('usability')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-purple-400" />
                      <span>Usability</span>
                      {selectedSessionTypes.includes('usability') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => toggleSessionType('bug-testing')} className="text-slate-200 hover:bg-white/5 focus:bg-white/10 transition-all duration-200 backdrop-blur-sm rounded-lg mx-1 my-0.5">
                    <div className="flex items-center gap-2">
                      <Star className="h-4 w-4 text-orange-400" />
                      <span>Bug Testing</span>
                      {selectedSessionTypes.includes('bug-testing') && <span className="ml-auto text-blue-400">✓</span>}
                    </div>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              {/* Clear Filters Button */}
              {hasActiveFilters && (
                <Button
                  variant="ghost"
                  size="default"
                  onClick={clearFilters}
                  className="text-slate-400 hover:text-slate-200 text-sm bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/50 rounded-xl hover:border-blue-400/60 transition-all duration-200 h-10 px-3"
                >
                  <X className="h-4 w-4 mr-1" />
                  Clear
                </Button>
              )}
            </div>

            {/* View Mode Toggle */}
            <div className="flex">
              <Button
                variant="ghost"
                size="default"
                onClick={() => setViewMode('cards')}
                className={`${
                  viewMode === 'cards' 
                    ? 'bg-gradient-to-r from-blue-500/30 to-blue-600/30 backdrop-blur-md border border-blue-400/40 text-blue-200 shadow-lg shadow-blue-500/20' 
                    : 'bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
                } transition-all duration-200 rounded-l-xl rounded-r-none border-r-0 h-10 px-3`}
              >
                <LayoutGrid className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="default"
                onClick={() => setViewMode('row')}
                className={`${
                  viewMode === 'row'
                    ? 'bg-gradient-to-r from-purple-500/30 to-purple-600/30 backdrop-blur-md border border-purple-400/40 text-purple-200 shadow-lg shadow-purple-500/20'
                    : 'bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
                } transition-all duration-200 rounded-none border-x-0 h-10 px-3`}
              >
                <Rows className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="default"
                onClick={() => setViewMode('table')}
                className={`${
                  viewMode === 'table'
                    ? 'bg-gradient-to-r from-green-500/30 to-green-600/30 backdrop-blur-md border border-green-400/40 text-green-200 shadow-lg shadow-green-500/20'
                    : 'bg-gradient-to-r from-slate-900/60 to-slate-800/60 backdrop-blur-md border border-slate-600/30 text-slate-400 hover:text-slate-200 hover:border-slate-500/50'
                } transition-all duration-200 rounded-r-xl rounded-l-none border-l-0 h-10 px-3`}
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>

          {/* Active Filters Display */}
          {hasActiveFilters && (
            <div className="flex flex-wrap items-center gap-2 pt-2">
              <span className="text-sm text-slate-400">Active filters:</span>
              {searchTerm && (
                <div className="bg-blue-500/20 border border-blue-400/30 rounded-full px-3 py-1 text-blue-300 text-xs flex items-center gap-2">
                  Search: &quot;{searchTerm}&quot;
                  <button onClick={() => setSearchTerm('')} className="hover:text-blue-200">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {selectedStatuses.map(status => (
                <div key={status} className="bg-green-500/20 border border-green-400/30 rounded-full px-3 py-1 text-green-300 text-xs flex items-center gap-2">
                  Status: {status.charAt(0).toUpperCase() + status.slice(1)}
                  <button onClick={() => toggleStatus(status)} className="hover:text-green-200">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              ))}
              {selectedSessionTypes.map(type => (
                <div key={type} className="bg-purple-500/20 border border-purple-400/30 rounded-full px-3 py-1 text-purple-300 text-xs flex items-center gap-2">
                  Type: {type.replace('-', ' ').charAt(0).toUpperCase() + type.replace('-', ' ').slice(1)}
                  <button onClick={() => toggleSessionType(type)} className="hover:text-purple-200">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Results Count */}
          <div className="text-center">
            <p className="text-slate-400 text-sm">
              Showing {filteredSessions.length} of {testSessions.length} sessions
            </p>
          </div>
        </div>
        {/* Sessions Display */}
        {filteredSessions.length > 0 ? (
          <section className="mb-12">
            {viewMode === 'cards' && <TestSessionGrid sessions={filteredSessions} />}
            {viewMode === 'row' && <TestSessionRow sessions={filteredSessions} />}
            {viewMode === 'table' && <TestSessionTable sessions={filteredSessions} />}
          </section>
        ) : testSessions.length > 0 ? (
          <div className="text-center py-16">
            <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-12 backdrop-blur-sm max-w-2xl mx-auto">
              <h3 className="text-2xl font-semibold text-white mb-4">No Sessions Match Your Filters</h3>
              <p className="text-slate-400 mb-6">Try adjusting your search terms or filters to find more sessions.</p>
              <Button 
                onClick={clearFilters}
                className="bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0"
              >
                Clear All Filters
              </Button>
            </div>
          </div>
        ) : (
          <div className="text-center py-16">
            <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-12 backdrop-blur-sm max-w-2xl mx-auto">
              <h3 className="text-2xl font-semibold text-white mb-4">No Sessions Available</h3>
              <p className="text-slate-400 mb-6">Check back soon for new testing opportunities, or submit your own game for testing!</p>
              <div className="flex flex-col sm:flex-row gap-4 justify-center">
                <Button asChild variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50">
                  <Link href="/testing-lab">Back to Testing Lab</Link>
                </Button>
                <Button asChild className="bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0">
                  <Link href="/testing-lab/submit">Submit Your Game</Link>
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
  
