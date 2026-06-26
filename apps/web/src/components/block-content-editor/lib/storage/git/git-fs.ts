import LightningFS from '@isomorphic-git/lightning-fs'

/**
 * LightningFS singleton for Git storage
 * Single database named 'block-content-editor' with isolated repos per project
 * Structure: /projects/{projectId}/.git/
 */

let fsInstance: LightningFS | null = null

export function getGitFS(): LightningFS {
  if (!fsInstance) {
    fsInstance = new LightningFS('block-content-editor')
  }
  return fsInstance
}

export function getFS() {
  return getGitFS().promises
}

/**
 * Get the directory path for a project's git repository
 */
export function getProjectDir(projectId: string): string {
  return `/projects/${projectId}`
}

/**
 * Ensure the base /projects directory exists
 */
export async function ensureProjectsDir(): Promise<void> {
  const fs = getFS()
  try {
    await fs.stat('/projects')
  } catch {
    await fs.mkdir('/projects')
  }
}

/**
 * Check if a project's git repository exists
 */
export async function projectRepoExists(projectId: string): Promise<boolean> {
  const fs = getFS()
  try {
    await fs.stat(`${getProjectDir(projectId)}/.git`)
    return true
  } catch {
    return false
  }
}
