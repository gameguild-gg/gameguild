'use client';

import type { UserResponseDto } from '@/lib/api/generated';
import { UserList } from './user-list';

interface UserManagementContentProps {
  users: UserResponseDto[]
}

export function UserManagementContent({ users }: UserManagementContentProps) {
  console.log('UserManagementContent received users:', users.length);

  return (
    <UserList users={users} />
  );
}
