"use client"

import React from 'react'
import { Card as ShadcnCard } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Package, Calendar } from 'lucide-react'
import { type CollectionCard, type CardAction } from '../types'
import { formatDistanceToNow } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import { formatFileSize } from './utils'
import { CardActionsMenu } from './card-actions-menu'

interface CollectionCardComponentProps {
  card: CollectionCard
  isGrid: boolean
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onClick?: (card: CollectionCard) => void
}

export function CollectionCardComponent({ 
  card, 
  isGrid, 
  primaryActions, 
  secondaryActions, 
  onClick 
}: CollectionCardComponentProps) {
  if (isGrid) {
    return (
      <ShadcnCard 
        className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[180px] flex flex-col"
        onClick={() => onClick?.(card)}
      >
        <div className="p-3 sm:p-4 flex-1 flex flex-col">
          <div className="flex items-start justify-between gap-2 mb-2">
            <div className="flex items-center gap-2 flex-1 min-w-0">
              <Package className="w-5 h-5 text-purple-500 shrink-0" />
              <h3 className="text-sm sm:text-base font-semibold text-gray-900 dark:text-white truncate leading-tight">
                {card.name}
              </h3>
            </div>
            <CardActionsMenu
              card={card}
              primaryActions={primaryActions}
              secondaryActions={secondaryActions}
              buttonClassName="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0"
            />
          </div>

          {card.description && (
            <p className="text-xs text-gray-600 dark:text-gray-400 mb-2 line-clamp-2">
              {card.description}
            </p>
          )}

          {card.tags && card.tags.length > 0 && (
            <div className="flex flex-wrap gap-1 mb-2">
              {card.tags.slice(0, 3).map((tag, idx) => (
                <Badge 
                  key={idx}
                  variant="secondary" 
                  className="text-[10px] sm:text-xs px-1.5 py-0 max-w-[80px] truncate"
                >
                  {tag}
                </Badge>
              ))}
              {card.tags.length > 3 && (
                <Badge variant="outline" className="text-[10px] sm:text-xs px-1.5 py-0">
                  +{card.tags.length - 3}
                </Badge>
              )}
            </div>
          )}

          <div className="mt-auto flex items-center justify-between text-[10px] sm:text-xs text-gray-500 dark:text-gray-400">
            <div className="flex items-center gap-2">
              <span>{card.fileCount} {card.fileCount === 1 ? 'file' : 'files'}</span>
              <span>•</span>
              <span>{formatFileSize(card.totalSize)}</span>
            </div>
            <div className="flex items-center gap-1">
              <Calendar className="w-3 h-3" />
              <span className="hidden sm:inline">
                {formatDistanceToNow(new Date(card.updatedAt), { addSuffix: true, locale: ptBR })}
              </span>
              <span className="sm:hidden">
                {new Date(card.updatedAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })}
              </span>
            </div>
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
        <div className="w-24 h-14 bg-purple-100 dark:bg-purple-900/20 rounded flex items-center justify-center shrink-0">
          <Package className="w-6 h-6 text-purple-600 dark:text-purple-400" />
        </div>

        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-medium text-gray-900 dark:text-white truncate mb-0.5">
            {card.name}
          </h3>
          {card.description && (
            <p className="text-xs text-gray-600 dark:text-gray-400 truncate mb-1">
              {card.description}
            </p>
          )}
          <div className="flex items-center gap-2 flex-wrap">
            <Badge variant="secondary" className="text-xs py-0.5">
              {card.fileCount} {card.fileCount === 1 ? 'file' : 'files'}
            </Badge>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {formatFileSize(card.totalSize)}
            </span>
            {card.tags && card.tags.slice(0, 2).map((tag, idx) => (
              <Badge key={idx} variant="outline" className="text-xs py-0.5">
                {tag}
              </Badge>
            ))}
            {card.tags && card.tags.length > 2 && (
              <Badge variant="outline" className="text-xs py-0.5">
                +{card.tags.length - 2}
              </Badge>
            )}
          </div>
        </div>

        <div className="hidden sm:flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 min-w-[100px]">
          <Calendar className="w-3 h-3" />
          <span>
            {formatDistanceToNow(new Date(card.updatedAt), { addSuffix: true, locale: ptBR })}
          </span>
        </div>

        <CardActionsMenu
          card={card}
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          buttonClassName="h-6 w-6 p-0"
          iconClassName="h-3.5 w-3.5"
        />
      </div>
    </ShadcnCard>
  )
}
