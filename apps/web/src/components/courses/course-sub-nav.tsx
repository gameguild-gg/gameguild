"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { LayoutDashboard, FileText, Truck, DollarSign, Award, BookOpen, ImageIcon, Settings, Users } from "lucide-react"
import { cn } from "@/lib/utils"

export function CourseSubNav({ courseSlug }: { courseSlug: string }) {
  const pathname = usePathname()
  const basePath = `/courses/${courseSlug}`

  const navItems = [
    { href: basePath, label: "Overview", icon: LayoutDashboard },
    { href: `${basePath}/details`, label: "Details", icon: FileText },
    { href: `${basePath}/content`, label: "Content", icon: BookOpen },
    { href: `${basePath}/delivery`, label: "Delivery", icon: Truck },
    { href: `${basePath}/pricing`, label: "Pricing", icon: DollarSign },
    { href: `${basePath}/certificates`, label: "Certificates", icon: Award },
    { href: `${basePath}/media`, label: "Media", icon: ImageIcon },
    { href: `${basePath}/settings`, label: "Settings", icon: Settings },
  ]

  return (
    <aside className="w-56 flex-shrink-0">
      <nav className="flex flex-col gap-1 p-2">
        {navItems.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            className={cn(
              "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
              pathname === item.href && "bg-muted text-foreground",
            )}
          >
            <item.icon className="h-4 w-4" />
            <span>{item.label}</span>
          </Link>
        ))}
      </nav>
    </aside>
  )
}
