'use client';

import React, { useMemo } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn } from '@/lib/utils';

// Generic data item interface
interface DataItem extends Record<string, unknown> {
  id: string | number;
}

// Generic column configuration for table/row views
export interface ColumnConfig<T extends DataItem> {
  key: keyof T;
  label: string;
  width?: string;
  className?: string;
  sortable?: boolean;
  render?: (value: T[keyof T], item: T) => React.ReactNode;
}

// Generic action configuration
export interface ActionConfig<T extends DataItem> {
  key: string;
  label: string;
  icon?: React.ReactNode;
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'ghost' | 'link';
  className?: string;
  onClick: (item: T) => void;
  disabled?: (item: T) => boolean;
  hidden?: (item: T) => boolean;
}

// Card view configuration
export interface CardConfig<T extends DataItem> {
  titleKey: keyof T;
  descriptionKey?: keyof T;
  imageKey?: keyof T;
  badgeKey?: keyof T;
  metaFields?: Array<{
    key: keyof T;
    label: string;
    render?: (value: T[keyof T], item: T) => React.ReactNode;
  }>;
  actions?: ActionConfig<T>[];
  className?: string;
}

// Generic Card View Component
interface GenericCardViewProps<T extends DataItem> {
  items: T[];
  config: CardConfig<T>;
  className?: string;
  emptyMessage?: string;
  loading?: boolean;
  onItemClick?: (item: T) => void;
}

