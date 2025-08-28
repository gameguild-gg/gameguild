import React from 'react';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { LogOut, Settings, User } from 'lucide-react';
// Import auth directly from root auth module where NextAuth instance exports it
import { auth } from '@/auth';
import { ThemeToggle } from '@/components/theme';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { forceSignOut } from '@/lib/auth/auth.actions';

export const DashboardHeader = async (): Promise<React.JSX.Element> => {
  const session = await auth();

  // const [searchQuery, setSearchQuery] = useState('');
  // const { theme, setTheme } = useTheme();
  // const { data: session } = useSession();

  // const handleSearch = (e: React.FormEvent) => {
  //   e.preventDefault();
  //   // Implement search functionality
  //   console.log('Searching for:', searchQuery);
  // };

  const handleLogout = async () => {
    'use server';
    try {
      await forceSignOut();
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
        <div className="">
          <SidebarTrigger
            className="text-slate-400 hover:text-white hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
        </div>

        {/* Search Bar */}
        {/*<form onSubmit={handleSearch} className="max-w-md">*/}
        {/*  <div className="relative">*/}
        {/*    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />*/}
        {/*    <Input*/}
        {/*      type="search"*/}
        {/*      placeholder="Search users, courses..."*/}
        {/*      className="pl-8 md:w-[300px] lg:w-[400px] bg-slate-800/50 border-slate-700/50 text-white placeholder:text-slate-400 focus:border-blue-500/50 focus:ring-blue-500/20"*/}
        {/*      value={searchQuery}*/}
        {/*      onChange={(e) => setSearchQuery(e.target.value)}*/}
        {/*    />*/}
        {/*  </div>*/}
        {/*</form>*/}

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Theme Toggle */}
          <ThemeToggle />

          {/* User Menu */}
          {session?.user && (
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
                <form action={handleLogout}>
                  <DropdownMenuItem asChild>
                    <button type="submit" className="w-full text-white hover:bg-slate-700/50 flex items-center">
                      <LogOut className="mr-2 h-4 w-4" />
                      <span>Log out</span>
                    </button>
                  </DropdownMenuItem>
                </form>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>
    </header>
  );
};
