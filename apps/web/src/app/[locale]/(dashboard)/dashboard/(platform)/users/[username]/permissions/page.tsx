import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { UserPermissionsContent } from '@/components/users/user-permissions-content';
import { getUserById } from '@/lib/users/users.actions';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import React from 'react';

interface UserPermissionsPageProps {
  params: Promise<{
    username: string;
  }>;
}

export async function generateMetadata({ params }: UserPermissionsPageProps): Promise<Metadata> {
  const { username } = await params;
  
  try {
    const userResponse = await getUserById({
      path: { id: username },
      url: '/api/users/{id}',
    });
    const user = userResponse.data;
    
    return {
      title: `${user?.name || 'User'} Permissions | User Management | Game Guild Dashboard`,
      description: `Manage ${user?.name || 'user'} permissions and access control.`,
    };
  } catch {
    return {
      title: 'User Permissions | Game Guild Dashboard',
      description: 'User permissions management.',
    };
  }
}

export default async function UserPermissionsPage({ params }: UserPermissionsPageProps): Promise<React.JSX.Element> {
  const { username } = await params;
  
  try {
    const userResponse = await getUserById({
      path: { id: username },
      url: '/api/users/{id}',
    });
    const user = userResponse.data;
    
    if (!user) {
      notFound();
    }

    return (
      <DashboardPage>
        <DashboardPageHeader>
          <DashboardPageTitle>{user.name} - Permissions</DashboardPageTitle>
          <DashboardPageDescription>Manage user permissions and access control</DashboardPageDescription>
        </DashboardPageHeader>
        <DashboardPageContent>
          <UserPermissionsContent user={user} />
        </DashboardPageContent>
      </DashboardPage>
    );
  } catch (error) {
    console.error('Error loading user:', error);
    notFound();
  }
}
