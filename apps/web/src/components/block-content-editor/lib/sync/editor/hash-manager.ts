export class HashManager {
  static async generateHash(data: string): Promise<string> {
    if (typeof crypto !== "undefined" && crypto.subtle) {
      try {
        const encoder = new TextEncoder()
        const dataBuffer = encoder.encode(data)
        const hashBuffer = await crypto.subtle.digest("SHA-1", dataBuffer)
        const hashArray = Array.from(new Uint8Array(hashBuffer))
        return hashArray.map((b) => b.toString(16).padStart(2, "0")).join("")
      } catch (error) {
        console.warn("Failed to generate SHA-1 hash with Web Crypto API, falling back to simple hash:", error)
        return this.generateSimpleHash(data)
      }
    }

    // Fallback for environments without Web Crypto API
    return this.generateSimpleHash(data)
  }

  private static generateSimpleHash(data: string): string {
    let hash = 0
    if (data.length === 0) return hash.toString()

    for (let i = 0; i < data.length; i++) {
      const char = data.charCodeAt(i)
      hash = (hash << 5) - hash + char
      hash = hash & hash // Convert to 32-bit integer
    }

    return Math.abs(hash).toString(16)
  }

  static async compareHashes(hash1: string, hash2: string): Promise<boolean> {
    return hash1 === hash2
  }

  static async generateProjectHash(projectData: {
    name: string
    data: string
    tags: string[]
    updatedAt: string
  }): Promise<string> {
    const hashInput = JSON.stringify({
      name: projectData.name,
      data: projectData.data,
      tags: projectData.tags.sort(), // Sort tags for consistent hashing
      updatedAt: projectData.updatedAt,
    })

    return this.generateHash(hashInput)
  }

  static async validateHash(data: string, expectedHash: string): Promise<boolean> {
    const actualHash = await this.generateHash(data)
    return this.compareHashes(actualHash, expectedHash)
  }
}
