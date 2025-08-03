import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export const cn = (...inputs: ClassValue[]) => twMerge(clsx(inputs));

export const numberToAbbreviation = (number: number): string => {
  if (!number || number === 0) return '0';

  const absoluteNumber = Math.abs(number);
  const sign = number < 0 ? '-' : '';

  if (absoluteNumber >= 1_000_000_000) {
    const value = absoluteNumber / 1_000_000_000;
    return `${sign}${value % 1 === 0 ? value.toFixed(0) : value.toFixed(1)}B`;
  }

  if (absoluteNumber >= 1_000_000) {
    const value = absoluteNumber / 1_000_000;
    return `${sign}${value % 1 === 0 ? value.toFixed(0) : value.toFixed(1)}M`;
  }

  if (absoluteNumber >= 1_000) {
    const value = absoluteNumber / 1_000;
    return `${sign}${value % 1 === 0 ? value.toFixed(0) : value.toFixed(1)}K`;
  }

  return `${sign}${absoluteNumber}`;
};

export const formatNumber = (number: number): string => {
  return new Intl.NumberFormat('en-US').format(number);
};

export const formatCurrency = (amount: number, currency: string = 'USD'): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(amount);
};
