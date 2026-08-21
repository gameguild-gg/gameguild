import { describe, expect, it } from "vitest";
import { evaluateFormula, validateFormula } from "./formula/formula-expression";

describe("formula evaluator", () => {
  it("preserves precedence and right-associative powers", () => {
    expect(evaluateFormula("2 + 3 * 4", {})).toBe(14);
    expect(evaluateFormula("2^3^2", {})).toBe(512);
  });

  it("supports variables and known functions", () => {
    expect(evaluateFormula("sqrt(x) + max(2, y)", { x: 9, y: 5 })).toBe(8);
  });

  it("rejects unknown identifiers, division by zero, and malformed numbers", () => {
    expect(() => evaluateFormula("secret + 1", {})).toThrow(/Unknown identifier/);
    expect(() => evaluateFormula("1 / 0", {})).toThrow(/Division by zero/);
    expect(validateFormula("1..2 + x", ["x"])).not.toBeNull();
  });
});
