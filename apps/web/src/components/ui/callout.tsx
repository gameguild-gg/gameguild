'use client';

import React from 'react';
import { cn } from '@/lib/utils';
import { AlertTriangle, Info, CheckCircle, XCircle } from 'lucide-react';

export type CalloutType = 'info' | 'warning' | 'success' | 'error';

export interface CalloutProps {
  type?: CalloutType;
  title?: string;
  children?: React.ReactNode;
  className?: string;
}

const calloutIcons = {
  info: Info,
  warning: AlertTriangle,
  success: CheckCircle,
  error: XCircle,
};

const calloutStyles = {
  info: 'border-blue-200 bg-blue-50 text-blue-800',
  warning: 'border-yellow-200 bg-yellow-50 text-yellow-800',
  success: 'border-green-200 bg-green-50 text-green-800',
  error: 'border-red-200 bg-red-50 text-red-800',
};

export function Callout({ type = 'info', title, children, className }: CalloutProps) {
  const Icon = calloutIcons[type];

  return (
    <div className={cn('rounded-lg border p-4', calloutStyles[type], className)}>
      <div className="flex items-start gap-3">
        <Icon className="h-5 w-5 mt-0.5 flex-shrink-0" />
        <div className="flex-1">
          {title && <h4 className="font-medium mb-1">{title}</h4>}
          {children && <div className="text-sm">{children}</div>}
        </div>
      </div>
    </div>
  );
}
