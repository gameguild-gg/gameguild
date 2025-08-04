'use client';

import { EnhancedUserList } from './enhanced-user-list';

interface UserManagementContentProps {
  initialPagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export function UserManagementContent(_props: UserManagementContentProps) {
  return <EnhancedUserList />;
}
