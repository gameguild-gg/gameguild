"use client";
import React from 'react';
import { useQuery } from '@apollo/client';
import { HEALTH_QUERY } from '@/lib/graphql/queries/health';

export function GraphQLHealthCheck() {
  const { data, loading, error } = useQuery(HEALTH_QUERY, { fetchPolicy: 'network-only' });
  if (loading) return <span>Loading GraphQL…</span>;
  if (error) return <span style={{ color: 'red' }}>GraphQL error: {error.message}</span>;
  return <span>GraphQL: {data?.health}</span>;
}
