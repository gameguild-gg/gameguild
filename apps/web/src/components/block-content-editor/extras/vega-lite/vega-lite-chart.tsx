"use client"

import { useState, useEffect, useRef } from "react"
import { applyThemeOverrides, LIGHT_THEME_OVERRIDES, DARK_THEME_OVERRIDES } from "@/components/block-content-editor/extras/vega-lite/vega-theme-overrides"

// Theme mapping for vega-themes
const THEME_MAP: Record<string, string> = {
  'default': 'default',
  'dark': 'dark',
  // Light themes
  'excel': 'excel',
  'ggplot2': 'ggplot2',
  'quartz': 'quartz',
  'vox': 'vox',
  'fivethirtyeight': 'fivethirtyeight',
  'latimes': 'latimes',
  'urbaninstitute': 'urbaninstitute',
  'googlecharts': 'googlecharts',
  'powerbi': 'powerbi',
  // Dark versions
  'excel-dark': 'excel',
  'ggplot2-dark': 'ggplot2',
  'quartz-dark': 'quartz',
  'vox-dark': 'vox',
  'fivethirtyeight-dark': 'fivethirtyeight',
  'latimes-dark': 'latimes',
  'urbaninstitute-dark': 'urbaninstitute',
  'googlecharts-dark': 'googlecharts',
  'powerbi-dark': 'powerbi'
}

// Function to create dark version of any theme
function createDarkTheme(baseTheme: any) {
  return {
    ...baseTheme,
    background: "#1a1a1a",
    view: {
      ...baseTheme.view,
      fill: "#1a1a1a",
      stroke: "#404040"
    },
    axis: {
      ...baseTheme.axis,
      domainColor: "#666666",
      gridColor: "#333333",
      tickColor: "#666666",
      labelColor: "#cccccc",
      titleColor: "#ffffff"
    },
    legend: {
      ...baseTheme.legend,
      labelColor: "#cccccc",
      titleColor: "#ffffff"
    },
    title: {
      ...baseTheme.title,
      color: "#ffffff"
    },
    text: {
      ...baseTheme.text,
      fill: "#cccccc"
    }
  }
}

interface VegaChartData {
  parsedSpec: any
  isLoading: boolean
  error: string
  vegaRef: React.RefObject<HTMLDivElement | null>
  fullscreenVegaRef: React.RefObject<HTMLDivElement | null>
}

interface UseVegaLiteChartProps {
  spec: string
  layout?: "square" | "rectangular"
  theme?: string
  title?: string
}

export function useVegaLiteChart({ 
  spec, 
  layout = "rectangular", 
  theme = "default",
  title 
}: UseVegaLiteChartProps): VegaChartData {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>("")
  const [parsedSpec, setParsedSpec] = useState<any>(null)
  const vegaRef = useRef<HTMLDivElement>(null)
  const fullscreenVegaRef = useRef<HTMLDivElement>(null)

  const parseAndValidateSpec = (spec: string | any) => {
    console.log("VegaChart: Parsing spec, type:", typeof spec)
    
    let parsed
    try {
      // Handle case where spec might already be an object
      if (typeof spec === 'object') {
        parsed = spec
        console.log("VegaChart: Spec is already an object")
      } else {
        parsed = JSON.parse(spec)
        console.log("VegaChart: Parsed spec successfully")
      }
    } catch (parseError) {
      console.error("VegaChart: JSON parse error:", parseError)
      throw new Error("Invalid JSON specification")
    }

    // Validate that we have required fields
    if (!parsed.data && !parsed.datasets) {
      console.error("VegaChart: No data field in spec")
      throw new Error("Vega-Lite spec missing data field")
    }

    if (!parsed.mark && !parsed.layer) {
      console.error("VegaChart: No mark field in spec")
      throw new Error("Vega-Lite spec missing mark field")
    }

    // Apply theme if specified
    if (theme && theme !== "default") {
      parsed.config = parsed.config || {}
      parsed.config.theme = theme
      console.log("VegaChart: Applied theme:", theme)
    }

    // Apply layout specific configurations
    if (layout === "square") {
      parsed.width = 400
      parsed.height = 400
      console.log("VegaChart: Applied square layout to spec")
    } else {
      // Rectangular layout - use specific dimensions
      parsed.width = 800
      parsed.height = 300
      console.log("VegaChart: Applied rectangular layout to spec (800x300)")
    }

    return parsed
  }

  useEffect(() => {
    const loadData = async () => {
      console.log("VegaChart useEffect: spec exists?", !!spec)
      
      if (!spec) {
        console.log("VegaChart: No spec provided")
        setParsedSpec(null)
        setError("")
        return
      }

      setIsLoading(true)
      setError("")

      try {
        console.log("VegaChart: Processing spec:", spec.substring(0, 100) + "...")
        
        const validated = parseAndValidateSpec(spec)
        setParsedSpec(validated)
        console.log("VegaChart: Spec processed successfully")
      } catch (err: any) {
        console.error("VegaChart: Error processing spec:", err)
        setError(err.message || "Failed to process chart specification")
        setParsedSpec(null)
      } finally {
        setIsLoading(false)
      }
    }

    loadData()
  }, [spec, theme, layout])

  return {
    parsedSpec,
    isLoading,
    error,
    vegaRef,
    fullscreenVegaRef
  }
}

