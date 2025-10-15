import Link from 'next/link';
import { UserProfileMenu } from './user-profile-menu';
import { User, Settings } from 'lucide-react';
import type { Session } from 'next-auth';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';

interface UserProfileProps {
  session: Session | null;
  userProfile?: UserResponseDto | null;
}

export function UserProfile({ session, userProfile }: UserProfileProps) {
  if (!session?.user) {
    return (
      <div className="flex items-center space-x-2">
        <Link
          href="/api/auth/signin"
          className="rounded-md bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          Sign In
        </Link>
      </div>
    );
  }

  // Use actual username for profile URL instead of transforming the display name
  const profileUsername = userProfile?.username || userProfile?.name?.toLowerCase().replace(/\s+/g, '') || 'user';

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
}
