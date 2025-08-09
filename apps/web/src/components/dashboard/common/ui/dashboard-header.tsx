import React from 'react';

import { auth } from '@/auth';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { ThemeToggle } from '@/components/theme';
import { UserProfile } from '@/components/common/header/common/ui/user-profile';

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

  // const handleLogout = async () => {
  //   try {
  //     await signOut({ callbackUrl: '/sign-in' });
  //   } catch (error) {
  //     console.error('Logout failed:', error);
  //   }
  // };

  // Session is handled by UserProfile component

  return (
    <header className="sticky top-0 z-50 flex flex-row h-16 items-center border-b backdrop-blur-sm gap-4 px-4 md:px-6">
      <div className="flex flex-1 justify-between content-between items-center gap-4">
        <div className="">
          <SidebarTrigger className="text-slate-400 hover:text-white hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
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

          {/* User Profile */}
          <UserProfile />
        </div>
      </div>
    </header>
  );
};
