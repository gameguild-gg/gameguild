import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Calendar, TestTube, TrendingUp, Users } from 'lucide-react';

interface TestingLabStatsProps {
  totalSessions: number;
  openSessions: number;
  upcomingSessions: number;
}

export function TestingLabStats({ totalSessions, openSessions, upcomingSessions }: TestingLabStatsProps) {
  return (
    <section className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm gap-2 py-4">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-0">
          <CardTitle className="text-sm font-medium text-slate-200">Total Sessions</CardTitle>
          <TestTube className="h-6 w-6 text-blue-400" />
        </CardHeader>
        <CardContent className="pt-0">
          <div className="text-4xl font-bold text-white">{totalSessions}</div>
          <p className="text-xs text-slate-400">Available this month</p>
        </CardContent>
      </Card>

      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm gap-2 py-4">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-0">
          <CardTitle className="text-sm font-medium text-slate-200">Open Now</CardTitle>
          <Users className="h-6 w-6 text-green-400" />
        </CardHeader>
        <CardContent className="pt-0">
          <div className="text-4xl font-bold text-white">{openSessions}</div>
          <p className="text-xs text-slate-400">Ready to join</p>
        </CardContent>
      </Card>

      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm gap-2 py-4">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-0">
          <CardTitle className="text-sm font-medium text-slate-200">Upcoming</CardTitle>
          <Calendar className="h-6 w-6 text-purple-400" />
        </CardHeader>
        <CardContent className="pt-0">
          <div className="text-4xl font-bold text-white">{upcomingSessions}</div>
          <p className="text-xs text-slate-400">This week</p>
        </CardContent>
      </Card>

      <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm gap-2 py-4">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-0">
          <CardTitle className="text-sm font-medium text-slate-200">Success Rate</CardTitle>
          <TrendingUp className="h-6 w-6 text-orange-400" />
        </CardHeader>
        <CardContent className="pt-0">
          <div className="text-4xl font-bold text-white">94%</div>
          <p className="text-xs text-slate-400">Completion rate</p>
        </CardContent>
      </Card>
    </section>
  );
}
