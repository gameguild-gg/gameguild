import React from 'react';
import Link from 'next/link';
import { CreditCard, FileText, Settings, User } from 'lucide-react';
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

  // Prepare user data for the client component
  const userData = {
    id: session.user.id,
    username: session.user.username,
    email: session.user.email,
  };

  // Generate username for profile URL
  const profileUsername = userData.username?.toLowerCase().replace(/\s+/g, '') || 'user';

  // Define menu items
  const menuItems = [
    {
      label: 'Profile',
      href: `/users/${profileUsername}`,
      icon: <User className="w-4 h-4" />,
    },
    {
      label: 'Subscription',
      href: '/subscription',
      icon: <CreditCard className="w-4 h-4" />,
      external: false,
    },
    {
      label: 'Settings',
      href: '/settings',
      icon: <Settings className="w-4 h-4" />,
    },
    {
      label: 'Terms & Policies',
      href: '/terms',
      icon: <FileText className="w-4 h-4" />,
      external: true,
    },
  ];

  return <UserProfileMenu user={userData} menuItems={menuItems} />;
};
