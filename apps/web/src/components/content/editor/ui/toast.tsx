/**
 * Stub toast component for content editor.
 */

import * as React from 'react';

export interface ToastProps {
    variant?: 'default' | 'destructive';
    className?: string;
    children?: React.ReactNode;
    open?: boolean;
    onOpenChange?: (open: boolean) => void;
}

export type ToastActionElement = React.ReactElement;

export function Toast({ children, className, variant }: ToastProps) {
    return (
        <div className={className} data-variant={variant}>
            {children}
        </div>
    );
}

export function ToastTitle({ children }: { children: React.ReactNode }) {
    return <div className="font-semibold">{children}</div>;
}

export function ToastDescription({ children }: { children: React.ReactNode }) {
    return <div className="text-sm text-slate-500">{children}</div>;
}

export const Toaster = () => null;
