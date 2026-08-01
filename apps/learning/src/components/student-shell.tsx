'use client';

import { useAuth } from '@game-guild/client/react';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Button } from '@game-guild/ui/components/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import { Award, BookOpen, CalendarDays, GraduationCap, Library, LogOut, Menu, Search, X } from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import type { ReactNode } from 'react';
import { useState } from 'react';

export interface StudentShellUser { id: string; name: string; email: string; image?: string | null }

const navigation = [
    { href: '/', label: 'My learning', icon: GraduationCap },
    { href: '/catalog', label: 'Catalog', icon: Library },
    { href: '/calendar', label: 'Calendar', icon: CalendarDays },
    { href: '/grades', label: 'Grades', icon: BookOpen },
    { href: '/certificates', label: 'Certificates', icon: Award },
];

function initials(name: string) {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase() || 'GG';
}

export function StudentShell({ user, children }: { user: StudentShellUser; children: ReactNode }) {
    const pathname = usePathname();
    const { signOut, isLoading } = useAuth();
    const [mobileOpen, setMobileOpen] = useState(false);
    const [signingOut, setSigningOut] = useState(false);

    const handleSignOut = async () => {
        setSigningOut(true);
        try {
            await signOut({ redirectTo: '/sign-in' });
        } finally {
            setSigningOut(false);
        }
    };

    return (
        <div className="min-h-screen bg-[#0c0d10] text-slate-100">
            <header className="sticky top-0 z-40 flex h-16 items-center border-b border-white/10 bg-[#101114]/95 px-4 backdrop-blur lg:pl-72">
                <Button variant="ghost" size="icon" className="mr-2 lg:hidden" onClick={() => setMobileOpen((open) => !open)} aria-label="Toggle navigation">
                    <Menu className="size-5" />
                </Button>
                <Link href="/" className="mr-auto flex items-center gap-2 font-semibold lg:hidden">
                    <span className="flex size-8 items-center justify-center rounded-md bg-violet-600"><GraduationCap className="size-4" /></span>
                    Game Guild Learning
                </Link>
                <Button asChild variant="outline" className="mr-3 hidden w-full max-w-md justify-start border-white/10 bg-white/[0.03] text-slate-400 md:flex">
                    <Link href="/catalog"><Search className="size-4" />Search courses and learning</Link>
                </Button>
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="ghost" className="gap-2 px-2" aria-label="Open account menu">
                            <Avatar size="sm">{user.image ? <AvatarImage src={user.image} alt={user.name} /> : null}<AvatarFallback>{initials(user.name)}</AvatarFallback></Avatar>
                            <span className="hidden max-w-36 truncate text-sm font-medium sm:inline">{user.name}</span>
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-64">
                        <DropdownMenuLabel className="font-normal"><p className="truncate text-sm font-medium">{user.name}</p><p className="truncate text-xs text-muted-foreground">{user.email}</p></DropdownMenuLabel>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem disabled={isLoading || signingOut} onSelect={(event) => { event.preventDefault(); void handleSignOut(); }}>
                            <LogOut className="size-4" />{signingOut ? 'Signing out...' : 'Sign out'}
                        </DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </header>

            <aside className={`${mobileOpen ? 'flex' : 'hidden'} fixed inset-y-0 left-0 z-50 w-64 flex-col border-r border-white/10 bg-[#111216] p-4 lg:flex`}>
                <div className="mb-8 flex h-10 items-center gap-2">
                    <Link href="/" onClick={() => setMobileOpen(false)} className="flex min-w-0 flex-1 items-center gap-3 px-2 text-sm font-semibold">
                        <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-violet-600"><GraduationCap className="size-5" /></span>
                        <span className="truncate">Game Guild Learning</span>
                    </Link>
                    <Button variant="ghost" size="icon" className="shrink-0 lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Close navigation">
                        <X className="size-5" />
                    </Button>
                </div>
                <nav aria-label="Learner navigation" className="space-y-1">
                    {navigation.map(({ href, label, icon: Icon }) => {
                        const active = href === '/' ? pathname === '/' : pathname.startsWith(href);
                        return <Link key={href} href={href} aria-current={active ? 'page' : undefined} onClick={() => setMobileOpen(false)} className={`flex h-10 items-center gap-3 rounded-md px-3 text-sm ${active ? 'bg-white/10 text-white' : 'text-slate-400 hover:bg-white/[0.06] hover:text-white'}`}><Icon className="size-4" />{label}</Link>;
                    })}
                </nav>
                <div className="mt-auto border-t border-white/10 pt-4 text-xs text-slate-500">Learn, build, and share with the community.</div>
            </aside>
            {mobileOpen ? <button className="fixed inset-0 z-40 bg-black/60 lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Dismiss navigation" /> : null}
            <main className="min-w-0 lg:pl-64"><div className="mx-auto w-full max-w-[1600px] p-4 sm:p-6 lg:p-8">{children}</div></main>
        </div>
    );
}