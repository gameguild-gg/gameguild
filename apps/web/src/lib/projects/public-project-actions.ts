"use server";

import { getPublishedProjectsPage, PUBLIC_PROJECT_PAGE_SIZE } from "@/lib/projects/public-projects";

export async function loadMorePublicProjects(skip: number) {
  if (!Number.isSafeInteger(skip) || skip < 0 || skip > 10_000) {
    return { items: [], hasMore: false, error: "We couldn’t load more projects. Refresh the page and try again." };
  }

  try {
    const items = await getPublishedProjectsPage(skip, PUBLIC_PROJECT_PAGE_SIZE);
    return { items, hasMore: items.length === PUBLIC_PROJECT_PAGE_SIZE };
  } catch {
    return { items: [], hasMore: false, error: "We couldn’t load more projects. Refresh the page and try again." };
  }
}
