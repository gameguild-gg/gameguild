import { Link } from "@/i18n/navigation";
import type { WorkspaceTeam } from "@/lib/workspaces";
import { Button } from "@game-guild/ui/components/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import { Check, ChevronsUpDown, FolderKanban, Users } from "lucide-react";

type ProjectScopeTeam = Pick<WorkspaceTeam, "id" | "name" | "slug">;

interface ProjectScopeSwitcherProps {
  teams: readonly ProjectScopeTeam[];
  selectedTeam?: ProjectScopeTeam;
}

export function ProjectScopeSwitcher({
  teams,
  selectedTeam,
}: ProjectScopeSwitcherProps) {
  const activeLabel = selectedTeam
    ? `${selectedTeam.name} projects`
    : "All projects";
  const ActiveIcon = selectedTeam ? Users : FolderKanban;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="outline"
          className="h-auto w-full justify-start gap-3 px-3 py-2 text-left sm:w-72"
          aria-label={`Filter projects by Team. Current scope: ${activeLabel}`}
        >
          <span className="bg-muted text-foreground flex size-8 shrink-0 items-center justify-center rounded-md border">
            <ActiveIcon className="size-4" aria-hidden="true" />
          </span>
          <span className="grid min-w-0 flex-1 leading-tight">
            <span className="truncate text-sm font-medium">{activeLabel}</span>
            <span className="truncate text-sm text-muted-foreground">
              {selectedTeam ? "Team projects" : "My workspace"}
            </span>
          </span>
          <ChevronsUpDown
            className="ml-auto size-4 shrink-0 text-muted-foreground"
            aria-hidden="true"
          />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className="w-[var(--radix-dropdown-menu-trigger-width)] min-w-0 max-w-[calc(100vw-2rem)]"
      >
        <DropdownMenuLabel className="text-muted-foreground">
          Project scope
        </DropdownMenuLabel>
        <DropdownMenuItem asChild>
          <Link
            href="/workspace/projects"
            className="w-full"
            aria-current={selectedTeam ? undefined : "page"}
          >
            <FolderKanban className="size-4" aria-hidden="true" />
            <span>All projects</span>
            {!selectedTeam && (
              <Check
                className="ml-auto size-4 text-primary"
                aria-hidden="true"
              />
            )}
          </Link>
        </DropdownMenuItem>
        {teams.map((team) => {
          const isSelected = selectedTeam?.id === team.id;

          return (
            <DropdownMenuItem key={team.id} asChild>
              <Link
                href={`/workspace/projects?team=${encodeURIComponent(team.slug)}`}
                className="min-w-0 w-full"
                aria-current={isSelected ? "page" : undefined}
              >
                <Users className="size-4" aria-hidden="true" />
                <span className="truncate">{team.name} projects</span>
                {isSelected && (
                  <Check
                    className="ml-auto size-4 text-primary"
                    aria-hidden="true"
                  />
                )}
              </Link>
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
