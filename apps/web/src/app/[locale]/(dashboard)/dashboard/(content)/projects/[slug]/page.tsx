"use client";

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Activity, AlertCircle, BookText, CheckCircle2, Download, Eye, MessageSquare, TrendingUp } from 'lucide-react';

export default function ProjectOverviewPage(): React.JSX.Element {
  // Placeholder content until wired to real data
  const readyPct = 60;
  const recentActivity: Array<{ id: string; type: 'devlog' | 'feedback'; title?: string; author?: string; comment?: string; content?: string; createdAt: number }> = [];

  return (
    <div className="grid gap-8">
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Visibility" value={"public"} icon={Eye} subtext={<Badge variant="outline" className="capitalize bg-emerald-500/10 text-emerald-400 border-emerald-500/20">public</Badge>} />
        <StatCard title="Status" value={"beta"} icon={CheckCircle2} subtext="Ready for players" trend="+12%" />
        <StatCard title="Revenue (30d)" value="$0.00" icon={TrendingUp} subtext={<span className="text-sm font-medium text-muted-foreground">No data</span>} />
        <StatCard title="Downloads (30d)" value="0" icon={Download} subtext="No recent downloads" trend="0%" />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2 dark-card">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-muted-foreground" />
              <CardTitle>Recent Activity</CardTitle>
            </div>
            <CardDescription>A log of the latest updates and feedback for your project.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-6">
              {recentActivity.length > 0 ? (
                recentActivity.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-start gap-4 p-4 rounded-lg bg-muted/20 border border-border/30">
                    <div className="p-2 bg-primary/10 rounded-full">
                      {item.type === 'devlog' ? <BookText className="h-4 w-4 text-primary" /> : <MessageSquare className="h-4 w-4 text-primary" />}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-foreground">{item.type === 'devlog' ? item.title : `Feedback from ${item.author}`}</p>
                      <p className="text-sm text-muted-foreground line-clamp-1 mt-1">{item.type === 'devlog' ? item.content : `"${item.comment}"`}</p>
                    </div>
                    <p className="text-xs text-muted-foreground whitespace-nowrap">{new Date(item.createdAt).toLocaleDateString()}</p>
                  </div>
                ))
              ) : (
                <div className="text-center py-12">
                  <div className="p-3 bg-muted/20 rounded-full w-fit mx-auto mb-4">
                    <Activity className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <p className="text-sm text-muted-foreground">No recent activity.</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Release Checklist</CardTitle>
            <CardDescription>Steps to get your page ready for the public.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <ChecklistItem label="Set a title" done={false} />
            <ChecklistItem label="Upload a cover image" done={false} />
            <ChecklistItem label="Write a description" done={false} />
            <ChecklistItem label="Select platforms" done={false} />
            <ChecklistItem label="Upload a build" done={false} />
            <div className="text-xs text-muted-foreground">Progress: {readyPct}%</div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function StatCard({ title, value, icon: Icon, subtext, trend }: { title: string; value: string; icon: any; subtext?: React.ReactNode; trend?: string }) {
  return (
    <Card className="dark-card">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="flex items-baseline gap-2">
          <div className="text-2xl font-bold capitalize text-foreground">{value}</div>
          {trend && <span className="text-xs text-emerald-400 font-medium">{trend}</span>}
        </div>
        {subtext && <div className="mt-2">{subtext}</div>}
      </CardContent>
    </Card>
  );
}

function ChecklistItem({ label, done }: { label: string; done: boolean }) {
  return (
    <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/10 border border-border/30">
      {done ? <CheckCircle2 className="h-5 w-5 text-emerald-400 flex-shrink-0" /> : <AlertCircle className="h-5 w-5 text-amber-400 flex-shrink-0" />}
      <span className={done ? 'text-foreground' : 'text-muted-foreground'}>{label}</span>
    </div>
  );
}

