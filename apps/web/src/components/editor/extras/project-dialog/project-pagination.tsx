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
    <div className="flex items-center justify-between mt-4 pt-4 border-t dark:border-gray-700">
      <div className="text-sm text-gray-500 dark:text-gray-400">
        Showing {(currentPage - 1) * itemsPerPage + 1} to {Math.min(currentPage * itemsPerPage, totalProjects)} of{" "}
        {totalProjects} projects
      </div>
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage === 1}
          className="bg-transparent"
        >
          Previous
        </Button>
        <div className="flex items-center gap-1">
          {Array.from({ length: totalPages }, (_, i) => i + 1)
            .filter((page) => {
              if (totalPages <= 7) return true
              if (page === 1 || page === totalPages) return true
              if (page >= currentPage - 1 && page <= currentPage + 1) return true
              return false
            })
            .map((page, index, array) => (
              <div key={page} className="flex items-center">
                {index > 0 && array[index - 1] !== page - 1 && <span className="px-2 text-gray-400">...</span>}
                <Button
                  variant={currentPage === page ? "default" : "ghost"}
                  size="sm"
                  onClick={() => onPageChange(page)}
                  className={currentPage === page ? "" : "bg-transparent"}
                >
                  {page}
                </Button>
              </div>
            ))}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage === totalPages}
          className="bg-transparent"
        >
          Next
        </Button>
      </div>
    </div>
  )
}
