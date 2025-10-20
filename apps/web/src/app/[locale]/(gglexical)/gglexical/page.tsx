"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ArrowRight, Eye, Blocks, Plus, Search, Filter, MoreVertical, Calendar, Tag, User } from "lucide-react"
import Link from "next/link"
import React, { useState } from 'react';
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { AdvancedFilters } from "@/components/editor/extras/project-dialog/advanced-filters"

// Mock data for demonstration
const mockProjects = [
  {
    id: "1",
    name: "A Origem e Evolução dos Jogos de Corrida: Das Pistas Virtuais Iniciais aos Simuladores Modernos",
    data: "content",
    tags: ["games", "racing", "history"],
    size: 1024,
    createdAt: "2024-10-15",
    updatedAt: "2024-10-20",
    storageType: "local" as const,
    isLocallyAvailable: true
  },
  {
    id: "2", 
    name: "A Origem e Evolução dos Jogos de Terror: Uma Jornada Aterrorizante",
    data: "content",
    tags: ["horror", "games", "psychology"],
    size: 2048,
    createdAt: "2024-10-10",
    updatedAt: "2024-10-18",
    storageType: "gameguild-cloud" as const,
    isLocallyAvailable: true
  },
  {
    id: "3",
    name: "A Evolução dos Jogos de Estratégia em Tempo Real (RTS): Das Origens ao Presente", 
    data: "content",
    tags: ["RTS", "strategy", "evolution"],
    size: 1536,
    createdAt: "2024-10-05",
    updatedAt: "2024-10-16",
    storageType: "google-drive" as const,
    isLocallyAvailable: false
  },
  {
    id: "4",
    name: "Confira os jogos grátis da Epic Games da última semana de março",
    data: "content",
    tags: ["epic-games", "free", "news"],
    size: 896,
    createdAt: "2024-03-25",
    updatedAt: "2024-03-28",
    storageType: "local" as const,
    isLocallyAvailable: true
  },
  {
    id: "5",
    name: "Analogue 3D: O Renascimento do Nintendo 64 em 4K Chega em 2025",
    data: "content",
    tags: ["nintendo", "retro", "hardware"],
    size: 1200,
    createdAt: "2024-03-20",
    updatedAt: "2024-03-22",
    storageType: "gameguild-cloud" as const,
    isLocallyAvailable: true
  },
  {
    id: "6",
    name: "A Origem e Evolução dos Jogos de Aventura: Uma Jornada pela História dos Games",
    data: "content",
    tags: ["adventure", "history", "games"],
    size: 1800,
    createdAt: "2024-03-15",
    updatedAt: "2024-03-19",
    storageType: "google-drive" as const,
    isLocallyAvailable: true
  },
  {
    id: "7",
    name: "10 Dispositivos Inusitados que Rodaram Doom: Explorando a Criatividade dos Desenvolvedores",
    data: "content",
    tags: ["doom", "retro", "creativity"],
    size: 1400,
    createdAt: "2024-03-10",
    updatedAt: "2024-03-18",
    storageType: "local" as const,
    isLocallyAvailable: true
  },
  {
    id: "8",
    name: "Como Nomear Jogos Eletrônicos: Estratégias e Boas Práticas para Desenvolvedores",
    data: "content",
    tags: ["development", "naming", "strategy"],
    size: 1100,
    createdAt: "2024-03-05",
    updatedAt: "2024-03-16",
    storageType: "gameguild-cloud" as const,
    isLocallyAvailable: false
  },
  {
    id: "9",
    name: "INACREDITÁVEL! Devs conseguem rodar DOOM em coisas inusitadas!",
    data: "content",
    tags: ["doom", "viral", "tech"],
    size: 750,
    createdAt: "2024-02-28",
    updatedAt: "2024-03-14",
    storageType: "local" as const,
    isLocallyAvailable: true
  }
]

