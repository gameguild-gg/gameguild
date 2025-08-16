"use client"

import * as React from "react"
import { notFound, useParams } from "next/navigation"
import { PageHeader } from "@/components/projects/page-header"
import { ProjectSubNav } from "@/components/projects/project-sub-nav"
import { getProjectBySlug } from "@/lib/local-db"
import type { GameProject } from "@/lib/types"
import { ProjectProvider } from "@/components/projects/project-context"

export default function ProjectDetailLayout({ children }: { children: React.ReactNode }) {
  const params = useParams()
  const slug = String(params.slug)
  const [project, setProject] = React.useState<GameProject | undefined>()

  React.useEffect(() => {
    const proj = getProjectBySlug(slug)
    if (proj) {
      setProject(proj)
    } else {
      notFound()
    }
  }, [slug])

  if (!project) {
    return null // Render nothing until the project data is loaded
  }

  return (
    <ProjectProvider project={project}>
      <div className="flex flex-col min-h-svh">
        <PageHeader title={project.title} />
        <div className="flex flex-1">
          <ProjectSubNav projectId={slug} />
          <main className="flex-1 p-4 md:p-6 bg-muted/40">{children}</main>
        </div>
      </div>
    </ProjectProvider>
  )
}
