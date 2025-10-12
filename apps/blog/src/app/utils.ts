export type ParamValue = string | Array<string> | undefined;
export type Params = Record<string, ParamValue>;

export type decodeParams = (params: Params) => {
  year?: string;
  month?: string;
  day?: string;
  slug?: string;
};

export const decodeParams: decodeParams = (params: Params) => {
  const result: { year?: string; month?: string; day?: string; slug?: string } = {};

  // Helper function to extract string value from ParamValue
  const getStringValue = (value: ParamValue): string | undefined => {
    if (typeof value === 'string') {
      return decodeURIComponent(value);
    }
    if (Array.isArray(value) && value.length > 0) {
      return decodeURIComponent(value[0]);
    }
    return undefined;
  };

  // Extract known route parameters
  result.year = getStringValue(params.year);
  result.month = getStringValue(params.month);
  result.day = getStringValue(params.day);
  result.slug = getStringValue(params.slug);

  return result;
};

export type isValidDate = (year: string, month: string, day: string) => boolean;

export const isValidDate: isValidDate = (year: string, month: string, day: string): boolean => {
  const dateString = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
  const date = new Date(dateString);

  return !isNaN(date.getTime()) && date.toISOString().startsWith(dateString);
};
