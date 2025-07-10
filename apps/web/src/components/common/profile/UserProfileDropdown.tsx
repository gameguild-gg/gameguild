'use client';

import { LogOut, MoveUpRight, Settings, CreditCard, FileText, User } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { useSession, signOut } from 'next-auth/react';
import { useEffect, useState } from 'react';
import { getUsersMe } from '@/lib/api/generated';
import { createClient } from '@/lib/api/generated/client';
import { environment } from '@/configs/environment';
import { UserResponseDto } from '@/lib/api/generated/types.gen';

interface MenuItem {
  label: string;
  value?: string;
  href: string;
  icon?: React.ReactNode;
  external?: boolean;
}

interface UserData {
  id?: string;
  name?: string;
  email?: string;
  image?: string;
  displayName?: string;
  role?: string;
  subscription?: string;
}

export default function UserProfileDropdown() {
  const { data: session, status } = useSession();
  const [userData, setUserData] = useState<UserData | null>(null);
  const [loading, setLoading] = useState(true);

  // Fetch user data from CMS backend
  useEffect(() => {
    async function fetchUserData() {
      if (!session?.accessToken) {
        setLoading(false);
        return;
      }

      try {
        // Create authenticated API client
        const apiClient = createClient({
          baseUrl: environment.apiBaseUrl,
          headers: {
            Authorization: `Bearer ${session.accessToken}`,
            'Content-Type': 'application/json',
          },
        });

        // Get current user data from CMS
        const result = await getUsersMe({
          client: apiClient,
        });

        if (result && result.data) {
          const cmsUser = result.data as UserResponseDto;
          setUserData({
            id: cmsUser.id,
            name: cmsUser.name || session.user?.name || 'User',
            email: cmsUser.email || session.user?.email || '',
            image: session.user?.image || '',
            displayName: cmsUser.name || session.user?.name || 'User',
            role: 'Game Developer', // Default role, could be enhanced with role data from backend
            subscription: 'Free Trial', // Default subscription, could be enhanced with subscription data
          });
        }
      } catch (error) {
        console.error('Failed to fetch user data:', error);
        // Fallback to session data
        setUserData({
          id: session.user?.id,
          name: session.user?.name || 'User',
          email: session.user?.email || '',
          image: session.user?.image || '',
          displayName: session.user?.name || 'User',
          role: 'Game Developer',
          subscription: 'Free Trial',
        });
      } finally {
        setLoading(false);
      }
    }

    if (status !== 'loading') {
      fetchUserData();
    }
  }, [session, status]);

  // Handle logout
  const handleLogout = async () => {
    await signOut({
      callbackUrl: '/',
      redirect: true,
    });
  };

  // Return loading state if session or user data is loading
  if (status === 'loading' || loading) {
    return (
      <Avatar className="h-9 w-9">
        <AvatarFallback className="bg-zinc-800 text-zinc-200 font-medium animate-pulse">?</AvatarFallback>
      </Avatar>
    );
  }

  // Return login button if not authenticated
  if (!session) {
    return (
      <Link
        href="/sign-in"
        className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 bg-primary text-primary-foreground hover:bg-primary/90 h-9 px-3"
      >
        Sign In
      </Link>
    );
  }

  // Return null if session exists but userData failed to load
  if (!userData) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Avatar className="h-9 w-9 cursor-pointer">
            {session.user?.image && <AvatarImage src={session.user.image} alt={`${session.user.name || 'User'} avatar`} />}
            <AvatarFallback className="bg-zinc-800 text-zinc-200 font-medium">
              {session.user?.name?.charAt(0).toUpperCase() || session.user?.email?.charAt(0).toUpperCase() || 'U'}
            </AvatarFallback>
          </Avatar>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-full max-w-sm mx-auto p-0 border-zinc-800">
          <div className="relative overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
            <div className="relative px-6 pt-12 pb-6 bg-white dark:bg-zinc-900">
              <div className="flex items-center gap-4 mb-8">
                <div className="relative shrink-0">
                  {session.user?.image ? (
                    <Image
                      src={session.user.image}
                      alt={session.user.name || 'User'}
                      width={72}
                      height={72}
                      className="rounded-full ring-4 ring-white dark:ring-zinc-900 object-cover"
                    />
                  ) : (
                    <div className="w-[72px] h-[72px] rounded-full ring-4 ring-white dark:ring-zinc-900 bg-zinc-800 flex items-center justify-center text-2xl font-bold text-zinc-200">
                      {session.user?.name?.charAt(0).toUpperCase() || session.user?.email?.charAt(0).toUpperCase() || 'U'}
                    </div>
                  )}
                  <div className="absolute bottom-0 right-0 w-4 h-4 rounded-full bg-emerald-500 ring-2 ring-white dark:ring-zinc-900" />
                </div>

                {/* Profile Info */}
                <div className="flex-1">
                  <h2 className="text-xl font-semibold text-zinc-900 dark:text-zinc-100">{session.user?.name || 'User'}</h2>
                  <p className="text-zinc-600 dark:text-zinc-400">Game Developer</p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-500">{session.user?.email || ''}</p>
                </div>
              </div>
              <div className="h-px bg-zinc-200 dark:bg-zinc-800 my-6" />
              <div className="space-y-2">
                <button
                  type="button"
                  onClick={handleLogout}
                  className="w-full flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800/50 rounded-lg transition-colors duration-200"
                >
                  <div className="flex items-center gap-2">
                    <LogOut className="w-4 h-4" />
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">Logout</span>
                  </div>
                </button>
              </div>
            </div>
          </div>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  // Generate username from name for profile URL
  const username = userData.name?.toLowerCase().replace(/\s+/g, '') || 'user';

  const menuItems: MenuItem[] = [
    {
      label: 'Profile',
      href: `/users/${username}`,
      icon: <User className="w-4 h-4" />,
    },
    {
      label: 'Subscription',
      value: userData.subscription,
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

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Avatar className="h-9 w-9 cursor-pointer">
          <AvatarImage src={userData.image} alt={`${userData.name} avatar`} />
          <AvatarFallback className="bg-zinc-800 text-zinc-200 font-medium">{userData.name?.charAt(0).toUpperCase() || 'U'}</AvatarFallback>
        </Avatar>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-full max-w-sm mx-auto p-0 border-zinc-800">
        <div className="relative overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
          <div className="relative px-6 pt-12 pb-6 bg-white dark:bg-zinc-900">
            <div className="flex items-center gap-4 mb-8">
              <div className="relative shrink-0">
                {userData.image ? (
                  <Image
                    src={userData.image}
                    alt={userData.name || 'User'}
                    width={72}
                    height={72}
                    className="rounded-full ring-4 ring-white dark:ring-zinc-900 object-cover"
                  />
                ) : (
                  <div className="w-[72px] h-[72px] rounded-full ring-4 ring-white dark:ring-zinc-900 bg-zinc-800 flex items-center justify-center text-2xl font-bold text-zinc-200">
                    {userData.name?.charAt(0).toUpperCase() || 'U'}
                  </div>
                )}
                <div className="absolute bottom-0 right-0 w-4 h-4 rounded-full bg-emerald-500 ring-2 ring-white dark:ring-zinc-900" />
              </div>

              {/* Profile Info */}
              <div className="flex-1">
                <h2 className="text-xl font-semibold text-zinc-900 dark:text-zinc-100">{userData.displayName}</h2>
                <p className="text-zinc-600 dark:text-zinc-400">{userData.role}</p>
                <p className="text-sm text-zinc-500 dark:text-zinc-500">{userData.email}</p>
              </div>
            </div>
            <div className="h-px bg-zinc-200 dark:bg-zinc-800 my-6" />
            <div className="space-y-2">
              {menuItems.map((item) => (
                <Link
                  key={item.label}
                  href={item.href}
                  className="flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800/50 rounded-lg transition-colors duration-200"
                >
                  <div className="flex items-center gap-2">
                    {item.icon}
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">{item.label}</span>
                  </div>
                  <div className="flex items-center">
                    {item.value && <span className="text-sm text-zinc-500 dark:text-zinc-400 mr-2">{item.value}</span>}
                    {item.external && <MoveUpRight className="w-4 h-4" />}
                  </div>
                </Link>
              ))}

              <button
                type="button"
                onClick={handleLogout}
                className="w-full flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800/50 rounded-lg transition-colors duration-200"
              >
                <div className="flex items-center gap-2">
                  <LogOut className="w-4 h-4" />
                  <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">Logout</span>
                </div>
              </button>
            </div>
          </div>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
