import { ProjectScopeSwitcher } from "@/components/workspace/project-scope-switcher";
import { WorkspaceProjectList } from "@/components/workspace/workspace-project-list";
import { Link } from "@/i18n/navigation";
import {
  getWorkspaceProjects,
  getWorkspaceTeamProjects,
  getWorkspaceTeams,
} from "@/lib/workspaces";
import { Badge } from "@game-guild/ui/components/badge";
import { buttonVariants } from "@game-guild/ui/components/button";
import { Plus } from "lucide-react";

interface ProjectsPageProps {
  searchParams: Promise<{ team?: string | string[] }>;
}

export default async function MyProjectsPage({
  searchParams,
}: ProjectsPageProps) {
  const [teams, query] = await Promise.all([getWorkspaceTeams(), searchParams]);
  const requestedTeamSlug =
    typeof query.team === "string" ? query.team : undefined;
  const selectedTeam = requestedTeamSlug
    ? teams.find((team) => team.slug === requestedTeamSlug)
    : undefined;
  const projects = selectedTeam
    ? await getWorkspaceTeamProjects(selectedTeam.id)
    : await getWorkspaceProjects();
  const emptyTitle = selectedTeam
    ? `No Projects for ${selectedTeam.name}`
    : "No Projects yet";
  const emptyDescription = selectedTeam
    ? `${selectedTeam.name} is not connected to any Projects yet.`
    : "Create a personal project or a Team project. Every Project needs a version before it can enter Testing Lab.";

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <Badge variant="outline">My workspace</Badge>
          <h1 className="mt-2 text-3xl font-bold tracking-tight">Projects</h1>
          <p className="text-muted-foreground">
            Projects you created, collaborate on, or access through an active
            Team relationship.
          </p>
        </div>
        <Link href="/workspace/projects/new" className={buttonVariants({ className: "w-full sm:w-auto" })}>
          <Plus className="size-4" />
          Create Project
        </Link>
      </header>

      <section className="flex flex-col gap-3 border-y py-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <h2 className="text-sm font-medium">Project scope</h2>
          <p className="text-sm text-muted-foreground">
            {selectedTeam
              ? `Showing Projects connected to ${selectedTeam.name}.`
              : "Showing every Project available in your workspace."}
          </p>
        </div>
        <ProjectScopeSwitcher teams={teams} selectedTeam={selectedTeam} />
      </section>

      <WorkspaceProjectList
        projects={projects}
        emptyTitle={emptyTitle}
        emptyDescription={emptyDescription}
      />
    </div>
  );
}
