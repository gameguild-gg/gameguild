"use server";

import { getPublishedProjectsPage, PUBLIC_PROJECT_PAGE_SIZE } from "@/lib/projects/public-projects";
import { isPublicProjectType } from "@/lib/projects/project-types";

const LOAD_ERROR = "We couldn’t load more projects. Try again.";

export async function loadMorePublicProjects(
  skip: number,
  searchTerm = "",
  type?: string,
) {
  const projectType = typeof type === "string" && isPublicProjectType(type) ? type : undefined;
  if (
    !Number.isSafeInteger(skip) ||
    skip < 0 ||
    skip > 10_000 ||
    typeof searchTerm !== "string" ||
    searchTerm.length > 120 ||
    (type !== undefined && projectType === undefined)
  ) {
    return { items: [], hasMore: false, error: "We couldn’t load more projects. Refresh the page and try again." };
  }

  try {
    const page = await getPublishedProjectsPage(
      skip,
      PUBLIC_PROJECT_PAGE_SIZE + 1,
      searchTerm,
      projectType,
    );
    return {
      items: page.slice(0, PUBLIC_PROJECT_PAGE_SIZE),
      hasMore: page.length > PUBLIC_PROJECT_PAGE_SIZE,
    };
  } catch {
    return { items: [], hasMore: false, error: LOAD_ERROR };
  }
}
