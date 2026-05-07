export interface VegaLiteValidationResult {
  isValid: boolean
  error?: string
  warnings?: string[]
}

export class VegaLiteValidator {
  /**
   * Validates a Vega-Lite specification
   */
  static async validateSpec(spec: string): Promise<VegaLiteValidationResult> {
    if (!spec || spec.trim() === "") {
      return {
        isValid: false,
        error: "Specification cannot be empty"
      }
    }

    try {
      // Parse JSON
      let parsedSpec: any
      try {
        parsedSpec = JSON.parse(spec)
      } catch (parseError) {
        return {
          isValid: false,
          error: `Invalid JSON: ${parseError instanceof Error ? parseError.message : 'Unknown parsing error'}`
        }
      }

      // Basic structure validation
      if (typeof parsedSpec !== 'object' || parsedSpec === null) {
        return {
          isValid: false,
          error: "Specification must be a JSON object"
        }
      }

      // Check for required fields
      const warnings: string[] = []

      // Check for $schema (recommended but not required)
      if (!parsedSpec.$schema) {
        warnings.push("Missing $schema field (recommended)")
      }

      // Check for data source
      if (!parsedSpec.data && !parsedSpec.datasets) {
        return {
          isValid: false,
          error: "Specification must include a 'data' field or 'datasets'"
        }
      }

      // Check for mark type
      if (!parsedSpec.mark && !parsedSpec.layer && !parsedSpec.concat && !parsedSpec.hconcat && !parsedSpec.vconcat && !parsedSpec.facet && !parsedSpec.repeat) {
        return {
          isValid: false,
          error: "Specification must include a 'mark' field or be a composite specification (layer, concat, etc.)"
        }
      }

      // Check for encoding (if using mark)
      if (parsedSpec.mark && !parsedSpec.encoding) {
        warnings.push("Mark-based charts typically need an 'encoding' field")
      }

      // Advanced validation using Vega-Lite library if available
      try {
        const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
        
        if (!vegaLiteImport) {
          // If we can't import vega-lite, do basic validation only
          console.warn("Vega-Lite library not available for advanced validation")
          
          // Basic validation passed, return as valid
          return {
            isValid: true,
            warnings: warnings.length > 0 ? warnings : undefined
          }
        }
        
        try {
          // Try to compile the specification
          const compiled = vegaLiteImport.compile(parsedSpec)
          if (!compiled || !compiled.spec) {
            return {
              isValid: false,
              error: "Failed to compile Vega-Lite specification"
            }
          }

          return {
            isValid: true,
            warnings: warnings.length > 0 ? warnings : undefined
          }
        } catch (compileError: any) {
          return {
            isValid: false,
            error: `Compilation error: ${compileError.message || 'Unknown compilation error'}`
          }
        }
      } catch (importError) {
        // If we can't import vega-lite, do basic validation only
        console.warn("Vega-Lite library not available for advanced validation")
        
        // Basic validation passed, return as valid
        return {
          isValid: true,
          warnings: warnings.length > 0 ? warnings : undefined
        }
      }

    } catch (error: any) {
      return {
        isValid: false,
        error: `Validation error: ${error.message || 'Unknown error'}`
      }
    }
  }

  /**
   * Validates specific field types in Vega-Lite
   */
  static validateFieldType(fieldType: string): boolean {
    const validTypes = ["quantitative", "temporal", "ordinal", "nominal", "geojson"]
    return validTypes.includes(fieldType)
  }

  /**
   * Validates mark types in Vega-Lite
   */
  static validateMarkType(markType: string): boolean {
    const validMarks = [
      "area", "bar", "circle", "line", "point", "rect", "rule", "square", "text", "tick",
      "arc", "boxplot", "errorband", "errorbar", "geoshape", "image", "trail"
    ]
    return validMarks.includes(markType)
  }

  /**
   * Validates encoding channels
   */
  static validateEncodingChannel(channel: string): boolean {
    const validChannels = [
      "x", "y", "x2", "y2", "longitude", "latitude", "longitude2", "latitude2",
      "color", "fill", "stroke", "opacity", "fillOpacity", "strokeOpacity",
      "strokeWidth", "strokeDash", "size", "angle", "theta", "radius", "radius2",
      "shape", "text", "tooltip", "href", "key", "order", "detail", "facet",
      "row", "column"
    ]
    return validChannels.includes(channel)
  }

  /**
   * Provides auto-completion suggestions for field names
   */
  static getFieldSuggestions(spec: string): string[] {
    try {
      const parsedSpec = JSON.parse(spec)
      const fields: string[] = []
      
      // Extract field names from data if inline
      if (parsedSpec.data?.values && Array.isArray(parsedSpec.data.values)) {
        const firstRow = parsedSpec.data.values[0]
        if (firstRow && typeof firstRow === 'object') {
          fields.push(...Object.keys(firstRow))
        }
      }

      // Extract field names from encoding
      if (parsedSpec.encoding && typeof parsedSpec.encoding === 'object') {
        Object.values(parsedSpec.encoding).forEach((encoding: any) => {
          if (encoding && typeof encoding === 'object' && encoding.field) {
            if (!fields.includes(encoding.field)) {
              fields.push(encoding.field)
            }
          }
        })
      }

      return fields
    } catch {
      return []
    }
  }

  /**
   * Provides suggestions for common Vega-Lite patterns
   */
  static getCommonPatterns(): { [key: string]: any } {
    return {
      "Basic Bar Chart": {
        "mark": "bar",
        "encoding": {
          "x": {"field": "category", "type": "nominal"},
          "y": {"field": "value", "type": "quantitative"}
        }
      },
      "Line Chart": {
        "mark": "line",
        "encoding": {
          "x": {"field": "date", "type": "temporal"},
          "y": {"field": "value", "type": "quantitative"}
        }
      },
      "Scatter Plot": {
        "mark": "circle",
        "encoding": {
          "x": {"field": "x", "type": "quantitative"},
          "y": {"field": "y", "type": "quantitative"}
        }
      },
      "Histogram": {
        "mark": "bar",
        "encoding": {
          "x": {"bin": true, "field": "value", "type": "quantitative"},
          "y": {"aggregate": "count", "type": "quantitative"}
        }
      }
    }
  }
}