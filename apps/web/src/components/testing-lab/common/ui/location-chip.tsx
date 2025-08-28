"use client";
import React from 'react';

interface LocationChipProps {
    locationName?: string | null;
    className?: string;
}

export const LocationChip: React.FC<LocationChipProps> = ({ locationName, className }) => {
    if (!locationName) return null;
    return (
        <span
            className={
                'inline-flex items-center rounded-full bg-violet-500/15 text-violet-600 dark:text-violet-400 px-2 py-0.5 text-xs font-medium ring-1 ring-inset ring-violet-500/30 ' +
                (className || '')
            }
        >
            {locationName}
        </span>
    );
};
