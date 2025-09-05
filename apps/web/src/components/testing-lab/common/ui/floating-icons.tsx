'use client';

import { Bug, Compass, Cpu, FlaskConical, Rocket, Sparkles, Trophy } from 'lucide-react';
import React, { useMemo } from 'react';

/**
 * Decorative floating icons background for Testing Lab landing page.
 * Non-essential; hidden from assistive tech.
 */
export function FloatingIcons(): React.JSX.Element {
    // Small deterministic set; avoids layout shift & hydration mismatch.
    const items = useMemo(
        () => [
            { Icon: FlaskConical, className: 'top-10 left-8 text-orange-500/30 animate-pulse' },
            { Icon: Cpu, className: 'top-1/3 left-1/4 text-fuchsia-500/20 animate-[pulse_5s_ease-in-out_infinite]' },
            { Icon: Bug, className: 'top-1/2 right-12 text-red-500/25 animate-bounce' },
            { Icon: Rocket, className: 'bottom-24 left-12 text-sky-500/25 animate-pulse' },
            { Icon: Compass, className: 'top-20 right-1/4 text-emerald-500/25 animate-[pulse_7s_linear_infinite]' },
            { Icon: Trophy, className: 'bottom-16 right-8 text-amber-400/30 animate-bounce' },
            { Icon: Sparkles, className: 'top-1/2 left-1/2 text-purple-400/30 animate-pulse' }
        ],
        []
    );

    return (
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden select-none">
            {items.map(({ Icon, className }, i) => (
                <Icon
                    key={i}
                    className={
                        'absolute h-10 w-10 md:h-14 md:w-14 drop-shadow transition-opacity duration-700 ' +
                        className
                    }
                />
            ))}
            {/* Subtle radial vignette */}
            <div className="absolute inset-0 bg-radial from-transparent via-transparent to-background/80" />
        </div>
    );
}
