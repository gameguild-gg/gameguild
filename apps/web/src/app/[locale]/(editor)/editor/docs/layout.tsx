import type { ReactNode } from "react"

interface DocsLayoutProps {
  children: ReactNode
}

export default function DocsLayout({ children }: DocsLayoutProps) {
  return (
    <div className="absolute inset-0 top-16 h-[calc(100vh-4rem)] max-h-[calc(100vh-4rem)] overflow-hidden bg-white dark:bg-gray-900">
      <div className="flex h-full">{children}</div>
    </div>
  )
}
