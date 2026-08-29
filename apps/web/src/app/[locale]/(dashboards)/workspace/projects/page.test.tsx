import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getWorkspaceProjects: vi.fn(),
  getWorkspaceTeamProjects: vi.fn(),
  getWorkspaceTeams: vi.fn(),
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    href,
    children,
    ...props
  }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));
vi.mock("@/lib/workspaces", () => ({
  getWorkspaceProjects: mocks.getWorkspaceProjects,
  getWorkspaceTeamProjects: mocks.getWorkspaceTeamProjects,
  getWorkspaceTeams: mocks.getWorkspaceTeams,
}));

import ProjectsPage from "./page";

const projects = [
  {
    id: "project-1",
    slug: "neon-racer",
    title: "Neon Racer",
    status: "Draft",
    visibility: "Private",
    shortDescription: "A racing game",
    description: null,
  },
];

const teamProjects = [
  {
    id: "project-2",
    slug: "sky-rail",
    title: "Sky Rail",
    status: "Published",
    visibility: "Public",
    shortDescription: "A Team racing game",
    description: null,
  },
];

const teams = [
  {
    id: "team-1",
    slug: "alpha-team",
    name: "Alpha Team",
  },
];

function pageProps(team?: string | string[]) {
  return { searchParams: Promise.resolve(team ? { team } : {}) };
}

async function openScopeMenu() {
  fireEvent.mouseDown(
    screen.getByRole("button", { name: /Current scope: All projects/i }),
    { button: 0 },
  );
  await screen.findByRole("menu");
}

describe("member projects list page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceProjects.mockResolvedValue(projects);
    mocks.getWorkspaceTeamProjects.mockResolvedValue(teamProjects);
    mocks.getWorkspaceTeams.mockResolvedValue(teams);
  });
  afterEach(cleanup);

  it("lists projects with links to the new /projects/:slug routes", async () => {
    render(await ProjectsPage(pageProps()));

    expect(
      screen.getByRole("heading", { name: "Projects" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Neon Racer/ })).toHaveAttribute(
      "href",
      "/workspace/projects/neon-racer",
    );
  });

  it("links project creation to /projects/new", async () => {
    render(await ProjectsPage(pageProps()));

    expect(
      screen.getByRole("link", { name: /Create Project/ }),
    ).toHaveAttribute("href", "/workspace/projects/new");
  });

  it("shows the empty state when no projects exist", async () => {
    mocks.getWorkspaceProjects.mockResolvedValue([]);

    render(await ProjectsPage(pageProps()));

    expect(screen.getByText("No Projects yet")).toBeInTheDocument();
    expect(
      screen.getAllByRole("link", { name: "Create Project" }),
    ).toHaveLength(1);
    expect(screen.getByText(/personal project or a Team project/i)).toBeInTheDocument();
    expect(screen.getByText(/version before it can enter Testing Lab/i)).toBeInTheDocument();
  });

  it("offers all Projects and each Team as URL-backed scope options", async () => {
    render(await ProjectsPage(pageProps()));

    await openScopeMenu();

    expect(
      screen.getByRole("menuitem", { name: /All projects/i }),
    ).toHaveAttribute("href", "/workspace/projects");
    expect(
      screen.getByRole("menuitem", { name: /Alpha Team projects/i }),
    ).toHaveAttribute("href", "/workspace/projects?team=alpha-team");
    expect(screen.getAllByRole("menuitem")).toHaveLength(2);
  });

  it("keeps All projects as the only scope when the member has no Teams", async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([]);

    render(await ProjectsPage(pageProps()));

    await openScopeMenu();

    expect(screen.getAllByRole("menuitem")).toHaveLength(1);
    expect(
      screen.getByRole("menuitem", { name: /All projects/i }),
    ).toHaveAttribute("href", "/workspace/projects");
    expect(mocks.getWorkspaceProjects).toHaveBeenCalledOnce();
  });

  it("offers an independent scope option for every Team membership", async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([
      ...teams,
      { id: "team-2", slug: "beta-team", name: "Beta Team" },
      { id: "team-3", slug: "gamma-team", name: "Gamma Team" },
    ]);

    render(await ProjectsPage(pageProps()));

    await openScopeMenu();

    expect(screen.getAllByRole("menuitem")).toHaveLength(4);
    expect(
      screen.getByRole("menuitem", { name: /Beta Team projects/i }),
    ).toHaveAttribute("href", "/workspace/projects?team=beta-team");
    expect(
      screen.getByRole("menuitem", { name: /Gamma Team projects/i }),
    ).toHaveAttribute("href", "/workspace/projects?team=gamma-team");
  });

  it("loads only the selected Team Projects when the Team scope is active", async () => {
    render(await ProjectsPage(pageProps("alpha-team")));

    expect(mocks.getWorkspaceTeamProjects).toHaveBeenCalledWith("team-1");
    expect(mocks.getWorkspaceProjects).not.toHaveBeenCalled();
    expect(screen.getByRole("link", { name: /Sky Rail/ })).toHaveAttribute(
      "href",
      "/workspace/projects/sky-rail",
    );
    expect(
      screen.queryByRole("link", { name: /Neon Racer/ }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", {
        name: /Current scope: Alpha Team projects/i,
      }),
    ).toBeInTheDocument();
  });

  it("falls back to all Projects for an unknown or repeated Team scope", async () => {
    render(await ProjectsPage(pageProps(["alpha-team", "other-team"])));

    expect(mocks.getWorkspaceProjects).toHaveBeenCalledOnce();
    expect(mocks.getWorkspaceTeamProjects).not.toHaveBeenCalled();
    expect(
      screen.getByRole("button", { name: /Current scope: All projects/i }),
    ).toBeInTheDocument();
  });

  it("shows a Team-specific empty state", async () => {
    mocks.getWorkspaceTeamProjects.mockResolvedValue([]);

    render(await ProjectsPage(pageProps("alpha-team")));

    expect(screen.getByText("No Projects for Alpha Team")).toBeInTheDocument();
    expect(
      screen.getByText("Alpha Team is not connected to any Projects yet."),
    ).toBeInTheDocument();
  });
});
