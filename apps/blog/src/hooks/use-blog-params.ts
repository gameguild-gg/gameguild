'use client';

import { useParams } from 'next/navigation';
import { decodeParams } from '@/app/utils';

export interface BlogParams {
  currentYear?: number;
  currentMonth?: number;
  currentDay?: number;
  currentSlug?: string;
}

export function useBlogParams(): BlogParams {
  const params = useParams();
  const { year, month, day, slug } = decodeParams(params);

  return {
    currentYear: year ? parseInt(year) : undefined,
    currentMonth: month ? parseInt(month) : undefined,
    currentDay: day ? parseInt(day) : undefined,
    currentSlug: slug,
  };
}