export async function renderVegaChart(
  container: HTMLElement, 
  parsedSpec: any,
  layout: "square" | "rectangular" = "rectangular",
  title?: string,
  theme: string = "default"
) {
  if (!container || !parsedSpec) {
    console.log("VegaChart Renderer: Missing container or spec")
    return
  }

  console.log("VegaChart Renderer: Starting render with layout:", layout, "theme:", theme)

  try {
    // Try to use Vega-Lite if available, otherwise show placeholder
    try {
      console.log("VegaChart Renderer: Attempting to import Vega-Lite and themes...")
      const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
      const vegaImport = await import("vega" as any).catch(() => null)
      const vegaThemesImport = await import("vega-themes" as any).catch(() => null)
      
      if (!vegaLiteImport || !vegaImport) {
        console.log("VegaChart Renderer: Vega-Lite not available, showing placeholder")
        throw new Error("Vega-Lite not available")
      }

      console.log("VegaChart Renderer: Vega-Lite imported successfully, compiling spec...")
      
      // Apply theme to the spec if theme is available and not default
      let specWithTheme = { ...parsedSpec }
      if (vegaThemesImport && theme !== "default" && THEME_MAP[theme]) {
        console.log("VegaChart Renderer: Applying theme:", theme)
        
        // Check if it's a dark version of a theme
        const isDarkTheme = theme.endsWith('-dark')
        const baseThemeName = isDarkTheme ? theme.replace('-dark', '') : theme
        const themeConfig = vegaThemesImport[THEME_MAP[theme]]
        
        if (themeConfig) {
          let finalThemeConfig = themeConfig
          
          // If it's a dark theme variant, apply dark modifications
          if (isDarkTheme && baseThemeName !== 'dark') {
            console.log("VegaChart Renderer: Creating dark version of theme:", baseThemeName)
            finalThemeConfig = createDarkTheme(themeConfig)
          }
          
          // Apply manual overrides from vega-theme-overrides.ts
          const overrides = isDarkTheme ? DARK_THEME_OVERRIDES[theme] : LIGHT_THEME_OVERRIDES[theme]
          if (overrides) {
            console.log("VegaChart Renderer: Applying manual theme overrides for:", theme)
            console.log("VegaChart Renderer: Override values:", JSON.stringify(overrides, null, 2))
            finalThemeConfig = applyThemeOverrides(finalThemeConfig, overrides)
            console.log("VegaChart Renderer: Final theme config:", JSON.stringify(finalThemeConfig, null, 2))
          } else {
            console.log("VegaChart Renderer: No overrides found for theme:", theme)
          }
          
          specWithTheme = {
            ...parsedSpec,
            config: {
              ...parsedSpec.config,
              ...finalThemeConfig
            }
          }
          console.log("VegaChart Renderer: Theme applied successfully")
        }
      }
      
      // Compile Vega-Lite spec to Vega spec
      const vegaSpec = vegaLiteImport.compile(specWithTheme).spec
      console.log("VegaChart Renderer: Spec compiled successfully")

      // Create a new view and render
      console.log("VegaChart Renderer: Creating Vega view...")
      
      // Clear container first
      container.innerHTML = ""
      
      const view = new vegaImport.View(vegaImport.parse(vegaSpec), {
        renderer: "svg"
      })

      console.log("VegaChart Renderer: Initializing view with container...")
      view.initialize(container)
      
      console.log("VegaChart Renderer: Running view...")
      try {
        await view.runAsync()
        console.log("VegaChart Renderer: View ran successfully")
        
        // Force update to ensure rendering
        view.hover()
        
      } catch (runError) {
        console.error("VegaChart Renderer: Error during view.runAsync():", runError)
        throw runError
      }
      
      // Wait a bit for the DOM to be updated
      setTimeout(async () => {
        console.log("VegaChart Renderer: Checking DOM after render...")
        console.log("VegaChart Renderer: Container children count:", container.children.length)
        
        if (container.children.length === 0) {
          console.error("VegaChart Renderer: No children in container after render!")
          console.log("VegaChart Renderer: Trying alternative rendering approach...")
          
          // Try alternative approach: get SVG directly from view
          try {
            console.log("VegaChart Renderer: Trying toSVG() method...")
            const svgString = await view.toSVG()
            console.log("VegaChart Renderer: Got SVG string, length:", svgString.length)
            container.innerHTML = svgString
            
            // Force display of the inserted SVG
            if (container.firstElementChild) {
              const svgElement = container.firstElementChild as HTMLElement
              svgElement.style.display = "block"
              
              // No transitions - instant rendering for real-time visualization
              svgElement.style.transformOrigin = "center"
              
              // Force fixed dimensions to prevent movement
              if (layout === "square") {
                svgElement.style.width = "400px !important"
                svgElement.style.height = "400px !important"
                svgElement.style.margin = "0 auto"
                svgElement.style.minWidth = "400px"
                svgElement.style.minHeight = "400px"
                svgElement.style.maxWidth = "400px"
                svgElement.style.maxHeight = "400px"
                console.log("VegaChart Renderer: Applied fixed square styles to SVG")
              } else {
                // Rectangular layout - force fixed dimensions
                svgElement.style.width = "100%"
                svgElement.style.height = "300px !important"
                svgElement.style.maxWidth = "100%"
                svgElement.style.minHeight = "300px"
                svgElement.style.maxHeight = "300px"
                console.log("VegaChart Renderer: Applied fixed rectangular styles to SVG")
              }
              
              // Prevent any auto-sizing that could cause movement
              svgElement.style.overflow = "hidden"
              svgElement.style.position = "relative"
              
              console.log("VegaChart Renderer: Applied styles to inserted SVG")
            }
          } catch (svgError) {
            console.error("VegaChart Renderer: Error getting SVG:", svgError)
            
            // Try canvas approach as last resort
            try {
              console.log("VegaChart Renderer: Trying canvas approach...")
              const canvas = await view.toCanvas()
              console.log("VegaChart Renderer: Got canvas:", !!canvas)
              container.innerHTML = ""
              container.appendChild(canvas)
              
              // Apply styles to canvas
              canvas.style.display = "block"
              
              // No transitions - instant rendering for real-time visualization
              canvas.style.transformOrigin = "center"
              
              // Force fixed dimensions to prevent movement
              if (layout === "square") {
                canvas.style.width = "400px !important"
                canvas.style.height = "400px !important"
                canvas.style.margin = "0 auto"
                canvas.style.minWidth = "400px"
                canvas.style.minHeight = "400px"
                canvas.style.maxWidth = "400px"
                canvas.style.maxHeight = "400px"
              } else {
                // Rectangular layout - force fixed dimensions
                canvas.style.width = "100%"
                canvas.style.height = "300px !important"
                canvas.style.maxWidth = "100%"
                canvas.style.minHeight = "300px"
                canvas.style.maxHeight = "300px"
              }
              
              // Prevent any auto-sizing that could cause movement
              canvas.style.overflow = "hidden"
              canvas.style.position = "relative"
              
              console.log("VegaChart Renderer: Canvas inserted and styled")
            } catch (canvasError) {
              console.error("VegaChart Renderer: Canvas approach failed:", canvasError)
              
              // Last resort: try to re-initialize
              try {
                view.initialize(container)
                view.runAsync().then(() => {
                  console.log("VegaChart Renderer: Retry render completed")
                }).catch((err: any) => {
                  console.error("VegaChart Renderer: Retry render failed:", err)
                })
              } catch (initError) {
                console.error("VegaChart Renderer: Re-initialization failed:", initError)
              }
            }
          }
        }
        
        // Apply centering styles for square layout
        if (container.firstElementChild) {
          const svgElement = container.firstElementChild as HTMLElement
          svgElement.style.display = "block"
          
          // No transitions - instant rendering for real-time visualization
          svgElement.style.transformOrigin = "center"
          
          console.log("VegaChart Renderer: Applying styles for layout:", layout)
          
          // Force fixed dimensions to prevent movement
          if (layout === "square") {
            svgElement.style.width = "400px !important"
            svgElement.style.height = "400px !important"
            svgElement.style.margin = "0 auto"
            svgElement.style.minWidth = "400px"
            svgElement.style.minHeight = "400px"
            svgElement.style.maxWidth = "400px"
            svgElement.style.maxHeight = "400px"
            console.log("VegaChart Renderer: Applied fixed square layout centering")
          } else {
            // Rectangular layout - force fixed dimensions
            svgElement.style.width = "100%"
            svgElement.style.height = "300px !important"
            svgElement.style.maxWidth = "100%"
            svgElement.style.minHeight = "300px"
            svgElement.style.maxHeight = "300px"
            console.log("VegaChart Renderer: Applied fixed rectangular layout styling")
          }
          
          // Prevent any auto-sizing that could cause movement
          svgElement.style.overflow = "hidden"
          svgElement.style.position = "relative"
          
          // Force minimum dimensions if SVG has no size
          const rect = svgElement.getBoundingClientRect()
          console.log("VegaChart Renderer: SVG rect:", rect)
          if (rect.width === 0 || rect.height === 0) {
            if (layout === "square") {
              svgElement.style.width = "400px"
              svgElement.style.height = "400px"
            } else {
              svgElement.style.width = "800px"
              svgElement.style.height = "300px"
            }
            console.log("VegaChart Renderer: Applied fallback dimensions for layout:", layout)
          }
          
          console.log("VegaChart Renderer: Applied visibility styles to SVG")
          console.log("VegaChart Renderer: SVG element:", svgElement.tagName)
          console.log("VegaChart Renderer: SVG dimensions:", svgElement.getBoundingClientRect())
          console.log("VegaChart Renderer: SVG innerHTML length:", svgElement.innerHTML.length)
        } else {
          console.log("VegaChart Renderer: No SVG element found in container!")
        }
      }, 50) // Reduced timeout for faster real-time rendering
    } catch (vegaError: any) {
      console.log("VegaChart Renderer: Vega error, showing placeholder:", vegaError.message)
      // If vega-lite is not available, show placeholder
      container.innerHTML = `
        <div style="
          display: flex; 
          align-items: center; 
          justify-content: center; 
          height: 300px; 
          background: #f8f9fa; 
          border: 2px dashed #dee2e6; 
          border-radius: 8px;
          flex-direction: column;
          color: #6c757d;
          font-family: system-ui, -apple-system, sans-serif;
        ">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 3v18h18"/>
            <path d="m19 9-5 5-4-4-3 3"/>
          </svg>
          <h3 style="margin: 16px 0 8px 0; font-size: 16px; font-weight: 600;">Vega-Lite Chart</h3>
          <p style="margin: 0; font-size: 14px; text-align: center; max-width: 300px;">
            ${title || "Interactive Data Visualization"}
          </p>
          <div style="margin-top: 12px; padding: 8px 12px; background: #e9ecef; border-radius: 4px; font-family: monospace; font-size: 12px;">
            Vega-Lite Renderer (${vegaError.message})
          </div>
        </div>
      `
    }
  } catch (err: any) {
    console.error("VegaChart Renderer: Error rendering chart:", err)
    throw err
  }
}