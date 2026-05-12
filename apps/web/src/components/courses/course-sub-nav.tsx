"use client"

import type { Route } from 'next'
import Link from "next/link"
import { usePathname } from "next/navigation"
import { LayoutDashboard, FileText, Truck, DollarSign, Award, BookOpen, ImageIcon, Settings } from "lucide-react"
import { cn } from "@/lib/utils"

export function CourseSubNav({ courseSlug }: { courseSlug: string }) {
  const pathname = usePathname()
  const basePath = `/dashboard/courses/${courseSlug}`

  const navItems = [
    { href: basePath as Route, label: "Overview", icon: LayoutDashboard },
    { href: `${basePath}/details` as Route, label: "Details", icon: FileText },
    { href: `${basePath}/content` as Route, label: "Content", icon: BookOpen },
    { href: `${basePath}/delivery` as Route, label: "Delivery", icon: Truck },
    { href: `${basePath}/pricing` as Route, label: "Pricing", icon: DollarSign },
    { href: `${basePath}/certificates` as Route, label: "Certificates", icon: Award },
    { href: `${basePath}/media` as Route, label: "Media", icon: ImageIcon },
    { href: `${basePath}/settings` as Route, label: "Settings", icon: Settings },
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
