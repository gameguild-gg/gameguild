import { layoutTemplates } from "./layout"
import { componentTemplates } from "./components"
import { mediaTemplates } from "./media"
import { formTemplates } from "./forms"
import { advancedTemplates } from "./advanced"
import type { HTMLTemplate } from "./types"

export type { HTMLTemplate } from "./types"

const allTemplates: HTMLTemplate[] = [
  ...layoutTemplates,
  ...componentTemplates,
  ...mediaTemplates,
  ...formTemplates,
  ...advancedTemplates,
]

export function getAllHTMLTemplates(): HTMLTemplate[] {
  return allTemplates
}

export function getHTMLTemplateById(id: string): HTMLTemplate | undefined {
  return allTemplates.find((t) => t.id === id)
}

export function getHTMLTemplatesByCategory(category: string): HTMLTemplate[] {
  return allTemplates.filter((t) => t.category === category)
}

export function getHTMLCategories(): string[] {
  return Array.from(new Set(allTemplates.map((t) => t.category)))
}

export function searchHTMLTemplates(query: string): HTMLTemplate[] {
  const lowerQuery = query.toLowerCase()
  return allTemplates.filter(
    (t) =>
      t.title.toLowerCase().includes(lowerQuery) ||
      t.description.toLowerCase().includes(lowerQuery) ||
      t.category.toLowerCase().includes(lowerQuery),
  )
}
