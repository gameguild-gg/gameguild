"use client"

import React from 'react'
import { type ManagerCard, type CardAction, type ViewMode } from './types'
import { ManagerCardComponent } from './card/card'

interface GridViewProps {
  cards: ManagerCard[]
  columns: number
  viewMode: ViewMode
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onCardClick?: (card: ManagerCard) => void
}

const getGridClass = (columns: number): string => {
  switch (columns) {
    case 4:
      return 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4'
    case 5:
      return 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5'
    case 6:
      return 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6'
    case 9:
      return 'grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-7 xl:grid-cols-9'
    case 12:
      return 'grid-cols-3 sm:grid-cols-6 md:grid-cols-8 lg:grid-cols-10 xl:grid-cols-12'
    case 15:
      return 'grid-cols-4 sm:grid-cols-6 md:grid-cols-9 lg:grid-cols-12 xl:grid-cols-15'
    default:
      return 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5'
  }
}

export function GridView({ 
  cards, 
  columns, 
  viewMode,
  primaryActions, 
  secondaryActions, 
  onCardClick 
}: GridViewProps) {
  const gridClass = getGridClass(columns)
  const isCompact = columns >= 9 // Compact mode for 9+ columns

  if (cards.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 dark:text-gray-400">
        <p>Nenhum item encontrado</p>
      </div>
    )
  }

  return (
    <div className={`grid ${gridClass} gap-4`}>
      {cards.map((card) => (
        <ManagerCardComponent
          key={card.id}
          card={card}
          viewMode={viewMode}
          isCompact={isCompact}
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          onClick={onCardClick}
        />
      ))}
    </div>
  )
}
