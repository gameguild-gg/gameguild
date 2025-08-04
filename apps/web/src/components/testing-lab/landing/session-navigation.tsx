'use client';

import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';

export function SessionNavigation() {
  return (
    <div className="flex items-center gap-4 mb-8">
      <Button asChild variant="ghost" className="text-slate-400 hover:text-white">
        <Link href="/testing-lab">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Testing Lab
        </Link>
      </Button>
    </div>
  );
}
