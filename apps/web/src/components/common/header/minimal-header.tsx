import React from 'react';

import { SidebarTrigger } from '@/components/ui/sidebar';
import { ThemeToggle } from '@/components/theme';
import { UserProfile } from '@/components/common/header/common/ui/user-profile';
import { auth } from '@/auth';
import { getCurrentUserProfile } from '@/lib/user-profile/user-profile.actions';

type MinimalHeaderProps = React.HTMLAttributes<HTMLElement>;

// A minimal header: dashboard-style layout + glassmorphism from default header + the shared UserProfile menu
export const MinimalHeader = async ({ className = '', ...props }: Readonly<MinimalHeaderProps>): Promise<React.JSX.Element> => {
  const session = await auth();
  let userProfile = null;
  
  if (session?.user?.id) {
    const userProfileResult = await getCurrentUserProfile();
    userProfile = userProfileResult.success ? userProfileResult.data : null;
  }
  return (
    <header
      className={
        'w-full bg-white/10 dark:bg-slate-900/20 backdrop-blur-md border-b border-white/10 dark:border-slate-700/30 text-white sticky top-0 z-50 shadow-lg ' +
        className
      }
      {...props}
    >
      {/* Top subtle gradient border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent" />

      <div className="container mx-auto px-4 flex justify-between items-center py-3">
        {/* Left: Sidebar trigger (like dashboard header) */}
        <div className="flex items-center gap-2">
          <SidebarTrigger className="text-slate-200 hover:text-white hover:bg-white/5 dark:hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
        </div>

        {/* Right: actions - theme + user profile */}
        <div className="flex items-center gap-3">
          <ThemeToggle />
          {/* UserProfile is a server component that renders Sign In or the profile menu */}
          <UserProfile session={session} userProfile={userProfile} />
        </div>
      </div>

      {/* Bottom subtle gradient border */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/20 dark:via-slate-400/30 to-transparent" />
    </header>
  );
};

export default MinimalHeader;
