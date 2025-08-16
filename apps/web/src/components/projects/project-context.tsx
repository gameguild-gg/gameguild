"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { Project } from "@/lib/api/generated/types.gen";
import type { GameProject } from "@/lib/types";

const ProjectContext = createContext<GameProject | null>(null);

export function ProjectProvider({ children, project }: { children: ReactNode; project: GameProject | null }) {
  return <ProjectContext.Provider value={project}>{children}</ProjectContext.Provider>;
}

export function useProject() {
  const context = useContext(ProjectContext);
  if (!context) {
    throw new Error("useProject must be used within a ProjectProvider");
  }
  return context;
}
