export const numberToAbbreviation = (number: number): string => {
  if (!number) return '0';
  const absoluteNumber = Math.abs(number);
  if (absoluteNumber >= 1_000_000_000) return `${(number / 1_000_000_000).toFixed(1)}B`;
  if (absoluteNumber >= 1_000_000) return `${(number / 1_000_000).toFixed(1)}M`;
  if (absoluteNumber >= 1_000) return `${(number / 1_000).toFixed(1)}K`;
  return number.toFixed(1);
};
