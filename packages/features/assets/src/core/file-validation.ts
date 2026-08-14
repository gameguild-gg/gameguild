import type { AssetKind } from "./asset-contracts";
import { classifyAssetKind } from "./mime";

export interface AssetAcceptanceRules {
  accept?: string;
  kinds?: readonly AssetKind[];
  minSizeBytes?: number;
  maxSizeBytes?: number;
}

export interface AssetValidationIssue {
  code: "empty" | "too-small" | "too-large" | "type" | "kind";
  message: string;
}

function matchesAccept(file: Pick<File, "name" | "type">, accept: string): boolean {
  const name = file.name.toLocaleLowerCase();
  const type = file.type.toLocaleLowerCase();
  return accept.split(",").some((rawRule) => {
    const rule = rawRule.trim().toLocaleLowerCase();
    if (!rule) return false;
    if (rule.startsWith(".")) return name.endsWith(rule);
    if (rule.endsWith("/*")) return type.startsWith(rule.slice(0, -1));
    return type === rule;
  });
}

export function validateAssetFile(
  file: Pick<File, "name" | "type" | "size">,
  rules: AssetAcceptanceRules = {},
): AssetValidationIssue[] {
  const issues: AssetValidationIssue[] = [];
  if (file.size === 0) issues.push({ code: "empty", message: `${file.name} is empty.` });
  if (rules.minSizeBytes && file.size < rules.minSizeBytes) {
    issues.push({ code: "too-small", message: `${file.name} is smaller than allowed.` });
  }
  if (rules.maxSizeBytes && file.size > rules.maxSizeBytes) {
    issues.push({ code: "too-large", message: `${file.name} exceeds the size limit.` });
  }
  if (rules.accept && !matchesAccept(file, rules.accept)) {
    issues.push({ code: "type", message: `${file.name} is not an accepted file type.` });
  }
  if (rules.kinds?.length && !rules.kinds.includes(classifyAssetKind(file.type, file.name))) {
    issues.push({ code: "kind", message: `${file.name} is not an accepted asset kind.` });
  }
  return issues;
}
