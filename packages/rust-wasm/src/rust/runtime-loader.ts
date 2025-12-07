/**
 * RuntimeLoader for Rust WASM Compiler
 * 
 * This loader dynamically loads the main.js script which initializes
 * the Rust WASM compiler and exposes window.RustCompiler
 */

export class RuntimeLoader {
  private isLoaded = false
  private loadPromise: Promise<void> | null = null

  async initialize(basePath: string = ''): Promise<void> {
    if (this.isLoaded) {
      return Promise.resolve()
    }

    if (this.loadPromise) {
      return this.loadPromise
    }

    this.loadPromise = this._doInitialize(basePath)
    return this.loadPromise
  }

  private async _doInitialize(basePath: string): Promise<void> {
    console.log('[Rust] Initializing Rust compiler runtime...')

    // Check if already loaded
    if ((window as any).RustCompiler) {
      this.isLoaded = true
      console.log('[Rust] ✓ Rust compiler already loaded')
      return
    }

    // Load main.js using script tag (ES module)
    const scriptUrl = `${basePath}/main.js`
    console.log(`[Rust] Loading runtime from ${scriptUrl}`)
    
    try {
      // Create and append script tag
      const script = document.createElement('script')
      script.type = 'module'
      script.src = scriptUrl
      
      // Wait for script to load
      await new Promise<void>((resolve, reject) => {
        script.onload = () => resolve()
        script.onerror = () => reject(new Error(`Failed to load script: ${scriptUrl}`))
        document.head.appendChild(script)
      })
      
      console.log('[Rust] Script loaded, waiting for initialization...')
    } catch (error) {
      console.error('[Rust] Failed to load main.js:', error)
      throw new Error(`Failed to load Rust compiler from ${scriptUrl}: ${error instanceof Error ? error.message : String(error)}`)
    }
    
    // Wait for window.RustCompiler to be available
    const maxAttempts = 100 // 10 seconds
    for (let i = 0; i < maxAttempts; i++) {
      if ((window as any).RustCompiler && (window as any).RustCompiler.compileRust) {
        this.isLoaded = true
        console.log('[Rust] ✓ Rust compiler ready')
        return
      }
      await new Promise(resolve => setTimeout(resolve, 100))
    }

    throw new Error('Failed to initialize Rust compiler: RustCompiler.compileRust not found after loading main.js')
  }

  isInitialized(): boolean {
    return this.isLoaded
  }
}
