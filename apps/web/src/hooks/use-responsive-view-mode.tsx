'use client';

import { useEffect, useState } from 'react';

export function useResponsiveViewMode() {
  const [isSmallScreen, setIsSmallScreen] = useState(false);

  useEffect(() => {
    // Only run on client side
    if (typeof window === 'undefined') return;

    const checkScreenSize = () => {
      setIsSmallScreen(window.innerWidth < 1024);
    };

    // Initial check
    checkScreenSize();

    // Add event listener
    window.addEventListener('resize', checkScreenSize);

    // Cleanup
    return () => window.removeEventListener('resize', checkScreenSize);
  }, []);

  return { isSmallScreen };
}
