import { auth } from '@/auth';
import { BookOpen } from 'lucide-react';
import Link from 'next/link';
import { redirect } from 'next/navigation';
import React from 'react';

export default async function AuthLayout({ children }: { children: React.ReactNode }) {
    const session = await auth();

    if (session) {
        redirect('/');
    }

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-950 px-6 py-10">
            <div className="flex w-full max-w-md flex-col gap-6">
                <Link href="/" className="inline-flex items-center justify-center gap-2 text-sm font-medium text-slate-200 hover:text-white">
                    <BookOpen className="size-4 text-sky-300" />
                    Game Guild Learning
                </Link>
                {children}
            </div>
        </div>
    );
}
