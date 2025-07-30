import { UserManagementContent } from '@/components/users/user-management-content';
import { getUsers } from '@/lib/user-management/users/users.actions';
import { UserProvider } from '@/lib/user-management/users/users.context';
import { Loader2 } from 'lucide-react';
import { Metadata } from 'next';
import { Suspense } from 'react';

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
    const response = await getUsers();

    const users = response.data || [];
    // For now, handle pagination and search client-side since the API structure is different
    const filteredUsers = search ? users.filter((user) => user.name?.toLowerCase().includes(search.toLowerCase()) || user.email?.toLowerCase().includes(search.toLowerCase())) : users;

    const startIndex = (page - 1) * limit;
    const paginatedUsers = filteredUsers.slice(startIndex, startIndex + limit);

    const userData = {
      users: paginatedUsers,
      pagination: {
        page,
        limit,
        total: filteredUsers.length,
        totalPages: Math.ceil(filteredUsers.length / limit),
      },
    };

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">User Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Manage users, roles, and permissions across the platform.</p>
        </div>

        <UserProvider initialUsers={userData.users} initialPagination={userData.pagination}>
          <Suspense
            fallback={
              <div className="flex items-center justify-center p-4">
                <Loader2 className="h-4 w-4 animate-spin" />
              </div>
            }
          >
            <UserManagementContent initialPagination={userData.pagination} />
          </Suspense>
        </UserProvider>
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
