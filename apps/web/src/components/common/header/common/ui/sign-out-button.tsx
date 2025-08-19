'use client';

import { signOut } from 'next-auth/react';
import { DropdownMenuItem } from '@/components/ui/dropdown-menu';
import { LogOut } from 'lucide-react';

export function SignOutButton() {
  return (
    <DropdownMenuItem onClick={() => signOut()}>
      <LogOut className="mr-2 h-4 w-4" />
      Sign out
    </DropdownMenuItem>
  );
}