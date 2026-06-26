import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Search, Filter, X } from "lucide-react"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface AssetFiltersProps {
  searchTerm: string
  onSearchChange: (value: string) => void
  mimeTypeFilter: string
  onMimeTypeFilterChange: (value: string) => void
  projectFilter: string
  onProjectFilterChange: (value: string) => void
  usageFilter: "all" | "used" | "unused"
  onUsageFilterChange: (value: "all" | "used" | "unused") => void
  itemsPerPage: number
  onItemsPerPageChange: (value: number) => void
  availableProjects: Array<{ id: string; name: string }>
}

export function AssetFilters({
  searchTerm,
  onSearchChange,
  mimeTypeFilter,
  onMimeTypeFilterChange,
  projectFilter,
  onProjectFilterChange,
  usageFilter,
  onUsageFilterChange,
  itemsPerPage,
  onItemsPerPageChange,
  availableProjects,
}: AssetFiltersProps) {
  const hasActiveFilters = searchTerm || mimeTypeFilter !== "all" || projectFilter !== "all" || usageFilter !== "all"

  const clearAllFilters = () => {
    onSearchChange("")
    onMimeTypeFilterChange("all")
    onProjectFilterChange("all")
    onUsageFilterChange("all")
  }

  return (
    <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-6">
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="flex items-center gap-3">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Search assets by name..."
              value={searchTerm}
              onChange={(e) => onSearchChange(e.target.value)}
              className="pl-10"
            />
          </div>
          {hasActiveFilters && (
            <Button variant="ghost" size="sm" onClick={clearAllFilters} className="gap-2">
              <X className="w-4 h-4" />
              Clear filters
            </Button>
          )}
        </div>

        {/* Filters Row */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* MIME Type Filter */}
          <div className="space-y-2">
            <Label className="text-xs text-gray-600 dark:text-gray-400">File Type</Label>
            <Select value={mimeTypeFilter} onValueChange={onMimeTypeFilterChange}>
              <SelectTrigger>
                <SelectValue placeholder="All types" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All types</SelectItem>
                <SelectItem value="image">Images</SelectItem>
                <SelectItem value="video">Videos</SelectItem>
                <SelectItem value="audio">Audio</SelectItem>
                <SelectItem value="text">Text</SelectItem>
                <SelectItem value="application">Applications</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Project Filter */}
          <div className="space-y-2">
            <Label className="text-xs text-gray-600 dark:text-gray-400">Project</Label>
            <Select value={projectFilter} onValueChange={onProjectFilterChange}>
              <SelectTrigger>
                <SelectValue placeholder="All projects" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All projects</SelectItem>
                {availableProjects.map((project) => (
                  <SelectItem key={project.id} value={project.id}>
                    {project.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Usage Filter */}
          <div className="space-y-2">
            <Label className="text-xs text-gray-600 dark:text-gray-400">Usage</Label>
            <Select value={usageFilter} onValueChange={onUsageFilterChange}>
              <SelectTrigger>
                <SelectValue placeholder="All assets" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All assets</SelectItem>
                <SelectItem value="used">Used in projects</SelectItem>
                <SelectItem value="unused">Unused</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Items Per Page */}
          <div className="space-y-2">
            <Label className="text-xs text-gray-600 dark:text-gray-400">Items per page</Label>
            <Select value={itemsPerPage.toString()} onValueChange={(v) => onItemsPerPageChange(Number(v))}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="12">12</SelectItem>
                <SelectItem value="24">24</SelectItem>
                <SelectItem value="48">48</SelectItem>
                <SelectItem value="96">96</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>
    </div>
  )
}
