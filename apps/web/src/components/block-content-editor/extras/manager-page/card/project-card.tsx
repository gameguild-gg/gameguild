"use client"

import React from 'react'
import { Card as ShadcnCard } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Calendar } from 'lucide-react'
import { type ProjectCard, type CardAction } from '../types'
import { formatDistanceToNow } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import { formatFileSize, getStorageIcon } from './utils'
import { CardActionsMenu } from './card-actions-menu'

const PROJECT_TYPE_BADGE_CLASS: Record<NonNullable<ProjectCard['projectType']>, string> = {
  document: "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300 border-indigo-200 dark:border-indigo-800",
  quiz: "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300 border-amber-200 dark:border-amber-800",
  general: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300 border-slate-200 dark:border-slate-700",
}

const PROJECT_TYPE_LABEL: Record<NonNullable<ProjectCard['projectType']>, string> = {
  document: "Document",
  quiz: "Quiz",
  general: "General",
}

function ProjectTypeBadge({ type, className = "" }: { type: ProjectCard['projectType']; className?: string }) {
  const t = type ?? 'general'
  return (
    <Badge variant="outline" className={`${PROJECT_TYPE_BADGE_CLASS[t]} ${className}`}>
      {PROJECT_TYPE_LABEL[t]}
    </Badge>
  )
}

interface ProjectCardComponentProps {
  card: ProjectCard
  isGrid: boolean
  isCompact?: boolean
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onClick?: (card: ProjectCard) => void
}

export function ProjectCardComponent({ 
  card, 
  isGrid, 
  isCompact = false, 
  primaryActions, 
  secondaryActions, 
  onClick 
}: ProjectCardComponentProps) {
  if (isGrid) {
    if (isCompact) {
      return (
        <ShadcnCard 
          className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[100px] flex flex-col"
          onClick={() => onClick?.(card)}
        >
          <div className="p-2 flex-1 flex flex-col gap-1.5">
            <div className="flex items-start justify-between gap-1">
              <h3 className="text-[11px] font-semibold text-gray-900 dark:text-white truncate leading-tight flex-1">
                {card.name}
              </h3>
              <CardActionsMenu
                card={card}
                primaryActions={primaryActions}
                secondaryActions={secondaryActions}
                buttonClassName="h-5 w-5 p-0 shrink-0"
                iconClassName="h-3 w-3"
              />
            </div>

            {card.tags.length > 0 && (
              <div className="flex gap-1">
                <Badge variant="secondary" className="text-[9px] px-1 py-0 max-w-[60px] truncate">
                  {card.tags[0]}
                </Badge>
                {card.tags.length > 1 && (
                  <Badge variant="outline" className="text-[9px] px-1 py-0">
                    +{card.tags.length - 1}
                  </Badge>
                )}
              </div>
            )}

            <ProjectTypeBadge type={card.projectType} className="text-[9px] px-1 py-0 w-fit" />

            <div className="mt-auto flex items-center justify-between text-[9px] text-gray-500 dark:text-gray-400">
              <div className="flex items-center gap-0.5">
                {React.createElement(getStorageIcon(card.storageType))}
              </div>
              <span className="truncate">
                {formatFileSize(card.size).split(' ')[0]}
              </span>
            </div>
          </div>
        </ShadcnCard>
      )
    }

    return (
      <ShadcnCard 
        className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[180px] flex flex-col"
        onClick={() => onClick?.(card)}
      >
        <div className="p-3 sm:p-4 flex-1 flex flex-col">
          <div className="flex items-start justify-between gap-2 mb-2">
            <h3 className="text-sm sm:text-base font-semibold text-gray-900 dark:text-white truncate leading-tight flex-1">
              {card.name}
            </h3>
            <CardActionsMenu
              card={card}
              primaryActions={primaryActions}
              secondaryActions={secondaryActions}
              buttonClassName="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0"
            />
          </div>

          <div className="flex flex-wrap gap-1 mb-2 min-h-[24px]">
            <ProjectTypeBadge type={card.projectType} className="text-[10px] sm:text-xs px-1.5 py-0" />
            {card.tags.slice(0, 2).map((tag, idx) => (
              <Badge key={idx} variant="secondary" className="text-[10px] sm:text-xs px-1.5 py-0 max-w-[100px] truncate">
                {tag}
              </Badge>
            ))}
            {card.tags.length > 2 && (
              <Badge variant="outline" className="text-[10px] sm:text-xs px-1.5 py-0">
                +{card.tags.length - 2}
              </Badge>
            )}
          </div>

          <div className="mt-auto flex items-center justify-between text-[10px] sm:text-xs text-gray-500 dark:text-gray-400">
            <div className="flex items-center gap-1">
              {React.createElement(getStorageIcon(card.storageType))}
              <span className="hidden sm:inline">{formatFileSize(card.size)}</span>
              <span className="sm:hidden">{formatFileSize(card.size).split(' ')[0]}</span>
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
      <div className="px-3 flex items-center justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-0.5">
            <h3 className="text-base font-semibold text-gray-900 dark:text-white truncate">
              {card.name}
            </h3>
            {card.storageType && (
              <div className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
                {React.createElement(getStorageIcon(card.storageType))}
                <span className="capitalize">{card.storageType}</span>
              </div>
            )}
          </div>
          <div className="flex items-center gap-3 text-sm text-gray-500 dark:text-gray-400">
            <div className="flex items-center gap-1">
              <Calendar className="w-3.5 h-3.5" />
              <span>
                {formatDistanceToNow(new Date(card.updatedAt), { addSuffix: true, locale: ptBR })}
              </span>
            </div>
            <span>{formatFileSize(card.size)}</span>
          </div>
        </div>

        <div className="flex flex-wrap gap-1 max-w-md">
          <ProjectTypeBadge type={card.projectType} className="text-xs py-0.5" />
          {card.tags.slice(0, 3).map((tag, idx) => (
            <Badge key={idx} variant="secondary" className="text-xs py-0.5">
              {tag}
            </Badge>
          ))}
          {card.tags.length > 3 && (
            <Badge variant="outline" className="text-xs py-0.5">
              +{card.tags.length - 3}
            </Badge>
          )}
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
