"use client"

import Image from "next/image"
import Link from "next/link"
import { CalendarClock, Gamepad2, Clock, Users } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import type { GameProject } from "@/lib/types"
import { cn } from "@/lib/utils"

export function ProjectCard({ project, viewMode = "grid" }: { project: GameProject; viewMode?: "grid" | "list" }) {
  const updated = new Date(project.updatedAt).toLocaleDateString()

  const statusColors = {
    released: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    beta: "bg-blue-500/10 text-blue-400 border-blue-500/20",
    wip: "bg-amber-500/10 text-amber-400 border-amber-500/20",
  }

  const visibilityColors = {
    public: "bg-emerald-500/10 text-emerald-400",
    unlisted: "bg-amber-500/10 text-amber-400",
    draft: "bg-slate-500/10 text-slate-400",
    private: "bg-red-500/10 text-red-400",
  }

  if (viewMode === "list") {
    return (
      <Link href={`/projects/${project.slug}`} className="block">
        <Card className="dark-card hover:shadow-xl transition-all duration-200 cursor-pointer">
          <CardContent className="p-6">
            <div className="flex items-center gap-6">
              <div className="relative w-24 h-16 bg-muted/20 rounded-lg overflow-hidden flex-shrink-0">
                <Image
                  src={project.coverUrl || "/placeholder.svg?height=64&width=96&query=game%20cover"}
                  alt={`${project.title} cover`}
                  fill
                  className="object-cover"
                  sizes="96px"
                />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold text-foreground truncate">{project.title}</h3>
                  <Badge variant="outline" className={cn("capitalize", statusColors[project.releaseStatus])}>
                    {project.releaseStatus}
                  </Badge>
                  <Badge variant="secondary" className={cn("capitalize", visibilityColors[project.visibility])}>
                    {project.visibility}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-1 mb-3">{project.tagline || "No description"}</p>
                <div className="flex items-center gap-6 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Gamepad2 className="w-3 h-3" />
                    {project.kind}
                  </span>
                  <span className="flex items-center gap-1">
                    <CalendarClock className="w-3 h-3" />
                    Updated {updated}
                  </span>
                  <span className="flex items-center gap-1">
                    <Users className="w-3 h-3" />
                    {project.feedback?.length || 0} reviews
                  </span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </Link>
    )
  }

  return (
    <Link href={`/projects/${project.slug}`} className="block">
      <Card className="dark-card group hover:shadow-xl transition-all duration-200 cursor-pointer">
        <CardHeader className="p-0">
          <div className="relative w-full h-48 bg-muted/20 rounded-t-lg overflow-hidden">
            <Image
              src={project.coverUrl || "/placeholder.svg?height=192&width=384&query=game%20cover"}
              alt={`${project.title} cover`}
              fill
              className="object-cover transition-transform group-hover:scale-105"
              sizes="(max-width:768px) 100vw, (max-width:1200px) 50vw, 33vw"
            />
            <div className="absolute top-3 right-3">
              <Badge variant="outline" className={cn("capitalize", statusColors[project.releaseStatus])}>
                {project.releaseStatus}
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6">
          <div className="flex items-start justify-between mb-3">
            <div className="flex-1 min-w-0">
              <CardTitle className="text-lg font-semibold text-foreground truncate mb-1">{project.title}</CardTitle>
              <CardDescription className="text-muted-foreground line-clamp-2">
                {project.tagline || "No description"}
              </CardDescription>
            </div>
            <Badge variant="secondary" className={cn("ml-2 capitalize", visibilityColors[project.visibility])}>
              {project.visibility}
            </Badge>
          </div>

          <div className="flex items-center justify-between text-xs text-muted-foreground mb-4">
            <div className="flex items-center gap-4">
              <span className="flex items-center gap-1">
                <Gamepad2 className="w-3 h-3" />
                {project.kind}
              </span>
              <span className="flex items-center gap-1">
                <Clock className="w-3 h-3" />
                {project.versions?.length || 0} versions
              </span>
            </div>
            <span>{updated}</span>
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}
