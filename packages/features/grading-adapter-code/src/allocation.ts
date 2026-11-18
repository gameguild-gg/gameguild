import { parseScoreValue, type ScoreValue } from '@game-guild/grading';
import type { CodingTestV1 } from '@game-guild/coding';
import type { CodeTestScoreAllocationV1 } from './contracts';

/**
 * Hamilton allocation over integer units. Floors are assigned first, then
 * remaining units follow descending fractional remainder and stable test ID.
 */
export function allocateCodeTestScores(tests: readonly Pick<CodingTestV1, 'id' | 'weightUnits'>[], maxScore: ScoreValue): CodeTestScoreAllocationV1[] {
  if (tests.length === 0) throw new TypeError('Code score allocation requires at least one test.');
  const totalScore = BigInt(parseScoreValue(maxScore));
  const seen = new Set<string>();
  let totalWeight = 0n;
  const weighted = tests.map(({ id, weightUnits }) => {
    if (!id.trim() || seen.has(id)) throw new TypeError(`Code test ID is empty or duplicated: ${id}.`);
    if (!Number.isSafeInteger(weightUnits) || weightUnits <= 0) {
      throw new TypeError(`Code test ${id} weightUnits must be a positive safe integer.`);
    }
    seen.add(id);
    const weight = BigInt(weightUnits);
    totalWeight += weight;
    return { id, weight };
  });

  const shares = weighted.map(({ id, weight }) => {
    const numerator = totalScore * weight;
    return {
      id,
      units: numerator / totalWeight,
      remainder: numerator % totalWeight,
    };
  });
  const allocated = shares.reduce((sum, { units }) => sum + units, 0n);
  const remaining = Number(totalScore - allocated);
  const priority = [...shares].sort((left, right) => {
    if (left.remainder !== right.remainder) return left.remainder > right.remainder ? -1 : 1;
    return left.id.localeCompare(right.id);
  });
  for (let index = 0; index < remaining; index += 1) {
    priority[index]!.units += 1n;
  }
  const byId = new Map(shares.map(({ id, units }) => [id, Number(units)]));
  return tests.map(({ id }) => ({ testId: id, maxScore: parseScoreValue(byId.get(id)) }));
}
