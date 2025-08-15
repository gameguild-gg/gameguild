"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { Course } from "@/lib/types";

const CourseContext = createContext<Course | null>(null);

export function CourseProvider({ children, course }: { children: ReactNode; course: Course | null }) {
  return <CourseContext.Provider value={course}>{children}</CourseContext.Provider>;
}

export function useCourse() {
  const context = useContext(CourseContext);
  if (!context) {
    throw new Error("useCourse must be used within a CourseProvider");
  }
  return context;
}
