"use client"

import { ChecklistItem } from "@/components/projects/checklist-item"
import { useProject } from "@/components/projects/project-context"
import { StatCard } from "@/components/projects/stat-card"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { cn } from "@/lib/utils"
import {
  Activity,
  BookText,
  CheckCircle2,
  DollarSign,
  Download,
  Eye,
  MessageSquare,
  TrendingUp,
} from "lucide-react"

export default function ProjectOverviewPage(): React.JSX.Element {
  const project = useProject()

  const readiness = {
    hasTitle: !!project.title?.trim(),
    hasCover: !!project.coverUrl,
    hasDescription: !!project.description && project.description.trim().length > 30,
    hasPlatforms: !!(project.platforms && project.platforms.length > 0),
    hasBuild: !!(project.versions && project.versions.length > 0),
  }
  const readyCount = Object.values(readiness).filter(Boolean).length
  const readyPct = Math.round((readyCount / Object.keys(readiness).length) * 100)

  // Mock recent activity - in real app this would come from the project data
  const recentActivity: Array<{
    id: string;
    type: "devlog" | "feedback";
    title?: string;
    author?: string;
    comment?: string;
    content?: string;
    createdAt: number;
  }> = []

  const getStatusDisplay = (visibility: string): { label: string; color: string } => {
    const statusMap: Record<string, { label: string; color: string }> = {
      'draft': { label: 'draft', color: 'bg-slate-500/10 text-slate-400 border-slate-500/20' },
      'public': { label: 'public', color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' },
      'unlisted': { label: 'unlisted', color: 'bg-amber-500/10 text-amber-400 border-amber-500/20' },
      'private': { label: 'private', color: 'bg-red-500/10 text-red-400 border-red-500/20' }
    }
    return statusMap[visibility] || { label: 'draft', color: 'bg-slate-500/10 text-slate-400 border-slate-500/20' }
  }

  const statusInfo = getStatusDisplay(project.visibility)

  return (
    <div className="grid gap-8">
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Visibility"
          value={statusInfo.label}
          icon={Eye}
          subtext={
            <Badge
              variant="outline"
              className={cn("capitalize", statusInfo.color)}
            >
              {statusInfo.label}
            </Badge>
          }
        />
        <StatCard
          title="Status"
          value="Beta"
          icon={CheckCircle2}
          subtext="Ready for players"
          trend="+12%"
        />
        <StatCard
          title="Revenue (30d)"
          value="$0.00"
          icon={DollarSign}
          subtext={
            <span className="text-sm font-medium text-muted-foreground flex items-center">
              <TrendingUp className="h-3 w-3 mr-1" /> No sales yet
            </span>
          }
        />
        <StatCard title="Downloads (30d)" value="0" icon={Download} subtext="Start promoting your project" />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
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
                  <div
                    key={item.id}
                    className="flex items-start gap-4 p-4 rounded-lg bg-muted/20 border border-border/30"
                  >
                    <div className="p-2 bg-primary/10 rounded-full">
                      {item.type === "devlog" ? (
                        <BookText className="h-4 w-4 text-primary" />
                      ) : (
                        <MessageSquare className="h-4 w-4 text-primary" />
                      )}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-foreground">
                        {item.type === "devlog" ? item.title : `Feedback from ${item.author}`}
                      </p>
                      <p className="text-sm text-muted-foreground line-clamp-1 mt-1">
                        {item.type === "devlog" ? item.content : `"${item.comment}"`}
                      </p>
                    </div>
                    <p className="text-xs text-muted-foreground whitespace-nowrap">
                      {new Date(item.createdAt).toLocaleDateString()}
                    </p>
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

        <Card>
          <CardHeader>
            <CardTitle>Release Checklist</CardTitle>
            <CardDescription>Steps to get your page ready for the public.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Progress</span>
                <span className="text-sm text-muted-foreground">{readyPct}%</span>
              </div>
              <Progress value={readyPct} className="h-2" />
            </div>
            <div className="space-y-4 text-sm">
              <ChecklistItem label="Set a title" done={readiness.hasTitle} />
              <ChecklistItem label="Upload a cover image" done={readiness.hasCover} />
              <ChecklistItem label="Write a description" done={readiness.hasDescription} />
              <ChecklistItem label="Select platforms" done={readiness.hasPlatforms} />
              <ChecklistItem label="Upload a build" done={readiness.hasBuild} />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
