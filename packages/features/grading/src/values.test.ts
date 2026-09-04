import { describe, expect, it } from "vitest";
import {
  addScoreValues,
  canonicalizePercentValue,
  canonicalizeScoreValue,
  compareScoreValues,
  parsePercentValue,
  parseScoreValue,
  scoreValueByRatio,
} from "./index";

describe("academic value objects", () => {
  it("canonicalizes and validates fixed-width values", () => {
    expect(canonicalizeScoreValue("12.5")).toBe("00000012.5000");
    expect(canonicalizePercentValue("5")).toBe("005.0000");
    expect(() => parseScoreValue("12.5")).toThrow(TypeError);
    expect(() => parsePercentValue("100.0001")).toThrow(RangeError);
    expect(() => canonicalizeScoreValue("1.00001")).toThrow(TypeError);
  });

  it("uses exact arithmetic and midpoint-away-from-zero quantization", () => {
    const one = parseScoreValue("00000001.0000");
    expect(addScoreValues([one, one])).toBe("00000002.0000");
    expect(scoreValueByRatio(one, 1n, 3n)).toBe("00000000.3333");
    expect(scoreValueByRatio(one, 1n, 32n)).toBe("00000000.0313");
    expect(compareScoreValues(one, parseScoreValue("00000002.0000"))).toBe(-1);
  });
});
