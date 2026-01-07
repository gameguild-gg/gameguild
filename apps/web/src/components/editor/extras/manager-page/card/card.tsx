"use client"

import React from 'react'
import { type ManagerCard, type CardAction, type ViewMode } from '../types'
import { ProjectCardComponent } from './project-card'
import { AssetCardComponent } from './asset-card'
import { CollectionCardComponent } from './collection-card'

interface ManagerCardProps {
  card: ManagerCard
  viewMode: ViewMode
  isCompact?: boolean
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onClick?: (card: ManagerCard) => void
}

export function ManagerCardComponent({ 
  card, 
  viewMode, 
  isCompact = false, 
  primaryActions, 
  secondaryActions, 
  onClick 
}: ManagerCardProps) {
  const isGrid = viewMode === 'grid'

  if (card.type === 'project') {
    return (
      <ProjectCardComponent
        card={card}
        isGrid={isGrid}
        isCompact={isCompact}
        primaryActions={primaryActions}
        secondaryActions={secondaryActions}
        onClick={onClick}
      />
    )
  }

  if (card.type === 'asset') {
    return (
      <AssetCardComponent
        card={card}
        isGrid={isGrid}
        isCompact={isCompact}
        primaryActions={primaryActions}
        secondaryActions={secondaryActions}
        onClick={onClick}
      />
    )
  }

  return (
    <CollectionCardComponent
      card={card}
      isGrid={isGrid}
      primaryActions={primaryActions}
      secondaryActions={secondaryActions}
      onClick={onClick}
    />
  )
}
