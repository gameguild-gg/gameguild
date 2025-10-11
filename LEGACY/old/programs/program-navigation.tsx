'use client';

import { Button } from '@/components/ui/button';
import { ArrowLeft, BookOpen } from 'lucide-react';
import Link from 'next/link';

export function ProgramNavigation() {
  return (
    <div className="flex items-center gap-4 mb-6">
      <Button variant="ghost" size="sm" asChild>
        <Link href="/dashboard">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Dashboard
        </Link>
      </Button>
      <div className="w-px h-6 bg-gray-300"></div>
      <div className="flex items-center gap-2">
        <BookOpen className="h-5 w-5 text-blue-600" />
        <span className="font-medium">Programs & Courses</span>
      </div>
    </div>
  );
}
