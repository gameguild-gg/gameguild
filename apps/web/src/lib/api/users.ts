'use server';

// STUB: Users module actions are stubbed; endpoints are unavailable in current SDK.

export type SortDirection = 'asc' | 'desc' | any;
export type UserSortField = any;

export interface User {
  id: string;
  version?: number;
  name: string;
  username: string;
  email: string;
  isActive: boolean;
  balance: number;
  availableBalance: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  isDeleted: boolean;
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

export interface UserSearchOptions {
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
  [key: string]: unknown;
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
  newUsersThisMonth: number;
  newUsersThisWeek: number;
}

export interface BulkOperationResult {
  totalProcessed: number;
  successful: number;
  failed: number;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
}

export async function getUsers(_includeDeleted = false, _skip = 0, _take = 50, _isActive?: boolean): Promise<User[]> {
  throw new Error('Not implemented (STUB): getUsers');
}

export async function getUser(_id: string, _includeDeleted = false): Promise<User | null> {
  throw new Error('Not implemented (STUB): getUser');
}

export async function createUser(_userData: CreateUserRequest): Promise<User> {
  throw new Error('Not implemented (STUB): createUser');
}

export async function updateUser(_id: string, _userData: UpdateUserRequest): Promise<User> {
  throw new Error('Not implemented (STUB): updateUser');
}

export async function deleteUser(_id: string, _softDelete = true, _reason?: string): Promise<void> {
  throw new Error('Not implemented (STUB): deleteUser');
}

export async function restoreUser(_id: string, _reason?: string): Promise<void> {
  throw new Error('Not implemented (STUB): restoreUser');
}

export async function updateUserBalance(_id: string, _balanceData: UpdateUserBalanceRequest): Promise<User> {
  throw new Error('Not implemented (STUB): updateUserBalance');
}

export async function searchUsers(_options: UserSearchOptions = {}): Promise<PagedResult<User>> {
  throw new Error('Not implemented (STUB): searchUsers');
}

export async function getUserStatistics(_fromDate?: string, _toDate?: string, _includeDeleted = false): Promise<UserStatistics> {
  throw new Error('Not implemented (STUB): getUserStatistics');
}

export async function bulkActivateUsers(_userIds: string[], _reason?: string): Promise<BulkOperationResult> {
  throw new Error('Not implemented (STUB): bulkActivateUsers');
}

export async function bulkDeactivateUsers(_userIds: string[], _reason?: string): Promise<BulkOperationResult> {
  throw new Error('Not implemented (STUB): bulkDeactivateUsers');
}

export async function getUserByUsername(_username: string, _includeDeleted = false): Promise<User | null> {
  throw new Error('Not implemented (STUB): getUserByUsername');
}
