import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Converts a number to an abbreviated format (e.g., 1000 -> 1K, 1000000 -> 1M)
 */
export function numberToAbbreviation(num: number): string {
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
