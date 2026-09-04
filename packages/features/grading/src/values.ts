export const SCORE_VALUE_PATTERN = /^\d{8}\.\d{4}$/;
export const PERCENT_VALUE_PATTERN = /^\d{3}\.\d{4}$/;
export const ZERO_SCORE_VALUE = "00000000.0000" as ScoreValue;
export const ZERO_PERCENT_VALUE = "000.0000" as PercentValue;
export const HUNDRED_PERCENT_VALUE = "100.0000" as PercentValue;

declare const scoreValueBrand: unique symbol;
declare const percentValueBrand: unique symbol;

export type ScoreValue = string & { readonly [scoreValueBrand]: "ScoreValue" };
export type PercentValue = string & { readonly [percentValueBrand]: "PercentValue" };

const SCALE = 10_000n;
const MAX_SCORE_SCALED = 999999999999n;
const MAX_PERCENT_SCALED = 1_000_000n;

export function parseScoreValue(value: unknown): ScoreValue {
  if (typeof value !== "string" || !SCORE_VALUE_PATTERN.test(value)) {
    throw new TypeError("ScoreValue must match ^\\d{8}\\.\\d{4}$.");
  }
  return value as ScoreValue;
}

export function parsePercentValue(value: unknown): PercentValue {
  if (typeof value !== "string" || !PERCENT_VALUE_PATTERN.test(value)) {
    throw new TypeError("PercentValue must match ^\\d{3}\\.\\d{4}$.");
  }
  const scaled = decimalToScaledInteger(value);
  if (scaled > MAX_PERCENT_SCALED) {
    throw new RangeError("PercentValue must be between 000.0000 and 100.0000.");
  }
  return value as PercentValue;
}

export function canonicalizeScoreValue(value: string): ScoreValue {
  return formatScaledInteger(parseDecimalInput(value, 8, MAX_SCORE_SCALED), 8) as ScoreValue;
}

export function canonicalizePercentValue(value: string): PercentValue {
  return formatScaledInteger(parseDecimalInput(value, 3, MAX_PERCENT_SCALED), 3) as PercentValue;
}

export function scoreValueFromScaledInteger(value: bigint): ScoreValue {
  assertScaledRange(value, MAX_SCORE_SCALED, "ScoreValue");
  return formatScaledInteger(value, 8) as ScoreValue;
}

export function percentValueFromScaledInteger(value: bigint): PercentValue {
  assertScaledRange(value, MAX_PERCENT_SCALED, "PercentValue");
  return formatScaledInteger(value, 3) as PercentValue;
}

export function scoreValueToScaledInteger(value: ScoreValue): bigint {
  return decimalToScaledInteger(parseScoreValue(value));
}

export function percentValueToScaledInteger(value: PercentValue): bigint {
  return decimalToScaledInteger(parsePercentValue(value));
}

export function addScoreValues(values: readonly ScoreValue[]): ScoreValue {
  return scoreValueFromScaledInteger(
    values.reduce((sum, value) => sum + scoreValueToScaledInteger(value), 0n),
  );
}

export function scoreValueByRatio(
  maximum: ScoreValue,
  earnedUnits: bigint,
  totalUnits: bigint,
): ScoreValue {
  if (earnedUnits < 0n || totalUnits <= 0n || earnedUnits > totalUnits) {
    throw new RangeError("Score ratio requires 0 <= earnedUnits <= totalUnits.");
  }
  const numerator = scoreValueToScaledInteger(maximum) * earnedUnits;
  return scoreValueFromScaledInteger(divideRoundHalfUp(numerator, totalUnits));
}

export function compareScoreValues(left: ScoreValue, right: ScoreValue): number {
  return left === right ? 0 : left < right ? -1 : 1;
}

export function comparePercentValues(left: PercentValue, right: PercentValue): number {
  return left === right ? 0 : left < right ? -1 : 1;
}

function parseDecimalInput(value: string, integerWidth: number, maximum: bigint): bigint {
  const match = /^(0|[1-9]\d*)(?:\.(\d{0,4}))?$/.exec(value.trim());
  if (!match || match[1]!.length > integerWidth) {
    throw new TypeError(`Expected a non-negative decimal with at most ${integerWidth} integer and 4 fractional digits.`);
  }
  const scaled = BigInt(match[1]!) * SCALE + BigInt((match[2] ?? "").padEnd(4, "0"));
  assertScaledRange(scaled, maximum, "Decimal value");
  return scaled;
}

function decimalToScaledInteger(value: string): bigint {
  const [integer, fraction] = value.split(".") as [string, string];
  return BigInt(integer) * SCALE + BigInt(fraction);
}

function formatScaledInteger(value: bigint, integerWidth: number): string {
  const integer = value / SCALE;
  const fraction = value % SCALE;
  return `${integer.toString().padStart(integerWidth, "0")}.${fraction.toString().padStart(4, "0")}`;
}

function assertScaledRange(value: bigint, maximum: bigint, label: string): void {
  if (value < 0n || value > maximum) {
    throw new RangeError(`${label} is outside the supported range.`);
  }
}

function divideRoundHalfUp(numerator: bigint, denominator: bigint): bigint {
  return (numerator + denominator / 2n) / denominator;
}
