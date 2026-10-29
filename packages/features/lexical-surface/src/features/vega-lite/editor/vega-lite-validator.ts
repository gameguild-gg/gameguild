import type { TopLevelSpec } from "vega-lite";

type VegaLitePattern = Pick<
  Extract<TopLevelSpec, { mark: unknown }>,
  "mark" | "encoding"
>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function getErrorMessage(error: unknown, fallback: string): string {
  try {
    const message = isRecord(error) ? error.message : undefined;
    return typeof message === "string" && message ? message : fallback;
  } catch {
    return fallback;
  }
}

export interface VegaLiteValidationResult {
  isValid: boolean;
  error?: string;
  warnings?: string[];
}

export class VegaLiteValidator {
  /**
   * Validates a Vega-Lite specification
   */
  static async validateSpec(spec: string): Promise<VegaLiteValidationResult> {
    if (!spec || spec.trim() === "") {
      return {
        isValid: false,
        error: "Specification cannot be empty",
      };
    }

    try {
      // Parse JSON
      let parsedSpec: unknown;
      try {
        parsedSpec = JSON.parse(spec);
      } catch (parseError) {
        return {
          isValid: false,
          error: `Invalid JSON: ${parseError instanceof Error ? parseError.message : "Unknown parsing error"}`,
        };
      }

      // Basic structure validation
      if (!isRecord(parsedSpec)) {
        return {
          isValid: false,
          error: "Specification must be a JSON object",
        };
      }

      // Check for required fields
      const warnings: string[] = [];

      // Check for $schema (recommended but not required)
      if (!parsedSpec.$schema) {
        warnings.push("Missing $schema field (recommended)");
      }

      // Check for data source
      if (!parsedSpec.data && !parsedSpec.datasets) {
        return {
          isValid: false,
          error: "Specification must include a 'data' field or 'datasets'",
        };
      }

      // Check for mark type
      if (
        !parsedSpec.mark &&
        !parsedSpec.layer &&
        !parsedSpec.concat &&
        !parsedSpec.hconcat &&
        !parsedSpec.vconcat &&
        !parsedSpec.facet &&
        !parsedSpec.repeat
      ) {
        return {
          isValid: false,
          error:
            "Specification must include a 'mark' field or be a composite specification (layer, concat, etc.)",
        };
      }

      // Check for encoding (if using mark)
      if (parsedSpec.mark && !parsedSpec.encoding) {
        warnings.push("Mark-based charts typically need an 'encoding' field");
      }

      // Advanced validation using Vega-Lite library if available
      try {
        const vegaLiteImport = await import("vega-lite").catch(() => null);

        if (!vegaLiteImport) {
          // If we can't import vega-lite, do basic validation only
          console.warn(
            "Vega-Lite library not available for advanced validation",
          );

          // Basic validation passed, return as valid
          return {
            isValid: true,
            warnings: warnings.length > 0 ? warnings : undefined,
          };
        }

        try {
          // Try to compile the specification
          const compiled = vegaLiteImport.compile(parsedSpec as TopLevelSpec);
          if (!compiled || !compiled.spec) {
            return {
              isValid: false,
              error: "Failed to compile Vega-Lite specification",
            };
          }

          return {
            isValid: true,
            warnings: warnings.length > 0 ? warnings : undefined,
          };
        } catch (compileError: unknown) {
          return {
            isValid: false,
            error: `Compilation error: ${getErrorMessage(compileError, "Unknown compilation error")}`,
          };
        }
      } catch (importError) {
        // If we can't import vega-lite, do basic validation only
        console.warn("Vega-Lite library not available for advanced validation");

        // Basic validation passed, return as valid
        return {
          isValid: true,
          warnings: warnings.length > 0 ? warnings : undefined,
        };
      }
    } catch (error: unknown) {
      return {
        isValid: false,
        error: `Validation error: ${getErrorMessage(error, "Unknown error")}`,
      };
    }
  }

  /**
   * Validates specific field types in Vega-Lite
   */
  static validateFieldType(fieldType: string): boolean {
    const validTypes = [
      "quantitative",
      "temporal",
      "ordinal",
      "nominal",
      "geojson",
    ];
    return validTypes.includes(fieldType);
  }

  /**
   * Validates mark types in Vega-Lite
   */
  static validateMarkType(markType: string): boolean {
    const validMarks = [
      "area",
      "bar",
      "circle",
      "line",
      "point",
      "rect",
      "rule",
      "square",
      "text",
      "tick",
      "arc",
      "boxplot",
      "errorband",
      "errorbar",
      "geoshape",
      "image",
      "trail",
    ];
    return validMarks.includes(markType);
  }

  /**
   * Validates encoding channels
   */
  static validateEncodingChannel(channel: string): boolean {
    const validChannels = [
      "x",
      "y",
      "x2",
      "y2",
      "longitude",
      "latitude",
      "longitude2",
      "latitude2",
      "color",
      "fill",
      "stroke",
      "opacity",
      "fillOpacity",
      "strokeOpacity",
      "strokeWidth",
      "strokeDash",
      "size",
      "angle",
      "theta",
      "radius",
      "radius2",
      "shape",
      "text",
      "tooltip",
      "href",
      "key",
      "order",
      "detail",
      "facet",
      "row",
      "column",
    ];
    return validChannels.includes(channel);
  }

  /**
   * Provides auto-completion suggestions for field names
   */
  static getFieldSuggestions(spec: string): string[] {
    try {
      const parsedSpec: unknown = JSON.parse(spec);
      if (!isRecord(parsedSpec)) return [];
      const fields: string[] = [];

      // Extract field names from data if inline
      const data = parsedSpec.data;
      if (isRecord(data) && Array.isArray(data.values)) {
        const firstRow: unknown = data.values[0];
        if (firstRow && typeof firstRow === "object") {
          fields.push(...Object.keys(firstRow));
        }
      }

      // Extract field names from encoding
      if (isRecord(parsedSpec.encoding)) {
        Object.values(parsedSpec.encoding).forEach((encoding) => {
          if (isRecord(encoding) && typeof encoding.field === "string" && encoding.field) {
            if (!fields.includes(encoding.field)) {
              fields.push(encoding.field);
            }
          }
        });
      }

      return fields;
    } catch {
      return [];
    }
  }

  /**
   * Provides suggestions for common Vega-Lite patterns
   */
  static getCommonPatterns(): Record<string, VegaLitePattern> {
    return {
      "Basic Bar Chart": {
        mark: "bar",
        encoding: {
          x: { field: "category", type: "nominal" },
          y: { field: "value", type: "quantitative" },
        },
      },
      "Line Chart": {
        mark: "line",
        encoding: {
          x: { field: "date", type: "temporal" },
          y: { field: "value", type: "quantitative" },
        },
      },
      "Scatter Plot": {
        mark: "circle",
        encoding: {
          x: { field: "x", type: "quantitative" },
          y: { field: "y", type: "quantitative" },
        },
      },
      Histogram: {
        mark: "bar",
        encoding: {
          x: { bin: true, field: "value", type: "quantitative" },
          y: { aggregate: "count", type: "quantitative" },
        },
      },
    };
  }
}
