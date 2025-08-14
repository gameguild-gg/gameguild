"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { Project } from "@/lib/api/generated/types.gen";

const ProjectContext = createContext<Project | null>(null);

export function ProjectProvider({ children, project }: { children: ReactNode; project: Project | null }) {
  return <ProjectContext.Provider value={project}>{children}</ProjectContext.Provider>;
}

export function useProject() {
  const context = useContext(ProjectContext);
  if (!context) {
    throw new Error("useProject must be used within a ProjectProvider");
  }
  return context;
}
