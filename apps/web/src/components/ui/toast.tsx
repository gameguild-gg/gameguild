/**
 * Stub toast component for the UI library.
 * Using sonner for toast notifications instead.
 */

import * as React from 'react';

export interface ToastProps {
    variant?: 'default' | 'destructive';
    className?: string;
    children?: React.ReactNode;
    open?: boolean;
    onOpenChange?: (open: boolean) => void;
}

export interface ToastActionElement {
    altText: string;
    children: React.ReactNode;
    onClick?: () => void;
}

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

export function ToastAction({ children, onClick, altText }: ToastActionElement) {
    return (
        <button onClick={onClick} aria-label={altText}>
            {children}
        </button>
    );
}

export function ToastClose({ onClick }: { onClick?: () => void }) {
    return (
        <button onClick={onClick} aria-label="Close">
            ×
        </button>
    );
}

export function ToastViewport() {
    return <div className="fixed top-0 right-0 p-4 z-50" />;
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
    return <>{children}</>;
}

export const Toaster = () => null;
