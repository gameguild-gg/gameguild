'use server';

// STUB: Health actions are stubbed; endpoints are unavailable in current SDK.

export type GetHealthData = any;
export type GetHealthDatabaseData = any;

export async function getSystemHealthAction(_params?: GetHealthData): Promise<any> {
  throw new Error('Not implemented (STUB): getSystemHealthAction');
}

export async function getDatabaseHealthAction(_params?: GetHealthDatabaseData): Promise<any> {
  throw new Error('Not implemented (STUB): getDatabaseHealthAction');
}
