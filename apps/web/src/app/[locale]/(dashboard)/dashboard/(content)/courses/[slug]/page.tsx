"use client"

import type * as React from "react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { useCourse } from "@/components/course-context"
import { Users, Star, TrendingUp, Activity, CheckCircle2, AlertCircle, type LucideIcon } from "lucide-react"
import { Progress } from "@/components/ui/progress"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"

export default function CourseOverviewPage() {
  const course = useCourse()

  const completionRate =
    course.enrollments.length > 0
      ? Math.round(course.enrollments.reduce((acc, e) => acc + e.progress, 0) / course.enrollments.length)
      : 0

  const statusColors = {
    published: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    draft: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    archived: "bg-slate-500/10 text-slate-400 border-slate-500/20",
  }

  return (
    <div className="grid gap-8">
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Status"
          value={course.status}
          icon={CheckCircle2}
          subtext={
            <Badge variant="outline" className={cn("capitalize", statusColors[course.status])}>
              {course.status}
            </Badge>
          }
        />
        <StatCard
          title="Total Students"
          value={course.totalStudents.toLocaleString()}
          icon={Users}
          subtext="All time enrollments"
          trend="+12%"
        />
        <StatCard
          title="Average Rating"
          value={course.averageRating.toFixed(1)}
          icon={Star}
          subtext={`${course.totalReviews} reviews`}
        />
        <StatCard
          title="Completion Rate"
          value={`${completionRate}%`}
          icon={TrendingUp}
          subtext="Average student progress"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2 dark-card">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-muted-foreground" />
              <CardTitle>Recent Enrollments</CardTitle>
            </div>
            <CardDescription>Latest students who joined your course.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {course.enrollments.length > 0 ? (
                course.enrollments.slice(0, 5).map((enrollment) => (
                  <div
                    key={enrollment.id}
                    className="flex items-center justify-between p-4 rounded-lg bg-muted/20 border border-border/30"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 bg-primary/10 rounded-full flex items-center justify-center">
                        <span className="text-sm font-medium text-primary">
                          {enrollment.studentName.charAt(0).toUpperCase()}
                        </span>
                      </div>
                      <div>
                        <p className="text-sm font-medium text-foreground">{enrollment.studentName}</p>
                        <p className="text-xs text-muted-foreground">
                          Enrolled {new Date(enrollment.enrolledAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium">{enrollment.progress}%</p>
                      <Progress value={enrollment.progress} className="w-20 h-2 mt-1" />
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12">
                  <div className="p-3 bg-muted/20 rounded-full w-fit mx-auto mb-4">
                    <Users className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <p className="text-sm text-muted-foreground">No enrollments yet.</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Course Readiness</CardTitle>
            <CardDescription>Complete these steps to publish your course.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-4 text-sm">
              <ChecklistItem label="Course details completed" done={!!course.description} />
              <ChecklistItem label="Cover image uploaded" done={!!course.coverUrl} />
              <ChecklistItem label="Learning objectives set" done={course.learningObjectives.length > 0} />
              <ChecklistItem label="Content modules added" done={course.modules.length > 0} />
              <ChecklistItem label="Pricing configured" done={true} />
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Course Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-muted-foreground">Duration</span>
                <p className="font-medium">{course.duration} hours</p>
              </div>
              <div>
                <span className="text-muted-foreground">Level</span>
                <p className="font-medium capitalize">{course.level}</p>
              </div>
              <div>
                <span className="text-muted-foreground">Delivery</span>
                <p className="font-medium capitalize">{course.deliveryMethod.replace("-", " ")}</p>
              </div>
              <div>
                <span className="text-muted-foreground">Modules</span>
                <p className="font-medium">{course.modules.length}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Instructor</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
                <span className="text-lg font-medium text-primary">{course.instructor.charAt(0).toUpperCase()}</span>
              </div>
              <div>
                <p className="font-medium text-foreground">{course.instructor}</p>
                <p className="text-sm text-muted-foreground">{course.instructorBio || "Course instructor"}</p>
              </div>
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

