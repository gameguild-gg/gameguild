import { Suspense } from 'react';
import { Metadata } from 'next';
import { getUsersData } from '@/lib/actions/users.ts';
import { UserProvider } from '@/lib/users/user-context.tsx';
import { UserManagementContent } from '@/components/dashboard/users/user-management-content';
import { LoadingSpinner } from '@game-guild/ui/components';
import { ErrorBoundary } from '@game-guild/ui/components';

export const metadata: Metadata = {
  title: 'User Management | Game Guild Dashboard',
  description: 'Manage users, permissions, and access control in the Game Guild platform.',
};

interface UsersPageProps {
  searchParams: Promise<{
    page?: string;
    limit?: string;
    search?: string;
    status?: string;
  }>;
}

export default async function UsersPage({ searchParams }: UsersPageProps) {
  const params = await searchParams;
  const page = parseInt(params.page || '1');
  const limit = parseInt(params.limit || '20');
  const search = params.search || '';

  try {
    const userData = await getUsersData(page, limit, search);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">User Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage users, roles, and permissions across the platform.</p>
        </div>

        <ErrorBoundary fallback={<div className="text-red-500">Failed to load user management interface</div>}>
          <UserProvider initialUsers={userData.users}>
            <Suspense fallback={<LoadingSpinner />}>
              <UserManagementContent initialPagination={userData.pagination} />
            </Suspense>
          </UserProvider>
        </ErrorBoundary>
      </div>
    );
  } catch (error) {
    console.error('Error loading users page:', error);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">User Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage users, roles, and permissions across the platform.</p>
        </div>

        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold text-red-800 mb-2">Unable to Load Users</h2>
          <p className="text-red-600">There was an error loading the user data. Please check your connection and try again.</p>
        </div>
      </div>
    );
  }
}
