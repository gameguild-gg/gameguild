"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { Program } from "@/lib/api/generated/types.gen";

const CourseContext = createContext<Program | null>(null);

export function CourseProvider({ children, course }: { children: ReactNode; course: Program | null }) {
  return <CourseContext.Provider value={course}>{children}</CourseContext.Provider>;
}

export function useCourse() {
  const context = useContext(CourseContext);
  if (!context) {
    throw new Error("useCourse must be used within a CourseProvider");
  }
  return context;
}
