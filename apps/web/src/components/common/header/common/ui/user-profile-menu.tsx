'use client';

import React from 'react';
import Link from 'next/link';
import { LogOut, MoveUpRight } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { signOut } from 'next-auth/react';

interface MenuItem {
  label: string;
  value?: string;
  href: string;
  icon?: React.ReactNode;
  external?: boolean;
}

interface UserData {
  id: string;
  username: string;
  email?: string;
}

interface UserProfileMenuProps {
  user: UserData;
  menuItems?: MenuItem[];
}

export const UserProfileMenu = ({ user, menuItems = [] }: UserProfileMenuProps): React.JSX.Element => {
  const handleSignOut = async () => {
    await signOut({ redirect: true, redirectTo: '/' });
  };

  // Generate display name and avatar fallback
  const displayName = user.username || 'User';
  const avatarFallback = displayName?.charAt(0).toUpperCase() || user.email?.charAt(0).toUpperCase() || 'U';

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Avatar className="h-9 w-9 cursor-pointer">
          <AvatarFallback className="bg-zinc-800 text-zinc-200 font-medium">{avatarFallback}</AvatarFallback>
        </Avatar>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-full max-w-sm mx-auto p-0 border-zinc-800">
        <div className="relative overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
          <div className="relative px-6 pt-12 pb-6 bg-white dark:bg-zinc-900">
            <div className="flex items-center gap-4 mb-8">
              <div className="relative shrink-0">
                <div className="w-[72px] h-[72px] rounded-full ring-4 ring-white dark:ring-zinc-900 bg-zinc-800 flex items-center justify-center text-2xl font-bold text-zinc-200">
                  {avatarFallback}
                </div>
                <div className="absolute bottom-0 right-0 w-4 h-4 rounded-full bg-emerald-500 ring-2 ring-white dark:ring-zinc-900" />
              </div>

              <div className="flex-1">
                <h2 className="text-xl font-semibold text-zinc-900 dark:text-zinc-100">{displayName}</h2>
                <p className="text-zinc-600 dark:text-zinc-400">Game Developer</p>
                <p className="text-sm text-zinc-500 dark:text-zinc-500">{user.email}</p>
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
                onClick={handleSignOut}
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
};
