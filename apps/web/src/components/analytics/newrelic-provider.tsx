'use client';

import { initNewRelic } from '@/lib/newrelic';
import { useEffect } from 'react';

export function NewRelicProvider({ children }: { children: React.ReactNode }) {
    useEffect(() => {
        // Initialize New Relic on client side
        try {
            initNewRelic();
        } catch (error) {
            console.warn('New Relic Provider: Failed to initialize:', error);
        }
    }, []);

    return <>{children}</>;
} 