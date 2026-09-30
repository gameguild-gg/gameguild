export const PROJECT_TYPE_OPTIONS = [
  { value: "Game", label: "Game" },
  { value: "Tool", label: "Tool" },
  { value: "Art", label: "Art" },
  { value: "Music", label: "Music" },
  { value: "Educational", label: "Educational" },
  { value: "Plugin", label: "Plugin" },
  { value: "Template", label: "Template" },
  { value: "Library", label: "Library" },
  { value: "Other", label: "Other" },
] as const;

export type PublicProjectType = (typeof PROJECT_TYPE_OPTIONS)[number]["value"];

export function isPublicProjectType(value: string): value is PublicProjectType {
  return PROJECT_TYPE_OPTIONS.some((option) => option.value === value);
}
