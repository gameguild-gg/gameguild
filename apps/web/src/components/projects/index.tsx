/**
 * Stub exports for projects module.
 * This component is disabled in production.
 */

'use client';


export interface Project {
    id: string;
    name: string;
    description?: string;
    status: string;
    thumbnail?: string;
}

export interface ProjectCardProps {
    project: Project;
    onClick?: () => void;
}

export function ProjectCard({ project, onClick }: ProjectCardProps) {
    return (
        <div
            className="border rounded-lg p-4 cursor-pointer hover:shadow-md transition-shadow"
            onClick={onClick}
        >
            <h3 className="font-semibold">{project.name}</h3>
            <p className="text-sm text-slate-500">{project.description}</p>
            <span className="text-xs">{project.status}</span>
        </div>
    );
}

export interface ProjectListProps {
    projects: Project[];
    onSelect?: (project: Project) => void;
}

export function ProjectList({ projects, onSelect }: ProjectListProps) {
    return (
        <div className="grid gap-4">
            {projects.map((project) => (
                <ProjectCard
                    key={project.id}
                    project={project}
                    onClick={() => onSelect?.(project)}
                />
            ))}
        </div>
    );
}

export function ProjectShowcase({ projects }: ProjectListProps) {
    return <ProjectList projects={projects} />;
}

export interface GameProjectEditorProps {
    projectId?: string;
    onSave?: (data: unknown) => void;
}

export function GameProjectEditor({ projectId }: GameProjectEditorProps) {
    return (
        <div className="p-4 border rounded-lg">
            <p className="text-slate-500">Project Editor disabled</p>
            {projectId && <p className="text-xs">Project: {projectId}</p>}
        </div>
    );
}

export default { ProjectCard, ProjectList, ProjectShowcase, GameProjectEditor };
