import { CourseLearnerNav } from "@/components/learning/course-learner-nav";
import { headers } from "next/headers";
import type { ReactNode } from "react";

export default async function CourseWorkspaceLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const requestHeaders = await headers();
  const visibleUrl = requestHeaders.get("x-gameguild-visible-url");
  const initialPathname = visibleUrl
    ? new URL(visibleUrl).pathname
    : `/courses/${slug}`;

  return (
    <>
      <CourseLearnerNav initialPathname={initialPathname} slug={slug} />
      {children}
    </>
  );
}
