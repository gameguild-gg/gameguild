"use client"

import { Sun, Moon, Menu } from "lucide-react"
import { useTheme } from "next-themes"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { useEffect, useState } from "react"

export function TopMenu() {
  const { theme, setTheme } = useTheme()
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const toggleTheme = () => {
    setTheme(theme === "dark" ? "light" : "dark")
  }

  const isDark = theme === "dark"

  return (
    <>
      {/* Left Menu Panel */}
      <div className="fixed top-3 left-4 z-50 flex items-center gap-4 bg-white/90 dark:bg-gray-900/90 backdrop-blur-md border border-gray-200 dark:border-gray-800 shadow-lg rounded-full px-4 h-11">
        {/* Logo/Brand */}
        <Link href="/block-content-editor" className="flex items-center gap-2">
          <div className="w-6 h-6 bg-blue-600 dark:bg-blue-500 rounded-md flex items-center justify-center">
            <Menu className="w-3.5 h-3.5 text-white" />
          </div>
          <span className="font-bold text-sm text-gray-900 dark:text-gray-100 hidden sm:inline">
            GameGuild Lexical Editor
          </span>
        </Link>

        <div className="w-px h-4 bg-gray-200 dark:bg-gray-800" />

        {/* Navigation */}
        <nav className="flex items-center gap-4 text-xs font-semibold">
          <Link
            href="/block-content-editor"
            className="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
          >
            Home
          </Link>
          <Link
            href="/block-content-editor/studio"
            className="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
          >
            Studio
          </Link>
          <Link
            href="/block-content-editor/viewer"
            className="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
          >
            Viewer
          </Link>
        </nav>
      </div>

      {/* Right Menu Panel */}
      <div className="fixed top-3 right-4 z-50 flex items-center bg-white/90 dark:bg-gray-900/90 backdrop-blur-md border border-gray-200 dark:border-gray-800 shadow-lg rounded-full p-1.5 h-11">
        {mounted && (
          <Button
            variant="ghost"
            size="sm"
            onClick={toggleTheme}
            className="rounded-full w-8 h-8 p-0 flex items-center justify-center hover:bg-gray-100 dark:hover:bg-gray-800"
            aria-label="Toggle theme"
          >
            {isDark ? <Sun className="w-4 h-4 text-yellow-500" /> : <Moon className="w-4 h-4 text-gray-600" />}
          </Button>
        )}
        {!mounted && <div className="w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-800 animate-pulse" />}
      </div>
    </>
  )
}
