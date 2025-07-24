'use client';

import { useState } from 'react';
import { LogOut, Moon, Search, Settings, Sun, User } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { useTheme } from 'next-themes';
import { useSession, signOut } from 'next-auth/react';
import { NotificationDropdown } from '@/components/legacy/notifications';
import { SidebarTrigger } from '@/components/ui/sidebar';

interface DashboardHeaderProps {
  title?: string;
  subtitle?: string;
}

export function DashboardHeader({ title, subtitle }: DashboardHeaderProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const { theme, setTheme } = useTheme();
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
    <header className="sticky top-0 z-30 flex h-16 items-center gap-4 border-b border-slate-700/50 bg-slate-900/80 backdrop-blur-sm px-4 md:px-6">
      {/* Title Section */}
      <div className="flex-1">
        <SidebarTrigger className="text-slate-400 hover:text-white hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
        {title && (
          <div>
            <h1 className="text-lg font-semibold md:text-xl text-white">{title}</h1>
            {subtitle && <p className="text-sm text-slate-400">{subtitle}</p>}
          </div>
        )}
      </div>

      {/* Search Bar */}
      <form onSubmit={handleSearch} className="flex-1 max-w-md">
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
        {/* Theme Toggle */}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
          className="h-8 w-8 text-slate-400 hover:text-white hover:bg-slate-800/50"
        >
          <Sun className="h-4 w-4 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
          <Moon className="absolute h-4 w-4 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
          <span className="sr-only">Toggle theme</span>
        </Button>

        {/* Notifications */}
        <NotificationDropdown className="text-slate-400 hover:text-white hover:bg-slate-800/50" />

        {/* Settings */}
        <Button variant="ghost" size="sm" className="h-8 w-8 text-slate-400 hover:text-white hover:bg-slate-800/50">
          <Settings className="h-4 w-4" />
          <span className="sr-only">Settings</span>
        </Button>

        {/* User Menu */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="relative h-8 w-8 rounded-full hover:bg-slate-800/50">
              <Avatar className="h-8 w-8">
                {userImage ? (
                  <AvatarImage src={userImage} alt={userDisplayName} />
                ) : (
                  <AvatarFallback className="bg-slate-700 text-white">{userInitials}</AvatarFallback>
                )}
              </Avatar>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-56 bg-slate-800 border-slate-700" align="end" forceMount>
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-col space-y-1">
                <p className="text-sm font-medium leading-none text-white">{userDisplayName}</p>
                <p className="text-xs leading-none text-slate-400">{userEmail}</p>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator className="bg-slate-700" />
            <DropdownMenuItem className="text-white hover:bg-slate-700/50">
              <User className="mr-2 h-4 w-4" />
              <span>Profile</span>
            </DropdownMenuItem>
            <DropdownMenuItem className="text-white hover:bg-slate-700/50">
              <Settings className="mr-2 h-4 w-4" />
              <span>Settings</span>
            </DropdownMenuItem>
            <DropdownMenuSeparator className="bg-slate-700" />
            <DropdownMenuItem onClick={handleLogout} className="text-white hover:bg-slate-700/50">
              <LogOut className="mr-2 h-4 w-4" />
              <span>Log out</span>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
