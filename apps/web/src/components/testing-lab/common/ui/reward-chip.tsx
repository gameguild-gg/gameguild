"use client";
import React from 'react';

interface RewardChipProps {
    reward?: string | number | null;
    value?: string | number | null;
    variant?: 'default' | 'compact' | 'inline';
    className?: string;
}

export const RewardChip: React.FC<RewardChipProps> = ({ reward, value, variant = 'default', className }) => {
    const displayValue = reward ?? value;
    if (displayValue === undefined || displayValue === null || displayValue === '') return null;
    return (
        <span
            className={
                'inline-flex items-center rounded-full bg-amber-500/15 text-amber-600 dark:text-amber-400 px-2 py-0.5 text-xs font-medium ring-1 ring-inset ring-amber-500/30 ' +
                (className || '')
            }
        >
            {typeof displayValue === 'number' ? displayValue.toLocaleString() : displayValue}
        </span>
    );
};
