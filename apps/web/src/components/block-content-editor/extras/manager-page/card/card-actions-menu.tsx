"use client"

import React from 'react'
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { MoreVertical } from 'lucide-react'
import { type ManagerCard, type CardAction } from '../types'

interface CardActionsMenuProps {
  card: ManagerCard
  primaryActions: CardAction[]
  secondaryActions: CardAction[]
  buttonClassName?: string
  iconClassName?: string
  variant?: 'ghost' | 'secondary'
}

export function CardActionsMenu({ 
  card, 
  primaryActions, 
  secondaryActions, 
  buttonClassName = "h-7 w-7 p-0",
  iconClassName = "h-4 w-4",
  variant = 'ghost'
}: CardActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
        <Button variant={variant} size="sm" className={buttonClassName}>
          <MoreVertical className={iconClassName} />
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
  )
}
