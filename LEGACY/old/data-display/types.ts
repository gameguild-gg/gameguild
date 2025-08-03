export interface Column<T = Record<string, unknown>> {
  key: keyof T | string;
  label: string;
  width?: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
  className?: string;
}

export interface SortConfig {
  key: string;
  direction: 'asc' | 'desc';
}

export interface DataDisplayProps<T = Record<string, unknown>> {
  data: T[];
  columns: Column<T>[];
  viewMode: 'cards' | 'row' | 'table';
  loading?: boolean;
  emptyMessage?: string;
  sortConfig?: SortConfig;
  onSort?: (key: string) => void;
  className?: string;
}

export interface CardDisplayProps<T = Record<string, unknown>> {
  data: T[];
  renderCard: (item: T) => React.ReactNode;
  loading?: boolean;
  emptyMessage?: string;
  className?: string;
}

export interface RowDisplayProps<T = Record<string, unknown>> {
  data: T[];
  renderRow: (item: T) => React.ReactNode;
  loading?: boolean;
  emptyMessage?: string;
  className?: string;
}

export interface TableDisplayProps<T = Record<string, unknown>> {
  data: T[];
  columns: Column<T>[];
  loading?: boolean;
  emptyMessage?: string;
  sortConfig?: SortConfig;
  onSort?: (key: string) => void;
  className?: string;
}
