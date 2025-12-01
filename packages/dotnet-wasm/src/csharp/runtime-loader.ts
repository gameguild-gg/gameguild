/**
 * RuntimeLoader for C# WASM Runtime
 * 
 * This loader simply ensures the main.js script is loaded.
 * The actual .NET initialization happens in main.js using Blazor.
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

  private async _doInitialize(_basePath: string): Promise<void> {
    console.log('[DotNet] Initializing C# runtime...')

    // The main.js script is loaded via <script type="module" src="/managed/main.js">
    // in the HTML or dynamically here
    
    // Wait for window.CSharpCompiler to be available
    const maxAttempts = 50 // 5 seconds
    for (let i = 0; i < maxAttempts; i++) {
      if ((window as any).CSharpCompiler) {
        this.isLoaded = true
        console.log('[DotNet] ✓ C# runtime ready')
        return
      }
      await new Promise(resolve => setTimeout(resolve, 100))
    }

    throw new Error('Failed to initialize C# runtime: CSharpCompiler not found')
  }

  isInitialized(): boolean {
    return this.isLoaded
  }
}
