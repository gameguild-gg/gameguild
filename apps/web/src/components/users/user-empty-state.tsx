'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Filter, Plus, Users, UserSearch } from 'lucide-react';

interface UserEmptyStateProps {
  hasFilters: boolean;
  hasUsers: boolean;
  onCreateUser?: () => void;
  onClearFilters?: () => void;
}

export function UserEmptyState({ hasFilters, hasUsers, onCreateUser, onClearFilters }: UserEmptyStateProps) {
  if (hasFilters && !hasUsers) {
    // No results due to filters
    return (
      <Card className="border-slate-700/50 bg-slate-800/30 backdrop-blur-sm">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-slate-700/50">
            <UserSearch className="h-8 w-8 text-slate-400" />
          </div>
          <CardTitle className="text-slate-200">No users match your filters</CardTitle>
          <CardDescription className="text-slate-400">Try adjusting your search criteria or clearing the active filters.</CardDescription>
        </CardHeader>
        <CardContent className="text-center">
          <Button variant="outline" onClick={onClearFilters} className="mr-2">
            <Filter className="mr-2 h-4 w-4" />
            Clear Filters
          </Button>
        </CardContent>
      </Card>
    );
  }

  // No users at all
  return (
    <Card className="border-slate-700/50 bg-slate-800/30 backdrop-blur-sm">
      <CardHeader className="text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-slate-700/50">
          <Users className="h-8 w-8 text-slate-400" />
        </div>
        <CardTitle className="text-slate-200">No users found</CardTitle>
        <CardDescription className="text-slate-400">Get started by creating your first user account.</CardDescription>
      </CardHeader>
      <CardContent className="text-center">
        <Button onClick={onCreateUser}>
          <Plus className="mr-2 h-4 w-4" />
          Create First User
        </Button>
      </CardContent>
    </Card>
  );
}
