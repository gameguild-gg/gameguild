import type { ReactNode } from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface LoadingProps {
  readonly children?: ReactNode;
  readonly className?: string;
  readonly size?: 'sm' | 'md' | 'lg';
  readonly text?: string;
}

const sizeClasses = {
  sm: 'h-4 w-4',
  md: 'h-8 w-8',
  lg: 'h-12 w-12',
} as const;

export function Loading({ children, className, size = 'md', text = 'Loading...' }: LoadingProps) {
  return (
    <div className={cn('flex items-center justify-center p-8', className)}>
      <div className="flex flex-col items-center space-y-4">
        <Loader2 className={cn('animate-spin text-blue-400', sizeClasses[size])} />
        {children ? <div className="text-sm text-gray-400">{children}</div> : text ? <div className="text-sm text-gray-400">{text}</div> : null}
      </div>
    </div>
  );
}

// Loading skeleton for course cards
export function CourseCardSkeleton() {
  return (
    <div className="bg-gray-800 border border-gray-700 rounded-lg p-6 animate-pulse">
      <div className="aspect-video bg-gray-700 rounded-lg mb-4" />
      <div className="space-y-3">
        <div className="h-4 bg-gray-700 rounded w-3/4" />
        <div className="h-3 bg-gray-700 rounded w-full" />
        <div className="h-3 bg-gray-700 rounded w-2/3" />
      </div>
    </div>
  );
}

// Full page loading component
export function PageLoading() {
  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center">
      <Loading size="lg" text="Loading course details..." />
    </div>
  );
}
