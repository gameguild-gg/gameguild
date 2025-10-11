'use client';

import { useState, useTransition } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import { Calendar, Filter } from 'lucide-react';

export function DashboardFilters() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();

  const [fromDate, setFromDate] = useState(searchParams.get('fromDate') || '');
  const [toDate, setToDate] = useState(searchParams.get('toDate') || '');
  const [includeDeleted, setIncludeDeleted] = useState(searchParams.get('includeDeleted') === 'true');

  const handleApplyFilters = () => {
    startTransition(() => {
      const params = new URLSearchParams(searchParams);

      if (fromDate) {
        params.set('fromDate', fromDate);
      } else {
        params.delete('fromDate');
      }

      if (toDate) {
        params.set('toDate', toDate);
      } else {
        params.delete('toDate');
      }

      if (includeDeleted) {
        params.set('includeDeleted', 'true');
      } else {
        params.delete('includeDeleted');
      }

      router.push(`?${params.toString()}`);
    });
  };

  const handleClearFilters = () => {
    startTransition(() => {
      setFromDate('');
      setToDate('');
      setIncludeDeleted(false);
      router.push('/dashboard/overview');
    });
  };

  return (
    <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
      <CardHeader>
        <CardTitle className="text-base text-white flex items-center gap-2">
          <Filter className="h-4 w-4" />
          Filter Statistics
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="fromDate" className="text-sm text-slate-300">
              From Date
            </Label>
            <Input
              id="fromDate"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="bg-slate-700/50 border-slate-600 text-white"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="toDate" className="text-sm text-slate-300">
              To Date
            </Label>
            <Input id="toDate" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} className="bg-slate-700/50 border-slate-600 text-white" />
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <Checkbox
            id="includeDeleted"
            checked={includeDeleted}
            onCheckedChange={(checked) => setIncludeDeleted(checked === true)}
            className="border-slate-600"
          />
          <Label htmlFor="includeDeleted" className="text-sm text-slate-300 cursor-pointer">
            Include deleted users in statistics
          </Label>
        </div>

        <div className="flex gap-2 pt-2">
          <Button onClick={handleApplyFilters} disabled={isPending} className="flex-1 bg-blue-600 hover:bg-blue-700 text-white">
            <Calendar className="mr-2 h-4 w-4" />
            {isPending ? 'Applying...' : 'Apply Filters'}
          </Button>

          <Button onClick={handleClearFilters} disabled={isPending} variant="outline" className="border-slate-600 text-white hover:bg-slate-800/50">
            Clear
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
