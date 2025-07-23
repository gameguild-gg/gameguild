interface SyncConfig {
  enabled: boolean
  serverUrl: string
  timeout: number
  retries: number
  autoSync: boolean
  syncInterval: number // in milliseconds
  batchSize: number
  debugMode: boolean
}

const DEFAULT_CONFIG: SyncConfig = {
  enabled: false, // Disabled by default until server is ready
  serverUrl: process.env.NEXT_PUBLIC_API_URL || "http://localhost:3001/api",
  timeout: 10000,
  retries: 3,
  autoSync: true,
  syncInterval: 30000, // 30 seconds
  batchSize: 10,
  debugMode: process.env.NODE_ENV === "development",
}

class SyncConfigManager {
  private config: SyncConfig
  private listeners: Array<(config: SyncConfig) => void> = []

  constructor() {
    this.config = this.loadConfig()
  }

  private loadConfig(): SyncConfig {
    if (typeof window === "undefined") return DEFAULT_CONFIG

    try {
      const stored = localStorage.getItem("gg-editor-sync-config")
      if (stored) {
        const parsed = JSON.parse(stored)
        return { ...DEFAULT_CONFIG, ...parsed }
      }
    } catch (error) {
      console.warn("Failed to load sync config from localStorage:", error)
    }

    return DEFAULT_CONFIG
  }

  private saveConfig(): void {
    if (typeof window === "undefined") return

    try {
      localStorage.setItem("gg-editor-sync-config", JSON.stringify(this.config))
    } catch (error) {
      console.warn("Failed to save sync config to localStorage:", error)
    }
  }

  getConfig(): SyncConfig {
    return { ...this.config }
  }

  updateConfig(updates: Partial<SyncConfig>): void {
    const oldConfig = { ...this.config }
    this.config = { ...this.config, ...updates }
    this.saveConfig()

    // Notify listeners of config changes
    this.listeners.forEach((listener) => {
      try {
        listener(this.getConfig())
      } catch (error) {
        console.error("Error in sync config listener:", error)
      }
    })

    if (this.config.debugMode) {
      console.log("Sync config updated:", {
        old: oldConfig,
        new: this.config,
        changes: updates,
      })
    }
  }

  isEnabled(): boolean {
    return this.config.enabled
  }

  enable(): void {
    this.updateConfig({ enabled: true })
  }

  disable(): void {
    this.updateConfig({ enabled: false })
  }

  toggle(): boolean {
    const newState = !this.config.enabled
    this.updateConfig({ enabled: newState })
    return newState
  }

  onConfigChange(listener: (config: SyncConfig) => void): () => void {
    this.listeners.push(listener)

    // Return unsubscribe function
    return () => {
      const index = this.listeners.indexOf(listener)
      if (index > -1) {
        this.listeners.splice(index, 1)
      }
    }
  }

  // Preset configurations
  setDevelopmentMode(): void {
    this.updateConfig({
      enabled: true,
      serverUrl: "http://localhost:3001/api",
      debugMode: true,
      syncInterval: 10000, // 10 seconds for development
      timeout: 5000,
    })
  }

  setProductionMode(): void {
    this.updateConfig({
      enabled: true,
      serverUrl: process.env.NEXT_PUBLIC_API_URL || "https://api.gameguild.dev/api",
      debugMode: false,
      syncInterval: 60000, // 1 minute for production
      timeout: 15000,
    })
  }

  setOfflineMode(): void {
    this.updateConfig({
      enabled: false,
      autoSync: false,
    })
  }

  // Validation
  validateConfig(): { isValid: boolean; errors: string[] } {
    const errors: string[] = []

    if (this.config.enabled) {
      if (!this.config.serverUrl || !this.config.serverUrl.trim()) {
        errors.push("Server URL is required when sync is enabled")
      }

      try {
        new URL(this.config.serverUrl)
      } catch {
        errors.push("Server URL must be a valid URL")
      }

      if (this.config.timeout < 1000) {
        errors.push("Timeout must be at least 1000ms")
      }

      if (this.config.retries < 0 || this.config.retries > 10) {
        errors.push("Retries must be between 0 and 10")
      }

      if (this.config.syncInterval < 5000) {
        errors.push("Sync interval must be at least 5000ms")
      }

      if (this.config.batchSize < 1 || this.config.batchSize > 100) {
        errors.push("Batch size must be between 1 and 100")
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
    }
  }

  // Debug helpers
  logConfig(): void {
    if (this.config.debugMode) {
      console.group("🔄 Sync Configuration")
      console.table(this.config)
      console.groupEnd()
    }
  }

  getStatus(): string {
    if (!this.config.enabled) return "disabled"
    if (!navigator.onLine) return "offline"
    return "enabled"
  }
}

// Singleton instance
export const syncConfig = new SyncConfigManager()

// Export types for use in other files
export type { SyncConfig }
export { DEFAULT_CONFIG }
