import React from 'react';
import Link from 'next/link';
import { Settings, User } from 'lucide-react';
import { auth } from '@/auth';
import { UserProfileMenu } from './user-profile-menu';

export const UserProfile = async (): Promise<React.JSX.Element> => {
  const session = await auth();

  // Return login button if not authenticated
  if (!session || !session.user) {
    return (
      <Link
        href="/sign-in"
        className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 bg-primary text-primary-foreground hover:bg-primary/90 h-9 px-3"
      >
        Sign In
      </Link>
    );
  }

  // Handle session errors
  if (session.error) {
    return (
      <Link
        href="/sign-in"
        className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 bg-destructive text-destructive-foreground hover:bg-destructive/90 h-9 px-3"
      >
        Sign In
      </Link>
    );
  }

  // Generate username for profile URL
  const profileUsername = session.user.username?.toLowerCase().replace(/\s+/g, '') || 'user';

  // Define menu items
  const menuItems = [
    {
      label: 'Profile',
      href: `/users/${profileUsername}`,
      icon: <User className="size-4" />,
    },
    {
      label: 'Settings',
      href: '/settings',
      icon: <Settings className="size-4" />,
    },
  ];

  return <UserProfileMenu session={session} menuItems={menuItems} />;
};
