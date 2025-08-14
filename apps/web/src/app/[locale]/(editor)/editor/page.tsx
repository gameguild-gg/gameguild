"use client"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Edit3, Eye, ArrowRight, BookOpen } from "lucide-react"
import Link from "next/link"

export default function HomePage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 via-white to-purple-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <div className="container mx-auto px-4">
        <div className="max-w-5xl mx-auto">
          {/* Header Section */}
          <div className="text-center space-y-6 mb-16">
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-200 text-sm font-medium">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                  clipRule="evenodd"
                />
              </svg>
              GameGuild Content Platform
            </div>

            <h1 className="text-5xl font-bold tracking-tight text-gray-900 dark:text-gray-100">
              Welcome to <span className="text-blue-600 dark:text-blue-400">GameGuild</span>
            </h1>

            <p className="text-xl text-gray-600 dark:text-gray-300 max-w-2xl mx-auto leading-relaxed">
              Create, edit, and preview your game development content with our powerful tools designed for the gaming
              community.
            </p>
          </div>

          {/* Navigation Cards */}
          <div className="grid lg:grid-cols-3 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
            {/* Editor Card */}
            <Card className="group hover:shadow-xl transition-all duration-300 border-2 hover:border-blue-200 dark:hover:border-blue-700 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
              <CardHeader className="text-center pb-4">
                <div className="mx-auto w-16 h-16 bg-blue-100 dark:bg-blue-900 rounded-2xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform duration-300">
                  <Edit3 className="w-8 h-8 text-blue-600 dark:text-blue-400" />
                </div>
                <CardTitle className="text-xl font-bold text-gray-900 dark:text-gray-100">Content Editor</CardTitle>
                <CardDescription className="text-gray-600 dark:text-gray-300 text-sm">
                  Create rich content with our advanced editor featuring text formatting, media, quizzes, and
                  interactive elements
                </CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="space-y-2 mb-6">
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    Rich text formatting
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    Media embedding
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    Interactive elements
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    Auto-save & projects
                  </div>
                </div>
                <Link href="/editor/lexical" className="block">
                  <Button className="w-full bg-blue-600 hover:bg-blue-700 text-white group-hover:bg-blue-700 transition-colors duration-300">
                    Start Creating
                    <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform duration-300" />
                  </Button>
                </Link>
              </CardContent>
            </Card>

            {/* Preview Card */}
            <Card className="group hover:shadow-xl transition-all duration-300 border-2 hover:border-purple-200 dark:hover:border-purple-700 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm">
              <CardHeader className="text-center pb-4">
                <div className="mx-auto w-16 h-16 bg-purple-100 dark:bg-purple-900 rounded-2xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform duration-300">
                  <Eye className="w-8 h-8 text-purple-600 dark:text-purple-400" />
                </div>
                <CardTitle className="text-xl font-bold text-gray-900 dark:text-gray-100">Content Preview</CardTitle>
                <CardDescription className="text-gray-600 dark:text-gray-300 text-sm">
                  Preview and share your content in a clean, reader-friendly format optimized for your audience
                </CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="space-y-2 mb-6">
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                    Clean reading experience
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                    Mobile-responsive
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                    Share & export options
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                    Print-friendly layouts
                  </div>
                </div>
                <Link href="/editor/preview" className="block">
                  <Button
                    variant="outline"
                    className="w-full border-purple-200 dark:border-purple-700 text-purple-600 dark:text-purple-400 hover:bg-purple-50 dark:hover:bg-purple-900 group-hover:bg-purple-50 dark:group-hover:bg-purple-900 transition-colors duration-300 bg-transparent"
                  >
                    View Content
                    <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform duration-300" />
                  </Button>
                </Link>
              </CardContent>
            </Card>

            <Card className="group hover:shadow-xl transition-all duration-300 border-2 hover:border-emerald-200 dark:hover:border-emerald-700 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm lg:col-span-1 md:col-span-2 lg:col-start-auto md:col-start-1">
              <CardHeader className="text-center pb-4">
                <div className="mx-auto w-16 h-16 bg-emerald-100 dark:bg-emerald-900 rounded-2xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform duration-300">
                  <BookOpen className="w-8 h-8 text-emerald-600 dark:text-emerald-400" />
                </div>
                <CardTitle className="text-xl font-bold text-gray-900 dark:text-gray-100">Documentation</CardTitle>
                <CardDescription className="text-gray-600 dark:text-gray-300 text-sm">
                  Comprehensive guides and tutorials to help you master the GameGuild Editor and all its features
                </CardDescription>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="space-y-2 mb-6">
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-emerald-500 rounded-full"></div>
                    Getting started guide
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-emerald-500 rounded-full"></div>
                    Feature tutorials
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-emerald-500 rounded-full"></div>
                    Advanced techniques
                  </div>
                  <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-300">
                    <div className="w-2 h-2 bg-emerald-500 rounded-full"></div>
                    Configuration guides
                  </div>
                </div>
                <Link href="/editor/docs" className="block">
                  <Button
                    variant="outline"
                    className="w-full border-emerald-200 dark:border-emerald-700 text-emerald-600 dark:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-900 group-hover:bg-emerald-50 dark:group-hover:bg-emerald-900 transition-colors duration-300 bg-transparent"
                  >
                    Read Docs
                    <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform duration-300" />
                  </Button>
                </Link>
              </CardContent>
            </Card>
          </div>

          {/* Footer Section */}
          <div className="text-center mt-16 space-y-4">
            <div className="flex items-center justify-center gap-6 text-sm text-gray-500 dark:text-gray-400">
              <span className="flex items-center gap-2">
                <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
                All systems operational
              </span>
              <span>•</span>
              <span>Built for game developers</span>
              <span>•</span>
              <span>Open source</span>
            </div>

            <p className="text-gray-400 dark:text-gray-500 text-sm">
              Empowering the game development community with better content creation tools
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
