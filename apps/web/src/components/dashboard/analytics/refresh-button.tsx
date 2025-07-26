'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { RefreshCw } from 'lucide-react';
import { refreshDashboardData } from '@/lib/dashboard/refresh-actions';
import { useRouter } from 'next/navigation';

interface RefreshButtonProps {
  className?: string;
}

export function RefreshButton({ className }: RefreshButtonProps) {
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState<string | null>(null);
  const router = useRouter();

  const handleRefresh = () => {
    startTransition(async () => {
      try {
        // Refresh dashboard data using server action
        const result = await refreshDashboardData();

        if (result.success) {
          setMessage(result.message);
        } else {
          setMessage(result.message);
        }

        // Refresh the page to show updated data
        router.refresh();

        // Clear message after 3 seconds
        setTimeout(() => setMessage(null), 3000);
      } catch {
        setMessage('Failed to refresh data');
        setTimeout(() => setMessage(null), 3000);
      }
    });
  };

  return (
    <div className={`flex flex-col items-end gap-2 ${className}`}>
      <Button
        onClick={handleRefresh}
        disabled={isPending}
        variant="outline"
        size="sm"
        className="border-slate-600 text-white hover:bg-slate-800/50 hover:border-slate-500"
      >
        <RefreshCw className={`mr-2 h-4 w-4 ${isPending ? 'animate-spin' : ''}`} />
        {isPending ? 'Refreshing...' : 'Refresh Data'}
      </Button>

      {message && <span className="text-xs text-slate-400 animate-in fade-in-0 duration-300">{message}</span>}
    </div>
  );
}
