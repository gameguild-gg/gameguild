import '@/styles/globals.css';
import React from 'react';
import { Providers } from '@/components/providers';

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="en" suppressHydrationWarning>
            <body className="min-h-screen bg-slate-950 text-white antialiased">
                <Providers>{children}</Providers>
            </body>
        </html>
    );
}
