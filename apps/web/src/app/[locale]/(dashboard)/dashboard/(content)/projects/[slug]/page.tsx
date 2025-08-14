"use client"

import type React from "react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import {
  BookText,
  CheckCircle2,
  Download,
  Eye,
  MessageSquare,
  type LucideIcon,
  DollarSign,
  AlertCircle,
  TrendingUp,
  Activity,
} from "lucide-react"
import { Progress } from "@/components/ui/progress"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"

export default function ProjectOverviewPage() {
  // Placeholder data - this would come from project context or props in real app
  const project = {
    title: "My Game Project",
    visibility: "public",
    releaseStatus: "beta",
    devlogs: [],
    feedback: [],
    versions: [],
    description: "",
    platforms: [],
    coverUrl: null
  }

  const readiness = {
    hasTitle: !!project.title?.trim(),
    hasCover: !!project.coverUrl,
    hasDescription: !!project.description && project.description.trim().length > 30,
    hasPlatforms: !!project.platforms && project.platforms.length > 0,
    hasBuild: !!project.versions && project.versions.length > 0,
  }
  const readyCount = Object.values(readiness).filter(Boolean).length
  const readyPct = Math.round((readyCount / Object.keys(readiness).length) * 100)

  const recentActivity: Array<{
    id: string;
    type: "devlog" | "feedback";
    title?: string;
    author?: string;
    comment?: string;
    content?: string;
    createdAt: number;
  }> = [
    // Placeholder - would come from real data
  ]

  return (
    <div className="grid gap-8">
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Visibility"
          value={project.visibility}
          icon={Eye}
          subtext={
            <Badge
              variant="outline"
              className={cn(
                "capitalize",
                project.visibility === "public" && "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
                project.visibility === "unlisted" && "bg-amber-500/10 text-amber-400 border-amber-500/20",
                project.visibility === "draft" && "bg-slate-500/10 text-slate-400 border-slate-500/20",
              )}
            >
              {project.visibility}
            </Badge>
          }
        />
        <StatCard
          title="Status"
          value={project.releaseStatus}
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
              No data
            </span>
          }
        />
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
                      {new Date(item.createdAt || Date.now()).toLocaleDateString()}
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

        <Card className="dark-card">
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

function StatCard({
  title,
  value,
  icon: Icon,
  subtext,
  trend,
}: {
  title: string
  value: string
  icon: LucideIcon
  subtext?: React.ReactNode
  trend?: string
}) {
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
  )
}

function ChecklistItem({ label, done }: { label: string; done: boolean }) {
  return (
    <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/10 border border-border/30">
      {done ? (
        <CheckCircle2 className="h-5 w-5 text-emerald-400 flex-shrink-0" />
      ) : (
        <AlertCircle className="h-5 w-5 text-amber-400 flex-shrink-0" />
      )}
      <span className={cn("transition-colors", done ? "text-foreground" : "text-muted-foreground")}>{label}</span>
    </div>
  )
}

