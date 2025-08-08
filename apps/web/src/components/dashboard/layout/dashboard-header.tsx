'use client';

import { useState, useEffect } from 'react';
import { Laptop, LogOut, Moon, Search, Settings, Sun, User } from 'lucide-react';
import { UserProfileMenu } from '@/components/common/header/common/ui/user-profile-menu';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { useTheme } from 'next-themes';
import { signOut, useSession } from 'next-auth/react';
import { NotificationDropdown } from '@/components/legacy/notifications';
import { SidebarTrigger } from '@/components/ui/sidebar';

interface DashboardHeaderProps {
  title?: string;
  subtitle?: string;
}

export function DashboardHeader({ title, subtitle }: DashboardHeaderProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);
  
  // Only show theme toggle client-side to prevent hydration mismatch
  useEffect(() => {
    setMounted(true);
  }, []);
  const { data: session } = useSession();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // Implement search functionality
    console.log('Searching for:', searchQuery);
  };

  const handleLogout = async () => {
    try {
      await signOut({ callbackUrl: '/sign-in' });
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  // Get user display information from session
  const userDisplayName = session?.user?.name || 'Unknown User';
  const userEmail = session?.user?.email || 'unknown@email.com';
  const userImage = session?.user?.image;
  const userInitials = userDisplayName
    .split(' ')
    .map((name) => name[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  return (
    <header className="sticky top-0 z-50 flex flex-row h-16 items-center border-b backdrop-blur-sm gap-4 px-4 md:px-6">
      <div className="flex flex-1 justify-between content-between items-center gap-4">
        {/* Title Section */}
        <div className="">
          <SidebarTrigger className="text-slate-400 hover:text-white hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
          {title && (
            <div>
              <h1 className="text-lg font-semibold md:text-xl text-white">{title}</h1>
              {subtitle && <p className="text-sm text-slate-400">{subtitle}</p>}
            </div>
          )}
        </div>

        {/* Search Bar */}
        <form onSubmit={handleSearch} className="max-w-md">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              type="search"
              placeholder="Search users, courses..."
              className="pl-8 md:w-[300px] lg:w-[400px] bg-slate-800/50 border-slate-700/50 text-white placeholder:text-slate-400 focus:border-blue-500/50 focus:ring-blue-500/20"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
        </form>

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Theme Toggle Dropdown */}
          {mounted && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-8 w-8 text-slate-400 hover:text-white hover:bg-slate-800/50">
                  {theme === 'light' && <Sun className="h-4 w-4" />}
                  {theme === 'dark' && <Moon className="h-4 w-4" />}
                  {theme === 'system' && <Laptop className="h-4 w-4" />}
                  <span className="sr-only">Toggle theme</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-40 bg-slate-800 border-slate-700" align="end">
                <DropdownMenuItem 
                  onClick={() => setTheme('light')} 
                  className="text-white hover:bg-slate-700/50"
                >
                  <Sun className="mr-2 h-4 w-4" />
                  <span>Light</span>
                </DropdownMenuItem>
                <DropdownMenuItem 
                  onClick={() => setTheme('dark')} 
                  className="text-white hover:bg-slate-700/50"
                >
                  <Moon className="mr-2 h-4 w-4" />
                  <span>Dark</span>
                </DropdownMenuItem>
                <DropdownMenuItem 
                  onClick={() => setTheme('system')} 
                  className="text-white hover:bg-slate-700/50"
                >
                  <Laptop className="mr-2 h-4 w-4" />
                  <span>System</span>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}

          {/* Notifications */}
          <NotificationDropdown className="text-slate-400 hover:text-white hover:bg-slate-800/50" />

          {/*/!* Settings *!/*/}
          {/*<Button variant="ghost" size="sm" className="h-8 w-8 text-slate-400 hover:text-white hover:bg-slate-800/50">*/}
          {/*  <Settings className="h-4 w-4" />*/}
          {/*  <span className="sr-only">Settings</span>*/}
          {/*</Button>*/}

          {/* User Menu */}
          {session ? (
            <UserProfileMenu 
              session={{
                ...session,
                user: {
                  ...session.user,
                  username: session.user?.name || 'User'
                }
              }} 
              menuItems={[
                {
                  label: 'Profile',
                  href: `/users/${session.user?.name?.toLowerCase().replace(/\s+/g, '') || 'user'}`,
                  icon: <User className="size-4" />,
                },
                {
                  label: 'Settings',
                  href: '/settings',
                  icon: <Settings className="size-4" />,
                },
              ]} 
            />
          ) : (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-8 w-8 rounded-full hover:bg-slate-800/50">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback className="bg-slate-700 text-white">U</AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-56 bg-slate-800 border-slate-700" align="end">
                <DropdownMenuItem asChild className="text-white hover:bg-slate-700/50">
                  <Link href="/sign-in">Sign In</Link>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>
    </header>
  );
}
