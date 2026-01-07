"use client"

import React, { useEffect, useState } from 'react'
import { Card as ShadcnCard } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { 
  MoreVertical, 
  FileText, 
  Image as ImageIcon,
  Video,
  Music,
  File,
  Calendar,
  HardDrive,
  Cloud,
  Package
} from 'lucide-react'
import { type ManagerCard, type CardAction, type ViewMode } from './types'
import { formatDistanceToNow } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import { assetManager } from '@/lib/storage/assets/asset-manager'

interface ManagerCardProps {
  card: ManagerCard
  viewMode: ViewMode
  isCompact?: boolean
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  onClick?: (card: ManagerCard) => void
}

const getMimeTypeIcon = (mimeType: string) => {
  if (mimeType.startsWith('image/')) return ImageIcon
  if (mimeType.startsWith('video/')) return Video
  if (mimeType.startsWith('audio/')) return Music
  if (mimeType.startsWith('text/')) return FileText
  return File
}

const getMimeTypeBadgeColor = (mimeType: string) => {
  if (mimeType.startsWith('image/')) return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
  if (mimeType.startsWith('video/')) return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200'
  if (mimeType.startsWith('audio/')) return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
  if (mimeType.startsWith('text/')) return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200'
  return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200'
}

const formatFileSize = (bytes: number) => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`
}

const getStorageIcon = (storageType?: string) => {
  switch (storageType) {
    case 'gameguild-cloud':
      return <Cloud className="w-3 h-3" />
    case 'google-drive':
      return <Package className="w-3 h-3" />
    case 'local':
    default:
      return <HardDrive className="w-3 h-3" />
  }
}

export function ManagerCardComponent({ card, viewMode, isCompact = false, primaryActions, secondaryActions, onClick }: ManagerCardProps) {
  const isGrid = viewMode === 'grid'
  const [assetDataUrl, setAssetDataUrl] = useState<string | null>(null)

  // Load asset thumbnail for images
  useEffect(() => {
    const loadAssetData = async () => {
      if (card.type === 'asset' && card.mimeType.startsWith('image/')) {
        try {
          const assetData = await assetManager.getAsset(card.id)
          if (assetData && assetData.data) {
            setAssetDataUrl(assetData.data)
          }
        } catch (error) {
          console.error(`Failed to load asset ${card.id}:`, error)
        }
      }
    }

    loadAssetData()
  }, [card])

  // Grid View
  if (isGrid) {
    if (card.type === 'project') {
      // Compact mode for dense grids (9+ columns)
      if (isCompact) {
        return (
          <ShadcnCard 
            className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[100px] flex flex-col"
            onClick={() => onClick?.(card)}
          >
            <div className="p-2 flex-1 flex flex-col gap-1.5">
              {/* Header - compact */}
              <div className="flex items-start justify-between gap-1">
                <h3 className="text-[11px] font-semibold text-gray-900 dark:text-white truncate leading-tight flex-1">
                  {card.name}
                </h3>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                    <Button variant="ghost" size="sm" className="h-5 w-5 p-0 shrink-0">
                      <MoreVertical className="h-3 w-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
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

              {/* Only show first tag or count */}
              {card.tags.length > 0 && (
                <div className="flex gap-1">
                  <Badge 
                    variant="secondary" 
                    className="text-[9px] px-1 py-0 max-w-[60px] truncate"
                  >
                    {card.tags[0]}
                  </Badge>
                  {card.tags.length > 1 && (
                    <Badge variant="outline" className="text-[9px] px-1 py-0">
                      +{card.tags.length - 1}
                    </Badge>
                  )}
                </div>
              )}

              {/* Footer - compact */}
              <div className="mt-auto flex items-center justify-between text-[9px] text-gray-500 dark:text-gray-400">
                <div className="flex items-center gap-0.5">
                  {getStorageIcon(card.storageType)}
                </div>
                <span className="truncate">
                  {formatFileSize(card.size).split(' ')[0]}
                </span>
              </div>
            </div>
          </ShadcnCard>
        )
      }

      // Normal mode
      return (
        <ShadcnCard 
          className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[180px] flex flex-col"
          onClick={() => onClick?.(card)}
        >
          <div className="p-3 sm:p-4 flex-1 flex flex-col">
            {/* Header */}
            <div className="flex items-start justify-between gap-2 mb-2">
              <h3 className="text-sm sm:text-base font-semibold text-gray-900 dark:text-white truncate leading-tight flex-1">
                {card.name}
              </h3>
              <DropdownMenu>
                <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                  <Button variant="ghost" size="sm" className="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0">
                    <MoreVertical className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
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

            {/* Tags - Limited to 2 */}
            <div className="flex flex-wrap gap-1 mb-2 min-h-[24px]">
              {card.tags.slice(0, 2).map((tag, idx) => (
                <Badge 
                  key={idx} 
                  variant="secondary" 
                  className="text-[10px] sm:text-xs px-1.5 py-0 max-w-[100px] truncate"
                >
                  {tag}
                </Badge>
              ))}
              {card.tags.length > 2 && (
                <Badge variant="outline" className="text-[10px] sm:text-xs px-1.5 py-0">
                  +{card.tags.length - 2}
                </Badge>
              )}
            </div>

            {/* Footer - pushed to bottom */}
            <div className="mt-auto flex items-center justify-between text-[10px] sm:text-xs text-gray-500 dark:text-gray-400">
              <div className="flex items-center gap-1">
                {getStorageIcon(card.storageType)}
                <span className="hidden sm:inline">
                  {formatFileSize(card.size)}
                </span>
                <span className="sm:hidden">
                  {formatFileSize(card.size).split(' ')[0]}
                </span>
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
    } else if (card.type === 'asset') {
      // Asset card
      const Icon = getMimeTypeIcon(card.mimeType)
      const isImage = card.mimeType.startsWith('image/')
      const mimeTypeCategory = card.mimeType.split('/')[0]
      const mimeTypeDetail = card.mimeType.split('/')[1]

      // Compact mode for dense grids (9+ columns)
      if (isCompact) {
        return (
          <ShadcnCard 
            className="group relative border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer overflow-hidden aspect-video"
            onClick={() => onClick?.(card)}
          >
            {/* Image/Icon Preview - full card */}
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

            {/* Menu button - visible on hover */}
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
                  {/* Info section */}
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
                  
                  {/* Actions */}
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

      // Normal mode
      return (
        <ShadcnCard 
          className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer overflow-hidden flex flex-col p-0"
          onClick={() => onClick?.(card)}
        >
          {/* Image/Icon Preview */}
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

          {/* Content */}
          <div className="p-2 sm:p-3 flex flex-col gap-2 min-h-[70px] sm:min-h-[80px]">
            {/* Header with title and actions */}
            <div className="flex items-start justify-between gap-2">
              <h3 className="text-xs sm:text-sm font-medium text-gray-900 dark:text-white truncate leading-tight flex-1">
                {card.name}
              </h3>
              <DropdownMenu>
                <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                  <Button variant="ghost" size="sm" className="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0">
                    <MoreVertical className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
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

            {/* MimeType badges */}
            <div className="flex flex-wrap gap-1">
              <Badge className={`text-[9px] sm:text-xs ${getMimeTypeBadgeColor(card.mimeType)}`}>
                {mimeTypeCategory}
              </Badge>
              <Badge variant="outline" className="text-[9px] sm:text-xs">
                {mimeTypeDetail}
              </Badge>
            </div>

            {/* Footer info */}
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
    } else {
      // Collection card
      return (
        <ShadcnCard 
          className="group border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer min-h-[180px] flex flex-col"
          onClick={() => onClick?.(card)}
        >
          <div className="p-3 sm:p-4 flex-1 flex flex-col">
            {/* Header */}
            <div className="flex items-start justify-between gap-2 mb-2">
              <div className="flex items-center gap-2 flex-1 min-w-0">
                <Package className="w-5 h-5 text-purple-500 shrink-0" />
                <h3 className="text-sm sm:text-base font-semibold text-gray-900 dark:text-white truncate leading-tight">
                  {card.name}
                </h3>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                  <Button variant="ghost" size="sm" className="h-7 w-7 sm:h-8 sm:w-8 p-0 shrink-0">
                    <MoreVertical className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
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

            {/* Description */}
            {card.description && (
              <p className="text-xs text-gray-600 dark:text-gray-400 mb-2 line-clamp-2">
                {card.description}
              </p>
            )}

            {/* Tags */}
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

            {/* Footer - pushed to bottom */}
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
  }

  // List View
  if (card.type === 'project') {
    return (
      <ShadcnCard 
        className="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer"
        onClick={() => onClick?.(card)}
      >
        <div className="px-3 flex items-center justify-between gap-2">
          {/* Left section */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-0.5">
              <h3 className="text-base font-semibold text-gray-900 dark:text-white truncate">
                {card.name}
              </h3>
              {card.storageType && (
                <div className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
                  {getStorageIcon(card.storageType)}
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

          {/* Tags */}
          <div className="flex flex-wrap gap-1 max-w-md">
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

          {/* Actions */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                <MoreVertical className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
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
  } else if (card.type === 'asset') {
    // Asset list view
    const Icon = getMimeTypeIcon(card.mimeType)
    const isImage = card.mimeType.startsWith('image/')
    const mimeTypeCategory = card.mimeType.split('/')[0]
    const mimeTypeDetail = card.mimeType.split('/')[1]

    return (
      <ShadcnCard 
        className="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer"
        onClick={() => onClick?.(card)}
      >
        <div className="px-3 flex items-center gap-3">
          {/* Thumbnail */}
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

          {/* Info */}
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

          {/* Actions */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0 shrink-0">
                <MoreVertical className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
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
  } else {
    // Collection list view
    return (
      <ShadcnCard 
        className="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm hover:shadow-lg hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200 cursor-pointer"
        onClick={() => onClick?.(card)}
      >
        <div className="px-3 flex items-center gap-3">
          {/* Icon */}
          <div className="w-24 h-14 bg-purple-100 dark:bg-purple-900/20 rounded flex items-center justify-center shrink-0">
            <Package className="w-6 h-6 text-purple-600 dark:text-purple-400" />
          </div>

          {/* Info */}
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

          {/* Date */}
          <div className="hidden sm:flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 min-w-[100px]">
            <Calendar className="w-3 h-3" />
            <span>
              {formatDistanceToNow(new Date(card.updatedAt), { addSuffix: true, locale: ptBR })}
            </span>
          </div>

          {/* Actions */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                <MoreVertical className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
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
}
