import { MapPin, Settings, Shield, Users } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

export default function TestingLabSettingsLayout({ children }: { children: React.ReactNode }) {
    const nav = [
        { href: './general', label: 'General', icon: Settings },
        { href: './collaborators', label: 'Collaborators', icon: Users },
        { href: './locations', label: 'Locations', icon: MapPin },
        { href: './roles', label: 'Roles', icon: Shield },
    ];
    return (
        <div className="flex min-h-[calc(100vh-4rem)]">
            <aside className="w-60 border-r bg-muted/30 p-4 sticky top-0 h-screen">
                <div className="mb-4 px-2">
                    <h2 className="text-sm font-semibold tracking-wide text-muted-foreground uppercase">Testing Lab</h2>
                    <p className="text-xs text-muted-foreground mt-1">Settings</p>
                </div>
                <nav className="space-y-1">
                    {nav.map(item => {
                        const Icon = item.icon;
                        return (
                            <Link key={item.href} href={item.href} className="flex items-center gap-2 px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors">
                                <Icon className="h-4 w-4" /> {item.label}
                            </Link>
                        );
                    })}
                </nav>
            </aside>
            <main className="flex-1 px-8 py-8 overflow-y-auto">{children}</main>
        </div>
    );
}
