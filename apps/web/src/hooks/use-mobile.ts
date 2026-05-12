'use client';

import * as React from 'react';

const MOBILE_BREAKPOINT = 768;

export function useIsMobile(): boolean {
    const [isMobile, setIsMobile] = React.useState(false);

    React.useEffect(() => {
        const mediaQuery = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`);

        const update = () => {
            setIsMobile(window.innerWidth < MOBILE_BREAKPOINT);
        };

        update();
        mediaQuery.addEventListener('change', update);

        return () => {
            mediaQuery.removeEventListener('change', update);
        };
    }, []);

    return isMobile;
}