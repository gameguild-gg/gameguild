"use client"

import { Button } from "@/components/ui/button"

interface ProjectPaginationProps {
  currentPage: number
  totalProjects: number
  itemsPerPage: number
  onPageChange: (page: number) => void
}

export function ProjectPagination({ currentPage, totalProjects, itemsPerPage, onPageChange }: ProjectPaginationProps) {
  const totalPages = Math.ceil(totalProjects / itemsPerPage)

  return (
    <div className="flex w-full items-center justify-between gap-3">
      <div className="text-xs text-muted-foreground">
        Showing {(currentPage - 1) * itemsPerPage + 1}–{Math.min(currentPage * itemsPerPage, totalProjects)} of{" "}
        {totalProjects}
      </div>
      <div className="flex items-center gap-1.5">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage === 1}
          className="h-8 px-2.5 text-xs"
        >
          Previous
        </Button>
        <div className="flex items-center gap-0.5">
          {Array.from({ length: totalPages }, (_, i) => i + 1)
            .filter((page) => {
              if (totalPages <= 7) return true
              if (page === 1 || page === totalPages) return true
              if (page >= currentPage - 1 && page <= currentPage + 1) return true
              return false
            })
            .map((page, index, array) => (
              <div key={page} className="flex items-center">
                {index > 0 && array[index - 1] !== page - 1 && (
                  <span className="px-1.5 text-xs text-muted-foreground">…</span>
                )}
                <Button
                  variant={currentPage === page ? "default" : "ghost"}
                  size="sm"
                  onClick={() => onPageChange(page)}
                  className="h-8 min-w-8 px-2 text-xs"
                >
                  {page}
                </Button>
              </div>
            ))}
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage === totalPages}
          className="h-8 px-2.5 text-xs"
        >
          Next
        </Button>
      </div>
    </div>
  )
}
