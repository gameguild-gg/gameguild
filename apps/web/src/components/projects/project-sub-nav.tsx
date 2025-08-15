"use client";

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LayoutDashboard, Store, Sparkles, Download, MessageSquare, BookText, Trophy, Award, TestTube, Users } from 'lucide-react';
import { cn } from '@/lib/utils';

export function ProjectSubNav({ projectId }: { projectId: string }) {
  const pathname = usePathname();
  // Compute base path up to and including the slug so locale and parent segments are preserved
  const slugIndex = pathname.indexOf(`/${projectId}`);
  const basePath = slugIndex >= 0 ? pathname.slice(0, slugIndex + projectId.length + 1) : `/project/${projectId}`;

  const navItems = [
    { href: basePath, label: 'Overview', icon: LayoutDashboard },
    { href: `${basePath}/store-presence`, label: 'Store Presence', icon: Store },
    { href: `${basePath}/distribution`, label: 'Distribution', icon: Sparkles },
    { href: `${basePath}/versions`, label: 'Versions', icon: Download },
    { href: `${basePath}/achievements`, label: 'Achievements', icon: Award },
    { href: `${basePath}/testing`, label: 'Testing Sessions', icon: TestTube },
    { href: `${basePath}/feedback`, label: 'Feedback', icon: MessageSquare },
    { href: `${basePath}/devlog`, label: 'Devlog', icon: BookText },
    { href: `${basePath}/game-jams`, label: 'Game Jams', icon: Trophy },
    { href: `${basePath}/team`, label: 'Team', icon: Users },
  ];

  return (
    <aside className="w-56 flex-shrink-0">
      <nav className="flex flex-col gap-1 p-2">
        {navItems.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={cn(
              'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
              pathname === item.href && 'bg-muted text-foreground',
            )}
          >
            <item.icon className="h-4 w-4" />
            <span>{item.label}</span>
          </Link>
        ))}
      </nav>
    </aside>
  );
}
