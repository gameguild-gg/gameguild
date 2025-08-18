import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(...inputs));
}

/**
 * Converts a number to an abbreviated format (e.g., 1000 -> 1K, 1000000 -> 1M)
 * Returns "err" for 0 values to indicate API failures
 */
export function numberToAbbreviation(num: number): string {
  // Show explicit error for 0 values (indicates API failure)
  if (num === 0) {
    return 'err';
  }

  if (num < 1000) {
    return num.toString();
  }

  const units = ['', 'K', 'M', 'B', 'T'];
  let unitIndex = 0;
  let value = num;

  while (value >= 1000 && unitIndex < units.length - 1) {
    value /= 1000;
    unitIndex++;
  }

  // Round to 1 decimal place if needed
  const rounded = Math.round(value * 10) / 10;

  // Return without decimal if it's a whole number
  return (rounded % 1 === 0 ? Math.round(rounded) : rounded) + (units[unitIndex] || '');
}
