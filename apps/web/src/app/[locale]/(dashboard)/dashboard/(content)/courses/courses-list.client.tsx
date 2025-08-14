"use client"

import * as React from "react"
import { Search, Filter, Grid3X3, List, BookOpen, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import type { Program } from "@/lib/api/generated/types.gen"

// Placeholder for create drawer - similar to pm-dashboard structure
function CourseCreateDrawer() {
  return (
    <Button variant="secondary" size="sm" disabled>
      <Plus className="h-4 w-4 mr-2" /> Create Course
    </Button>
  )
}

// Course card component following pm-dashboard pattern
function CourseCard({ course, viewMode = "grid" }: { course: Program; viewMode?: "grid" | "list" }) {
  const updated = new Date(course.updatedAt || Date.now()).toLocaleDateString()
  
  const statusColors = {
    published: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    draft: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    archived: "bg-slate-500/10 text-slate-400 border-slate-500/20",
  }

  const getStatus = () => {
    if (typeof course.status === 'number') {
      return course.status === 1 ? 'published' : course.status === 2 ? 'archived' : 'draft'
    }
    return String(course.status || 'draft').toLowerCase()
  }

  const status = getStatus()

  if (viewMode === "list") {
    return (
      <Card className="dark-card hover:shadow-xl transition-all duration-200 cursor-pointer">
        <CardContent className="p-6">
          <div className="flex items-center gap-6">
            <div className="relative w-32 h-20 bg-muted/20 rounded-lg overflow-hidden flex-shrink-0">
              <div className="w-full h-full flex items-center justify-center">
                <BookOpen className="h-8 w-8 text-muted-foreground" />
              </div>
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-3 mb-2">
                <h3 className="font-semibold text-foreground truncate">{course.title}</h3>
                <Badge variant="outline" className={`capitalize ${statusColors[status as keyof typeof statusColors] || statusColors.draft}`}>
                  {status}
                </Badge>
              </div>
              <p className="text-sm text-muted-foreground line-clamp-1 mb-3">
                {course.description}
              </p>
              <div className="flex items-center gap-6 text-xs text-muted-foreground">
                <span className="flex items-center gap-1">
                  <BookOpen className="w-3 h-3" />
                  {(course as any).contentItems?.length || 0} modules
                </span>
                <span className="text-muted-foreground">Updated {updated}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="dark-card group hover:shadow-xl transition-all duration-200 cursor-pointer">
      <CardHeader className="p-0">
        <div className="relative w-full h-48 bg-muted/20 rounded-t-lg overflow-hidden flex items-center justify-center">
          <BookOpen className="h-16 w-16 text-muted-foreground" />
          <div className="absolute top-3 right-3">
            <Badge variant="outline" className={`capitalize ${statusColors[status as keyof typeof statusColors] || statusColors.draft}`}>
              {status}
            </Badge>
          </div>
        </div>
      </CardHeader>
      <CardContent className="p-6">
        <div className="mb-3">
          <CardTitle className="text-lg font-semibold text-foreground line-clamp-2 mb-2">{course.title}</CardTitle>
          <CardDescription className="text-muted-foreground line-clamp-2">
            {course.description}
          </CardDescription>
        </div>

        <div className="flex items-center justify-between text-xs text-muted-foreground mb-4">
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-1">
              <BookOpen className="w-3 h-3" />
              {(course as any).contentItems?.length || 0} modules
            </span>
          </div>
        </div>

        <div className="text-xs text-muted-foreground">Updated {updated}</div>
      </CardContent>
    </Card>
  )
}

interface CoursesListClientProps {
  initialCourses: Program[]
}

export function CoursesListClient({ initialCourses }: CoursesListClientProps) {
  const [courses] = React.useState<Program[]>(initialCourses)
  const [searchQuery, setSearchQuery] = React.useState("")
  const [statusFilter, setStatusFilter] = React.useState("all")
  const [viewMode, setViewMode] = React.useState<"grid" | "list">("grid")

  const getFilteredStatus = (course: Program) => {
    if (typeof course.status === 'number') {
      return course.status === 1 ? 'published' : course.status === 2 ? 'archived' : 'draft'
    }
    return String(course.status || 'draft').toLowerCase()
  }

  const filteredCourses = courses.filter((course) => {
    const matchesSearch = course.title?.toLowerCase().includes(searchQuery.toLowerCase()) || false
    const matchesStatus = statusFilter === "all" || getFilteredStatus(course) === statusFilter
    return matchesSearch && matchesStatus
  })

  return (
    <>
      <div className="flex items-center justify-between mb-6">
        <CourseCreateDrawer />
      </div>

      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search courses..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 w-80 bg-background/50 border-border/50"
            />
          </div>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-40 bg-background/50 border-border/50">
              <Filter className="h-4 w-4 mr-2" />
              <SelectValue placeholder="All Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="published">Published</SelectItem>
              <SelectItem value="draft">Draft</SelectItem>
              <SelectItem value="archived">Archived</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">
            Showing {filteredCourses.length} of {courses.length} courses
          </span>
          <div className="flex items-center border border-border/50 rounded-md">
            <Button
              variant={viewMode === "grid" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setViewMode("grid")}
              className="rounded-r-none"
            >
              <Grid3X3 className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === "list" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setViewMode("list")}
              className="rounded-l-none"
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {filteredCourses.length > 0 ? (
        <div
          className={viewMode === "grid" ? "grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4" : "space-y-4"}
        >
          {filteredCourses.map((course) => (
            <CourseCard key={course.id} course={course} viewMode={viewMode} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center text-center py-20">
          <div className="p-4 bg-muted/20 rounded-full mb-6">
            <BookOpen className="h-12 w-12 text-muted-foreground" />
          </div>
          <h3 className="text-xl font-semibold mb-2">No courses found</h3>
          <p className="text-muted-foreground mb-6 max-w-md">
            {searchQuery || statusFilter !== "all"
              ? "Try adjusting your search or filters"
              : "Create your first course to get started"}
          </p>
          <CourseCreateDrawer />
        </div>
      )}
    </>
  )
}
