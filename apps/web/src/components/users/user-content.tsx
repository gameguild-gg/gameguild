'use client';

import { User } from '@/components/legacy/types/user';
import { UserGrid } from './user-grid';
import { UserRow } from './user-row';
import { UserTable } from './user-table';

interface UserContentProps {
  users: User[];
  viewMode: 'cards' | 'row' | 'table';
}

export function UserContent({ users, viewMode }: UserContentProps) {
  return (
    <section className="mb-12">
      {viewMode === 'cards' && <UserGrid users={users} />}
      {viewMode === 'row' && <UserRow users={users} />}
      {viewMode === 'table' && <UserTable users={users} />}
    </section>
  );
}
