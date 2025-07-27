import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { BookOpen, DollarSign, TrendingUp, Users } from 'lucide-react';

export function DashboardQuickActions() {
  return (
    <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
      <CardHeader>
        <CardTitle className="text-base text-white">Quick Actions</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
          <Users className="mr-2 h-4 w-4" />
          Manage Users
        </Button>
        <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
          <BookOpen className="mr-2 h-4 w-4" />
          Course Management
        </Button>
        <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
          <DollarSign className="mr-2 h-4 w-4" />
          Revenue Reports
        </Button>
        <Button className="w-full justify-start bg-slate-700/50 text-white hover:bg-slate-600/50 border-slate-600" variant="outline">
          <TrendingUp className="mr-2 h-4 w-4" />
          Analytics
        </Button>
      </CardContent>
    </Card>
  );
}
