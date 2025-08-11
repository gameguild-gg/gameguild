'use client';

import { UserNavigation } from './user-navigation';
import { UserHeader } from './user-header';
import { UserContent } from './user-content';
import { UserEmptyState } from './user-empty-state';

import { UserActiveFilters, UserFilterControls, UserFilterProvider, useUserFilters } from './filters';
import { useResponsiveViewMode } from '@/hooks/use-responsive-view-mode';
import { User } from '@/lib/api/generated';

interface UserManagementProps {
  users: User[];
}

function UserManagementContent() {
  const { state, filteredUsers, hasActiveFilters } = useUserFilters();
  const { isSmallScreen } = useResponsiveViewMode();

  return (
    <>
      {/* Filters, Search & View Toggle */}
      <div className="space-y-3 mb-8">
        {/* Search and Filter Controls */}
        <UserFilterControls hideViewToggle={isSmallScreen} />

        {/* Active Filters with Results Count - Only show when filters are applied */}
        {hasActiveFilters() && <UserActiveFilters totalCount={filteredUsers.length} />}
      </div>

      {/* Users Display */}
      {filteredUsers.length > 0 ? <UserContent users={filteredUsers} viewMode={state.viewMode} /> : <UserEmptyState hasFilters={hasActiveFilters()} hasUsers={filteredUsers.length > 0} />}
    </>
  );
}

export function UserManagement({ users }: UserManagementProps) {
  return (
    <UserFilterProvider users={users}>
      <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        <div className="container mx-auto px-4 py-8">
          {/* Navigation */}
          <UserNavigation />

          {/* Header */}
          <UserHeader userCount={users.length} />

          {/* Main Content with Filters */}
          <UserManagementContent />
        </div>
      </div>
    </UserFilterProvider>
  );
}
