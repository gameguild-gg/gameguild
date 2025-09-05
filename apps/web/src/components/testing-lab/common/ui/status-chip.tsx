"use client";
import React from 'react';

export type SessionStatus = 'draft' | 'scheduled' | 'active' | 'completed' | 'cancelled';

interface StatusChipProps {
    status?: SessionStatus | number | null;
    className?: string;
}

const STATUS_MAP: Record<string, { label: string; color: string }> = {
    draft: { label: 'Draft', color: 'bg-gray-500/15 text-gray-600 ring-gray-500/30 dark:text-gray-300' },
    scheduled: { label: 'Scheduled', color: 'bg-blue-500/15 text-blue-600 ring-blue-500/30 dark:text-blue-400' },
    active: { label: 'Active', color: 'bg-green-500/15 text-green-600 ring-green-500/30 dark:text-green-400' },
    completed: { label: 'Completed', color: 'bg-zinc-500/15 text-zinc-600 ring-zinc-500/30 dark:text-zinc-300' },
    cancelled: { label: 'Cancelled', color: 'bg-red-500/15 text-red-600 ring-red-500/30 dark:text-red-400' },
    // numeric fallbacks
    '0': { label: 'Draft', color: 'bg-gray-500/15 text-gray-600 ring-gray-500/30 dark:text-gray-300' },
    '1': { label: 'Scheduled', color: 'bg-blue-500/15 text-blue-600 ring-blue-500/30 dark:text-blue-400' },
    '2': { label: 'Active', color: 'bg-green-500/15 text-green-600 ring-green-500/30 dark:text-green-400' },
    '3': { label: 'Completed', color: 'bg-zinc-500/15 text-zinc-600 ring-zinc-500/30 dark:text-zinc-300' },
    '4': { label: 'Cancelled', color: 'bg-red-500/15 text-red-600 ring-red-500/30 dark:text-red-400' },
};

export const StatusChip: React.FC<StatusChipProps> = ({ status, className }) => {
    if (status === undefined || status === null) return null;
    const key = String(status).toLowerCase();
    const cfg = STATUS_MAP[key] ?? STATUS_MAP['draft'];
    return (
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${(cfg!).color} ${className || ''}`}>
            {(cfg!).label}
        </span>
    );
};
