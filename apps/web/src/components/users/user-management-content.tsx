'use client';

import { Button } from '@/components/ui/button';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import { RefreshCw } from 'lucide-react';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';
import { EnhancedUserList } from './enhanced-user-list';

interface UserManagementContentProps {
  users: UserResponseDto[]
}

export function UserManagementContent({ users: initialUsers }: UserManagementContentProps) {
  console.log('UserManagementContent received users:', initialUsers.length);
  const [users, setUsers] = useState<UserResponseDto[]>(initialUsers);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const refreshUsers = useCallback(async () => {
    setIsRefreshing(true);
    try {
      const apiBase = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const response = await fetch(`${apiBase}/api/users?take=50`, {
        cache: 'no-store',
        headers: { 'Content-Type': 'application/json' }
      });

      if (!response.ok) {
        throw new Error(`Error fetching users: ${response.status}`);
      }

      const data = await response.json();
      const usersList = Array.isArray(data) ? data : (data.items || []);
      console.log('Refreshed users:', usersList.length);
      setUsers(usersList);
      toast.success('User list refreshed successfully');
    } catch (error) {
      console.error('Failed to refresh users:', error);
      toast.error('Failed to refresh users');
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button
          variant="outline"
          size="sm"
          onClick={refreshUsers}
          disabled={isRefreshing}
        >
          <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
          Refresh Users
        </Button>
      </div>
      <EnhancedUserList users={users} />
    </div>
  );
}
