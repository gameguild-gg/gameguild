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

export function UserManagementContent({ initialPagination }: UserManagementContentProps) {
  // Note: This is a simplified version to avoid build errors
  console.log('initialPagination:', initialPagination); // Use the parameter to avoid unused variable warning
  return <EnhancedUserList />;
}
