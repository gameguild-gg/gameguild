"use client"

import React from 'react'
import { Card as ShadcnCard } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { MoreVertical } from 'lucide-react'
import { type AssetCard, type CardAction } from '../types'
import { formatFileSize, getMimeTypeIcon, getMimeTypeBadgeColor } from './utils'
import { useAssetPreview } from './hooks'
import { CardActionsMenu } from './card-actions-menu'

interface AssetCardComponentProps {
  card: AssetCard
  isGrid: boolean
  isCompact?: boolean
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onClick?: (card: AssetCard) => void
}

export function AssetCardComponent({ 
  card, 
  isGrid, 
  isCompact = false, 
  primaryActions, 
  secondaryActions, 
  onClick 
}: AssetCardComponentProps) {
  const assetDataUrl = useAssetPreview(card.id, card.mimeType)
  const Icon = getMimeTypeIcon(card.mimeType)
  const isImage = card.mimeType.startsWith('image/')
  const mimeTypeCategory = card.mimeType.split('/')[0]
  const mimeTypeDetail = card.mimeType.split('/')[1]

  if (isGrid) {
    if (isCompact) {
      return (
        <ShadcnCard 
          className="group relative border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer overflow-hidden aspect-video"
          onClick={() => onClick?.(card)}
        >
          <div className="absolute inset-0 bg-gray-100 dark:bg-gray-900 flex items-center justify-center">
            {isImage && assetDataUrl ? (
              <img 
                src={assetDataUrl} 
                alt={card.name}
                className="w-full h-full object-cover"
              />
            ) : (
              <Icon className="w-8 h-8 text-gray-400 dark:text-gray-600" />
            )}
          </div>

          <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity">
            <DropdownMenu>
              <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                <Button 
                  variant="secondary" 
                  size="sm" 
                  className="h-7 w-7 p-0 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm hover:bg-white dark:hover:bg-gray-800 shadow-md"
                >
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <div className="px-2 py-2 border-b border-gray-200 dark:border-gray-700">
                  <div className="text-xs font-medium text-gray-900 dark:text-white truncate mb-1">
                    {card.name}
                  </div>
                  <div className="flex flex-wrap gap-1 mb-1">
                    <Badge className={`text-[10px] px-1.5 py-0.5 ${getMimeTypeBadgeColor(card.mimeType)}`}>
                      {mimeTypeCategory}/{mimeTypeDetail}
                    </Badge>
                  </div>
                  <div className="text-[10px] text-gray-500 dark:text-gray-400">
                    {formatFileSize(card.size)}
                  </div>
                </div>
                
                {primaryActions.map((action, idx) => (
                  <DropdownMenuItem 
                    key={idx}
                    onClick={(e) => {
                      e.stopPropagation()
                      action.onClick(card)
                    }}
                  >
                    {action.icon}
                    <span className="ml-2">{action.label}</span>
                  </DropdownMenuItem>
                ))}
                {secondaryActions.length > 0 && <DropdownMenuSeparator />}
                {secondaryActions.map((action, idx) => (
                  <DropdownMenuItem 
                    key={idx}
                    onClick={(e) => {
                      e.stopPropagation()
                      action.onClick(card)
                    }}
                    className={action.variant === 'destructive' ? 'text-red-600 dark:text-red-400' : ''}
                  >
                    {action.icon}
                    <span className="ml-2">{action.label}</span>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </ShadcnCard>
      )
    }

    return (
      <ShadcnCard 
        className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer overflow-hidden flex flex-col p-0"
        onClick={() => onClick?.(card)}
      >
        <div className="bg-gray-100 dark:bg-gray-900 flex items-center justify-center min-h-[120px] m-0">
          {isImage && assetDataUrl ? (
            <img 
              src={assetDataUrl} 
              alt={card.name}
              className="w-full h-full object-cover"
            />
          ) : (
            <div className="p-6">
              <Icon className="w-8 h-8 text-gray-400 dark:text-gray-600" />
            </div>
          )}
        </div>

        <div className="p-2 sm:p-3 flex flex-col gap-2 min-h-[70px] sm:min-h-[80px]">
          <div className="flex items-start justify-between gap-2">
            <h3 className="text-xs sm:text-sm font-medium text-gray-900 dark:text-white truncate leading-tight flex-1">
              {card.name}
            </h3>
            <CardActionsMenu
              card={card}
              primaryActions={primaryActions}
              secondaryActions={secondaryActions}
              buttonClassName="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0"
            />
          </div>

          <div className="flex flex-wrap gap-1">
            <Badge className={`text-[9px] sm:text-xs ${getMimeTypeBadgeColor(card.mimeType)}`}>
              {mimeTypeCategory}
            </Badge>
            <Badge variant="outline" className="text-[9px] sm:text-xs">
              {mimeTypeDetail}
            </Badge>
          </div>

          <div className="flex items-center justify-between text-[10px] sm:text-xs text-gray-500 dark:text-gray-400 mt-auto">
            <span>{formatFileSize(card.size)}</span>
            {card.projects && card.projects.length > 0 && (
              <Badge variant="secondary" className="text-[9px] sm:text-xs hidden sm:inline-flex">
                {card.projects.length} {card.projects.length === 1 ? 'project' : 'projects'}
              </Badge>
            )}
          </div>
        </div>
      </ShadcnCard>
    )
  }

  // List view
  return (
    <ShadcnCard 
      className="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer"
      onClick={() => onClick?.(card)}
    >
      <div className="px-3 flex items-center gap-3">
        <div className="w-24 h-14 bg-gray-100 dark:bg-gray-900 rounded flex items-center justify-center shrink-0 overflow-hidden">
          {isImage && assetDataUrl ? (
            <img 
              src={assetDataUrl} 
              alt={card.name}
              className="w-full h-full object-cover"
            />
          ) : (
            <Icon className="w-5 h-5 text-gray-400 dark:text-gray-600" />
          )}
        </div>

        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-medium text-gray-900 dark:text-white truncate mb-0.5">
            {card.name}
          </h3>
          <div className="flex items-center gap-1.5 flex-wrap">
            <Badge className={`text-xs py-0.5 ${getMimeTypeBadgeColor(card.mimeType)}`}>
              {mimeTypeCategory}
            </Badge>
            <Badge variant="outline" className="text-xs py-0.5">
              {mimeTypeDetail}
            </Badge>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {formatFileSize(card.size)}
            </span>
            {card.projects && card.projects.length > 0 && (
              <Badge variant="secondary" className="text-xs py-0.5">
                {card.projects.length} {card.projects.length === 1 ? 'project' : 'projects'}
              </Badge>
            )}
          </div>
        </div>

        <CardActionsMenu
          card={card}
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          buttonClassName="h-6 w-6 p-0 shrink-0"
          iconClassName="h-3.5 w-3.5"
        />
      </div>
    </ShadcnCard>
  )
}
