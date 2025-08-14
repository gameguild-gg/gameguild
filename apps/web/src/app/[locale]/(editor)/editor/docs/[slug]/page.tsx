import DocsSlugPageClient from "./DocsSlugPageClient"

// Valid documentation sections
const validSections = [
  "introduction",
  "about-editor",
  "getting-started",
  "basic-concepts",
  "installation",
  "text-formatting",
  "media-content",
  "interactive-elements",
  "code-blocks",
  "quizzes",
  "tables",
  "lists",
  "project-management",
  "collaboration",
  "export-options",
  "plugins",
  "automation",
  "api-integration",
  "settings",
  "themes",
  "shortcuts",
  "storage",
  "performance",
]

// Generate static params for better performance
export function generateStaticParams() {
  return validSections.map((section) => ({
    slug: section,
  }))
}

// Generate metadata for each page
export function generateMetadata({ params }: { params: { slug: string } }) {
  const slug = params.slug

  const titleMap: Record<string, string> = {
    introduction: "Introduction - GameGuild Editor Documentation",
    "about-editor": "About the Editor - GameGuild Editor Documentation",
    "getting-started": "Getting Started - GameGuild Editor Documentation",
    "text-formatting": "Text Formatting - GameGuild Editor Documentation",
    "media-content": "Media Content - GameGuild Editor Documentation",
    // Add more as needed
  }

  const descriptionMap: Record<string, string> = {
    introduction: "Learn about the GameGuild Editor and its core features",
    "about-editor": "Understand the architecture and components of the GameGuild Editor",
    "getting-started": "Quick start guide for new users of the GameGuild Editor",
    "text-formatting": "Master text formatting features in the GameGuild Editor",
    "media-content": "Learn how to work with images, videos, and other media",
    // Add more as needed
  }

  return {
    title: titleMap[slug] || "GameGuild Editor Documentation",
    description: descriptionMap[slug] || "Comprehensive documentation for the GameGuild Editor",
  }
}

export default function DocsSlugPage({ params }: { params: { slug: string } }) {
  return <DocsSlugPageClient slug={params.slug} />
}