export function GenericCardView<T extends DataItem>({
  items,
  config,
  className,
  emptyMessage = 'No items to display',
  loading = false,
  onItemClick,
}: GenericCardViewProps<T>) {
  if (loading) {
    return (
      <div className={cn('grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4', className)}>
        {Array.from({ length: 6 }).map((_, index) => (
          <Card key={index} className="animate-pulse">
            <CardHeader>
              <div className="h-4 bg-muted rounded w-3/4"></div>
              <div className="h-3 bg-muted rounded w-1/2"></div>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div className="h-3 bg-muted rounded"></div>
                <div className="h-3 bg-muted rounded w-2/3"></div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={cn('grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4', className)}>
      {items.map((item) => (
        <Card
          key={item.id}
          className={cn('transition-all duration-200 hover:shadow-md', onItemClick && 'cursor-pointer hover:border-primary/50', config.className)}
          onClick={() => onItemClick?.(item)}
        >
          <CardHeader>
            <CardTitle className="flex items-start justify-between">
              <span>{String(item[config.titleKey])}</span>
              {config.badgeKey && <Badge variant="secondary">{String(item[config.badgeKey])}</Badge>}
            </CardTitle>
            {config.descriptionKey && <CardDescription>{String(item[config.descriptionKey])}</CardDescription>}
          </CardHeader>
          <CardContent>
            {config.metaFields && (
              <div className="space-y-2 mb-4">
                {config.metaFields.map((field) => (
                  <div key={String(field.key)} className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{field.label}:</span>
                    <span>{field.render ? field.render(item[field.key], item) : String(item[field.key])}</span>
                  </div>
                ))}
              </div>
            )}
            {config.actions && config.actions.length > 0 && (
              <div className="flex gap-2 flex-wrap">
                {config.actions
                  .filter((action) => !action.hidden?.(item))
                  .map((action) => (
                    <Button
                      key={action.key}
                      variant={action.variant || 'outline'}
                      size="sm"
                      className={action.className}
                      disabled={action.disabled?.(item)}
                      onClick={(e) => {
                        e.stopPropagation();
                        action.onClick(item);
                      }}
                    >
                      {action.icon && <span className="mr-1">{action.icon}</span>}
                      {action.label}
                    </Button>
                  ))}
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// Generic Table View Component
interface GenericTableViewProps<T extends DataItem> {
  items: T[];
  columns: ColumnConfig<T>[];
  actions?: ActionConfig<T>[];
  className?: string;
  emptyMessage?: string;
  loading?: boolean;
  onItemClick?: (item: T) => void;
  sortKey?: keyof T;
  sortDirection?: 'asc' | 'desc';
  onSort?: (key: keyof T) => void;
}

export function GenericTableView<T extends DataItem>({
  items,
  columns,
  actions,
  className,
  emptyMessage = 'No items to display',
  loading = false,
  onItemClick,
  sortKey,
  sortDirection,
  onSort,
}: GenericTableViewProps<T>) {
  const sortedItems = useMemo(() => {
    if (!sortKey || !sortDirection) return items;

    return [...items].sort((a, b) => {
      const aValue = a[sortKey];
      const bValue = b[sortKey];

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [items, sortKey, sortDirection]);

  if (loading) {
    return (
      <div className={cn('border rounded-lg', className)}>
        <Table>
          <TableHeader>
            <TableRow>
              {columns.map((column) => (
                <TableHead key={String(column.key)} style={{ width: column.width }}>
                  {column.label}
                </TableHead>
              ))}
              {actions && actions.length > 0 && <TableHead className="w-[120px]">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {Array.from({ length: 5 }).map((_, index) => (
              <TableRow key={index}>
                {columns.map((column) => (
                  <TableCell key={String(column.key)}>
                    <div className="h-4 bg-muted rounded animate-pulse"></div>
                  </TableCell>
                ))}
                {actions && actions.length > 0 && (
                  <TableCell>
                    <div className="h-4 bg-muted rounded animate-pulse w-16"></div>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="text-center py-12 border rounded-lg">
        <p className="text-muted-foreground">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={cn('border rounded-lg', className)}>
      <Table>
        <TableHeader>
          <TableRow>
            {columns.map((column) => (
              <TableHead
                key={String(column.key)}
                className={cn(column.className, column.sortable && onSort && 'cursor-pointer hover:bg-muted/50')}
                style={{ width: column.width }}
                onClick={() => column.sortable && onSort?.(column.key)}
              >
                <div className="flex items-center gap-2">
                  {column.label}
                  {column.sortable && sortKey === column.key && <span className="text-xs">{sortDirection === 'asc' ? '↑' : '↓'}</span>}
                </div>
              </TableHead>
            ))}
            {actions && actions.length > 0 && <TableHead className="w-[120px]">Actions</TableHead>}
          </TableRow>
        </TableHeader>
        <TableBody>
          {sortedItems.map((item) => (
            <TableRow key={item.id} className={cn(onItemClick && 'cursor-pointer hover:bg-muted/50')} onClick={() => onItemClick?.(item)}>
              {columns.map((column) => (
                <TableCell key={String(column.key)} className={column.className}>
                  {column.render ? column.render(item[column.key], item) : String(item[column.key])}
                </TableCell>
              ))}
              {actions && actions.length > 0 && (
                <TableCell>
                  <div className="flex gap-1">
                    {actions
                      .filter((action) => !action.hidden?.(item))
                      .map((action) => (
                        <Button
                          key={action.key}
                          variant={action.variant || 'ghost'}
                          size="sm"
                          className={action.className}
                          disabled={action.disabled?.(item)}
                          onClick={(e) => {
                            e.stopPropagation();
                            action.onClick(item);
                          }}
                        >
                          {action.icon && <span className="mr-1">{action.icon}</span>}
                          {action.label}
                        </Button>
                      ))}
                  </div>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

// Generic Row View Component (list style)
interface GenericRowViewProps<T extends DataItem> {
  items: T[];
  config: CardConfig<T>;
  className?: string;
  emptyMessage?: string;
  loading?: boolean;
  onItemClick?: (item: T) => void;
}

export function GenericRowView<T extends DataItem>({
  items,
  config,
  className,
  emptyMessage = 'No items to display',
  loading = false,
  onItemClick,
}: GenericRowViewProps<T>) {
  if (loading) {
    return (
      <div className={cn('space-y-2', className)}>
        {Array.from({ length: 5 }).map((_, index) => (
          <div key={index} className="border rounded-lg p-4 animate-pulse">
            <div className="flex justify-between items-start">
              <div className="space-y-2 flex-1">
                <div className="h-4 bg-muted rounded w-1/3"></div>
                <div className="h-3 bg-muted rounded w-1/2"></div>
              </div>
              <div className="h-6 bg-muted rounded w-16"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={cn('space-y-2', className)}>
      {items.map((item) => (
        <div
          key={item.id}
          className={cn(
            'border rounded-lg p-4 transition-all duration-200 hover:shadow-md',
            onItemClick && 'cursor-pointer hover:border-primary/50',
            config.className,
          )}
          onClick={() => onItemClick?.(item)}
        >
          <div className="flex justify-between items-start">
            <div className="flex-1 space-y-1">
              <div className="flex items-center gap-2">
                <h3 className="font-medium">{String(item[config.titleKey])}</h3>
                {config.badgeKey && <Badge variant="secondary">{String(item[config.badgeKey])}</Badge>}
              </div>
              {config.descriptionKey && <p className="text-sm text-muted-foreground">{String(item[config.descriptionKey])}</p>}
              {config.metaFields && (
                <div className="flex gap-4 text-xs text-muted-foreground">
                  {config.metaFields.map((field) => (
                    <span key={String(field.key)}>
                      {field.label}: {field.render ? field.render(item[field.key], item) : String(item[field.key])}
                    </span>
                  ))}
                </div>
              )}
            </div>
            {config.actions && config.actions.length > 0 && (
              <div className="flex gap-2 ml-4">
                {config.actions
                  .filter((action) => !action.hidden?.(item))
                  .map((action) => (
                    <Button
                      key={action.key}
                      variant={action.variant || 'outline'}
                      size="sm"
                      className={action.className}
                      disabled={action.disabled?.(item)}
                      onClick={(e) => {
                        e.stopPropagation();
                        action.onClick(item);
                      }}
                    >
                      {action.icon && <span className="mr-1">{action.icon}</span>}
                      {action.label}
                    </Button>
                  ))}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
