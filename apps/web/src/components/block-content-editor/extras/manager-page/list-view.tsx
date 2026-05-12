"use client"

import React from 'react'
import { type ManagerCard, type CardAction, type ViewMode } from './types'
import { ManagerCardComponent } from './card/card'

interface ListViewProps {
  cards: ManagerCard[]
  columns: number
  viewMode: ViewMode
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onCardClick?: (card: ManagerCard) => void
}

const getListGridClass = (columns: number): string => {
  if (columns === 2) {
    return 'grid-cols-1 lg:grid-cols-2'
  }
  return 'grid-cols-1'
}

export function ListView({ 
  cards, 
  columns, 
  viewMode,
  primaryActions, 
  secondaryActions, 
  onCardClick 
}: ListViewProps) {
  const gridClass = getListGridClass(columns)

  if (cards.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500 dark:text-gray-400">
        <p>Nenhum item encontrado</p>
      </div>
    )
  }

  return (
    <div className={`grid ${gridClass} gap-3`}>
      {cards.map((card) => (
        <ManagerCardComponent
          key={card.id}
          card={card}
          viewMode={viewMode}
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          onClick={onCardClick}
        />
      ))}
    </div>
  )
}
