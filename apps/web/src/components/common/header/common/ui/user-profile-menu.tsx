'use client';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Separator } from '@/components/ui/separator';
import { LogOut, MoveUpRight } from 'lucide-react';
import type { Session } from 'next-auth';
import { signOut } from 'next-auth/react';
import Link from 'next/link';
import React from 'react';

interface MenuItem {
  label: string;
  value?: string;
  href: string;
  icon?: React.ReactNode;
  external?: boolean;
}

interface UserProfileMenuProps {
  session: Session;
  menuItems?: MenuItem[];
}

export const UserProfileMenu = ({ session, menuItems = [] }: UserProfileMenuProps): React.JSX.Element => {
  const handleSignOut = async () => {
    await signOut({ redirect: true, redirectTo: '/' });
  };

  // Generate display name and avatar fallback
  const displayName = session.user.username || 'User';
  const avatarFallback = displayName?.charAt(0).toUpperCase() || session.user.email?.charAt(0).toUpperCase() || 'U';
  const userImage = session.user.profilePictureUrl;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Avatar className="h-9 w-9 cursor-pointer">
          {userImage && <AvatarImage src={userImage} alt={displayName} />}
          <AvatarFallback className="bg-zinc-800 text-zinc-200 font-medium">{avatarFallback}</AvatarFallback>
        </Avatar>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-full max-w-sm mx-auto p-0 border-zinc-800">
        <div className="relative overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
          <div className="flex flex-col relative p-4 bg-white dark:bg-zinc-900 gap-4">
            <div className="flex items-center gap-4">
              <div className="relative shrink-0">
                {userImage ? (
                  <div className="w-[72px] h-[72px] rounded-full ring-4 ring-white dark:ring-zinc-900 overflow-hidden">
                    <img src={userImage} alt={displayName} className="w-full h-full object-cover" />
                  </div>
                ) : (
                  <div className="w-[72px] h-[72px] rounded-full ring-4 ring-white dark:ring-zinc-900 bg-zinc-800 flex items-center justify-center text-2xl font-bold text-zinc-200">{avatarFallback}</div>
                )}
                <div className="absolute bottom-0 right-0 size-4 rounded-full bg-emerald-500 ring-2 ring-white dark:ring-zinc-900" />
              </div>

              <div className="flex flex-col flex-1">
                <h2 className="text-xl font-semibold text-zinc-900 dark:text-zinc-100">{displayName}</h2>
                <p className="text-zinc-600 dark:text-zinc-400">Game Developer</p>
                <p className="text-sm text-zinc-500 dark:text-zinc-500">{session.user.email}</p>
              </div>
            </div>

            {/* Tenant Switcher */}

            {/*<TenantSwitcherDropdown className="w-full" currentTenant={session.currentTenant} availableTenants={session.availableTenants || []} />*/}
            <Separator className="my-1" />
            <div className="space-y-">
              {menuItems.map((item) => (
                <Link key={item.label} href={item.href} className="flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800/50 rounded-lg transition-colors duration-200">
                  <div className="flex items-center gap-2">
                    {item.icon}
                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">{item.label}</span>
                  </div>
                  <div className="flex items-center">
                    {item.value && <span className="text-sm text-zinc-500 dark:text-zinc-400 mr-2">{item.value}</span>}
                    {item.external && <MoveUpRight className="size-4" />}
                  </div>
                </Link>
              ))}

              <button type="button" onClick={handleSignOut} className="w-full flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800/50 rounded-lg transition-colors duration-200">
                <div className="flex items-center gap-2">
                  <LogOut className="size-4" />
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