export default function HomePage() {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")
  const [storageTypeFilter, setStorageTypeFilter] = useState<"local" | "gameguild-cloud" | "google-drive" | undefined>()
  const [itemsPerPage, setItemsPerPage] = useState(8)
  const [currentPage, setCurrentPage] = useState(1)
  const [showFilters, setShowFilters] = useState(false)
  
  // Advanced filters
  const [authorFilter, setAuthorFilter] = useState("")
  const [statusFilter, setStatusFilter] = useState<"all" | "draft" | "published" | "scheduled">("all")
  const [dateFromFilter, setDateFromFilter] = useState("")
  const [dateToFilter, setDateToFilter] = useState("")
  const [accessFilter, setAccessFilter] = useState<"all" | "all-access" | "all-authors">("all")
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false)

  // Extract available tags from projects
  const availableTags = Array.from(
    new Set(mockProjects.flatMap(project => project.tags))
  ).map(tag => ({ name: tag }))

  // Filter projects based on search and filters
  const filteredProjects = mockProjects.filter(project => {
    const matchesSearch = project.name.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesTags = selectedTags.length === 0 || 
      (tagFilterMode === "any" 
        ? selectedTags.some(tag => project.tags.includes(tag))
        : selectedTags.every(tag => project.tags.includes(tag)))
    const matchesStorage = !storageTypeFilter || project.storageType === storageTypeFilter
    const matchesAuthor = !authorFilter || "Miguel Moroni".toLowerCase().includes(authorFilter.toLowerCase())
    const matchesStatus = statusFilter === "all" || statusFilter === "draft" // All mock projects are drafts
    const matchesDateFrom = !dateFromFilter || new Date(project.updatedAt) >= new Date(dateFromFilter)
    const matchesDateTo = !dateToFilter || new Date(project.updatedAt) <= new Date(dateToFilter)
    
    return matchesSearch && matchesTags && matchesStorage && matchesAuthor && matchesStatus && matchesDateFrom && matchesDateTo
  })

  const totalPages = Math.ceil(filteredProjects.length / itemsPerPage)

  const handleProjectOpen = (projectId: string) => {
    // Navigate to studio with the project
    window.location.href = `/gglexical/studio?project=${projectId}`
  }

  const handleProjectView = (projectId: string) => {
    // Navigate to viewer with the project
    window.location.href = `/gglexical/viewer?project=${projectId}`
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="flex h-screen">
        {/* Left Sidebar */}
        <div className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
          {/* Logo/Header */}
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                <Blocks className="w-5 h-5 text-white" />
              </div>
              <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">GameGuild</h1>
            </div>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Content Platform</p>
          </div>

          {/* Navigation */}
          <div className="flex-1 p-4 space-y-2">
            <Button 
              asChild 
              className="w-full justify-start bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Link href="/gglexical/studio">
                <Blocks className="w-4 h-4 mr-3" />
                Studio
              </Link>
            </Button>
            
            <Button 
              asChild 
              variant="ghost" 
              className="w-full justify-start hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              <Link href="/gglexical/viewer">
                <Eye className="w-4 h-4 mr-3" />
                Viewer
              </Link>
            </Button>
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
              All systems operational
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="flex-1 flex flex-col">
          {/* Top Header */}
          <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Posts</h2>
                <p className="text-gray-600 dark:text-gray-400">
                  {filteredProjects.length} of {mockProjects.length} projects
                </p>
              </div>
              <div className="flex items-center gap-3">
                <select
                  className="rounded border bg-background px-3 py-2 text-sm border-gray-300 dark:border-gray-600"
                  defaultValue="newest"
                >
                  <option value="newest">Newest first</option>
                  <option value="oldest">Oldest first</option>
                  <option value="name">Name A-Z</option>
                  <option value="name-desc">Name Z-A</option>
                </select>
                <Button 
                  variant="outline"
                  onClick={() => setShowFilters(!showFilters)}
                  className="gap-2"
                >
                  <Filter className="w-4 h-4" />
                  Filters
                </Button>
                <Button 
                  variant="outline"
                  onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
                  className="gap-2"
                >
                  <Calendar className="w-4 h-4" />
                  Advanced
                </Button>
                <Button className="gap-2 bg-blue-600 hover:bg-blue-700">
                  <Plus className="w-4 h-4" />
                  New post
                </Button>
              </div>
            </div>
            
            {/* Search Bar */}
            <div className="relative">
              <Search className="w-4 h-4 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <Input
                placeholder="Search posts..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {/* Filters */}
          <ProjectSearchFilters
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            selectedTags={selectedTags}
            onTagsChange={setSelectedTags}
            availableTags={availableTags}
            tagFilterMode={tagFilterMode}
            onTagFilterModeChange={setTagFilterMode}
            storageTypeFilter={storageTypeFilter}
            onStorageTypeFilterChange={setStorageTypeFilter}
            itemsPerPage={itemsPerPage}
            onItemsPerPageChange={setItemsPerPage}
            showFilters={showFilters}
            forceVerticalLayout={false}
          />

          {/* Advanced Filters */}
          <AdvancedFilters
            authorFilter={authorFilter}
            onAuthorFilterChange={setAuthorFilter}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            dateFromFilter={dateFromFilter}
            onDateFromFilterChange={setDateFromFilter}
            dateToFilter={dateToFilter}
            onDateToFilterChange={setDateToFilter}
            accessFilter={accessFilter}
            onAccessFilterChange={setAccessFilter}
            showAdvanced={showAdvancedFilters}
          />

          {/* Projects List */}
          <div className="flex-1 p-6">
            <div className="space-y-4">
              {filteredProjects.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((project) => (
                <Card key={project.id} className="group hover:shadow-md transition-all duration-200">
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-3 mb-2">
                          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors truncate">
                            {project.name}
                          </h3>
                          <div className="flex gap-1 flex-shrink-0">
                            {project.tags.slice(0, 2).map((tag) => (
                              <span
                                key={tag}
                                className="inline-flex items-center rounded-md bg-blue-50 px-2 py-1 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10 dark:bg-blue-900/50 dark:text-blue-300"
                              >
                                {tag}
                              </span>
                            ))}
                          </div>
                        </div>
                        <div className="flex items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
                          <span className="flex items-center gap-1">
                            <User className="w-4 h-4" />
                            Miguel Moroni
                          </span>
                          <span className="flex items-center gap-1">
                            <Calendar className="w-4 h-4" />
                            {new Date(project.updatedAt).toLocaleDateString()}
                          </span>
                          <span className={`px-2 py-1 rounded-full text-xs ${
                            project.storageType === 'local' ? 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200' :
                            project.storageType === 'gameguild-cloud' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200' :
                            'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                          }`}>
                            Draft
                          </span>
                        </div>
                      </div>
                      
                      {/* Action Buttons */}
                      <div className="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                        <Button
                          onClick={() => handleProjectOpen(project.id)}
                          size="sm"
                          className="bg-blue-600 hover:bg-blue-700 text-white"
                        >
                          <Blocks className="w-4 h-4 mr-2" />
                          Studio
                        </Button>
                        <Button
                          onClick={() => handleProjectView(project.id)}
                          size="sm"
                          variant="outline"
                          className="border-purple-200 text-purple-700 hover:bg-purple-50 dark:border-purple-700 dark:text-purple-300 dark:hover:bg-purple-900/20"
                        >
                          <Eye className="w-4 h-4 mr-2" />
                          Viewer
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                        >
                          <MoreVertical className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-6">
                <ProjectPagination
                  currentPage={currentPage}
                  totalProjects={filteredProjects.length}
                  itemsPerPage={itemsPerPage}
                  onPageChange={setCurrentPage}
                />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
