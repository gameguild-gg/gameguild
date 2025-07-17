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
