export interface User {
  id: string;
  version: number;
  name: string;
  email: string;
  isActive: boolean;
  balance: number;
  availableBalance: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  isDeleted: boolean;
  activeSubscription?: UserSubscriptionSummary;
  role?: string;
}

export interface UserSubscriptionSummary {
  id: string;
  productId: string;
  productName: string;
  status: string;
  startDate: string;
  endDate?: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  isActive?: boolean;
  initialBalance?: number;
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  isActive?: boolean;
  expectedVersion?: number;
}

export interface UpdateUserBalanceRequest {
  balance: number;
  availableBalance: number;
  reason?: string;
  expectedVersion?: number;
}

export interface BulkOperationResult {
  successCount: number;
  failureCount: number;
  errors: string[];
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
}

export interface SearchUsersParams {
  searchTerm?: string;
  isActive?: boolean;
  minBalance?: number;
  maxBalance?: number;
  createdAfter?: string;
  createdBefore?: string;
  includeDeleted?: boolean;
  skip?: number;
  take?: number;
  sortBy?: UserSortField;
  sortDirection?: SortDirection;
}

export enum UserSortField {
  Name = 'Name',
  Email = 'Email',
  CreatedAt = 'CreatedAt',
  UpdatedAt = 'UpdatedAt',
  Balance = 'Balance',
}

export enum SortDirection {
  Ascending = 'Ascending',
  Descending = 'Descending',
}
