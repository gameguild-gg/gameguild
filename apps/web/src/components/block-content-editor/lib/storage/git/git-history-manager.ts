import git from 'isomorphic-git'
import { getGitFS, getFS, getProjectDir, ensureProjectsDir, projectRepoExists } from './git-fs'

export interface CommitInfo {
  sha: string
  message: string
  date: string
}

export interface SnapshotInfo {
  tag: string
  sha: string
  message: string
  date: string
}

/**
 * Git History Manager
 * 
 * Manages linear commit history per project with optional snapshots (tags).
 * - Every save() creates a commit (automatic history)
 * - Snapshots are tags on specific commits (user-created versions)
 */
export class GitHistoryManager {
  private readonly author = { name: 'Block Content Editor User', email: 'user@block-content-editor.local' }

  /**
   * Initialize a project's git repository if it doesn't exist
   */
  async initRepo(projectId: string): Promise<void> {
    await ensureProjectsDir()
    
    const dir = getProjectDir(projectId)
    const fs = getFS()
    
    if (await projectRepoExists(projectId)) {
      return // Already initialized
    }

    // Create project directory
    try {
      await fs.mkdir(dir)
    } catch {
      // Directory may already exist
    }

    // Initialize git repo
    await git.init({
      fs: getGitFS(),
      dir,
      defaultBranch: 'main'
    })

    console.log(`📁 Git repo initialized: ${dir}/.git/`)
  }

  /**
   * Create a commit with the current project data
   * This is called automatically on every save()
   * @param projectId - Project identifier
   * @param data - Serialized project data (JSON string)
   * @param message - Commit message (auto-generated if not provided)
   */
  async commitProject(
    projectId: string,
    data: string,
    message?: string
  ): Promise<CommitInfo> {
    await this.initRepo(projectId)
    
    const dir = getProjectDir(projectId)
    const fs = getFS()
    const gitFs = getGitFS()

    // Generate commit message if not provided
    const commitMessage = message || `Auto-save: ${new Date().toISOString()}`

    // Write project.json
    await fs.writeFile(`${dir}/project.json`, data, 'utf8')

    // Stage the file
    await git.add({
      fs: gitFs,
      dir,
      filepath: 'project.json'
    })

    // Check if there are changes to commit
    const status = await git.status({
      fs: gitFs,
      dir,
      filepath: 'project.json'
    })

    // If file is unmodified, skip commit
    if (status === 'unmodified') {
      // Return the current HEAD commit info
      const head = await this.getHeadCommit(projectId)
      if (head) {
        return head
      }
    }

    // Get parent commit (if exists)
    let parent: string[] = []
    try {
      const headRef = await git.resolveRef({ fs: gitFs, dir, ref: 'HEAD' })
      parent = [headRef]
    } catch {
      // No parent - first commit
    }

    // Create commit
    const sha = await git.commit({
      fs: gitFs,
      dir,
      message: commitMessage,
      author: this.author,
      parent
    })

    const date = new Date().toISOString()

    console.log(`📝 Git commit: ${sha.substring(0, 7)} - ${commitMessage}`)

    return { sha, message: commitMessage, date }
  }

  /**
   * Get the HEAD commit info
   */
  private async getHeadCommit(projectId: string): Promise<CommitInfo | null> {
    if (!(await projectRepoExists(projectId))) {
      return null
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      const sha = await git.resolveRef({ fs: gitFs, dir, ref: 'HEAD' })
      const commit = await git.readCommit({ fs: gitFs, dir, oid: sha })
      
      return {
        sha,
        message: commit.commit.message,
        date: new Date(commit.commit.author.timestamp * 1000).toISOString()
      }
    } catch {
      return null
    }
  }

  /**
   * Create a snapshot (tag) on the current HEAD commit
   * @param projectId - Project identifier
   * @param tag - Tag name for the snapshot
   * @param message - Optional message for the tag
   */
  async createSnapshot(
    projectId: string,
    tag: string,
    message?: string
  ): Promise<SnapshotInfo> {
    if (!(await projectRepoExists(projectId))) {
      throw new Error(`Project ${projectId} has no Git history. Save the project first.`)
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    // Sanitize tag name
    const sanitizedTag = this.sanitizeTagName(tag)

    // Check if tag already exists
    const existingTags = await git.listTags({ fs: gitFs, dir })
    if (existingTags.includes(sanitizedTag)) {
      throw new Error(`Snapshot "${sanitizedTag}" already exists`)
    }

    // Get current HEAD
    const sha = await git.resolveRef({ fs: gitFs, dir, ref: 'HEAD' })
    const commit = await git.readCommit({ fs: gitFs, dir, oid: sha })

    // Create tag
    await git.tag({
      fs: gitFs,
      dir,
      ref: sanitizedTag,
      object: sha
    })

    const date = new Date(commit.commit.author.timestamp * 1000).toISOString()
    const snapshotMessage = message || commit.commit.message

    console.log(`📸 Snapshot created:`)
    console.log(`   Project ID: ${projectId}`)
    console.log(`   Path: ${dir}/`)
    console.log(`   Tag: ${sanitizedTag}`)
    console.log(`   SHA: ${sha}`)

    return {
      tag: sanitizedTag,
      sha,
      message: snapshotMessage,
      date
    }
  }

  /**
   * Get the next version number for auto-naming snapshots
   */
  async getNextVersionNumber(projectId: string, baseName: string): Promise<number> {
    const snapshots = await this.listSnapshots(projectId)
    
    // Find the highest "v{N}" suffix for this base name
    let maxVersion = 0
    const sanitizedBase = this.sanitizeTagName(baseName)
    
    for (const snapshot of snapshots) {
      // Match pattern like "ProjectName-v1", "ProjectName-v2"
      const match = snapshot.tag.match(new RegExp(`^${sanitizedBase}-v(\\d+)$`))
      if (match && match[1]) {
        const num = parseInt(match[1], 10)
        if (num > maxVersion) {
          maxVersion = num
        }
      }
    }
    
    return maxVersion + 1
  }

  /**
   * List all commits (history) for a project
   * Returns commits in reverse chronological order (newest first)
   */
  async listHistory(projectId: string, maxCount: number = 50): Promise<CommitInfo[]> {
    if (!(await projectRepoExists(projectId))) {
      return []
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      const commits = await git.log({
        fs: gitFs,
        dir,
        depth: maxCount
      })

      return commits.map(commit => ({
        sha: commit.oid,
        message: commit.commit.message,
        date: new Date(commit.commit.author.timestamp * 1000).toISOString()
      }))
    } catch (error) {
      console.error('Failed to list history:', error)
      return []
    }
  }

  /**
   * List all snapshots (tags) for a project
   */
  async listSnapshots(projectId: string): Promise<SnapshotInfo[]> {
    if (!(await projectRepoExists(projectId))) {
      return []
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      const tags = await git.listTags({ fs: gitFs, dir })
      const snapshots: SnapshotInfo[] = []

      for (const tag of tags) {
        try {
          const sha = await git.resolveRef({ fs: gitFs, dir, ref: tag })
          const commit = await git.readCommit({ fs: gitFs, dir, oid: sha })
          
          snapshots.push({
            tag,
            sha,
            message: commit.commit.message,
            date: new Date(commit.commit.author.timestamp * 1000).toISOString()
          })
        } catch (error) {
          console.warn(`Failed to read snapshot ${tag}:`, error)
        }
      }

      // Sort by date descending (newest first)
      snapshots.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())

      return snapshots
    } catch (error) {
      console.error('Failed to list snapshots:', error)
      return []
    }
  }

  /**
   * Load project data from a specific commit
   */
  async loadCommit(projectId: string, sha: string): Promise<string | null> {
    if (!(await projectRepoExists(projectId))) {
      return null
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      const { blob } = await git.readBlob({
        fs: gitFs,
        dir,
        oid: sha,
        filepath: 'project.json'
      })

      const decoder = new TextDecoder('utf-8')
      return decoder.decode(blob)
    } catch (error) {
      console.error(`Failed to load commit ${sha}:`, error)
      return null
    }
  }

  /**
   * Load project data from a snapshot (tag)
   */
  async loadSnapshot(projectId: string, tag: string): Promise<string | null> {
    if (!(await projectRepoExists(projectId))) {
      return null
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      const sha = await git.resolveRef({ fs: gitFs, dir, ref: tag })
      return await this.loadCommit(projectId, sha)
    } catch (error) {
      console.error(`Failed to load snapshot ${tag}:`, error)
      return null
    }
  }

  /**
   * Delete a snapshot (tag only, commit remains in history)
   */
  async deleteSnapshot(projectId: string, tag: string): Promise<boolean> {
    if (!(await projectRepoExists(projectId))) {
      return false
    }

    const dir = getProjectDir(projectId)
    const gitFs = getGitFS()

    try {
      await git.deleteTag({ fs: gitFs, dir, ref: tag })
      console.log(`🗑️ Snapshot deleted: ${tag}`)
      return true
    } catch (error) {
      console.error(`Failed to delete snapshot ${tag}:`, error)
      return false
    }
  }

  /**
   * Check if a project has any commits
   */
  async hasHistory(projectId: string): Promise<boolean> {
    const history = await this.listHistory(projectId, 1)
    return history.length > 0
  }

  /**
   * Check if a project has any snapshots
   */
  async hasSnapshots(projectId: string): Promise<boolean> {
    const snapshots = await this.listSnapshots(projectId)
    return snapshots.length > 0
  }

  /**
   * Delete the entire git repository for a project
   * Used when deleting a project
   */
  async deleteProjectRepo(projectId: string): Promise<boolean> {
    if (!(await projectRepoExists(projectId))) {
      return true
    }

    const dir = getProjectDir(projectId)
    const fs = getFS()

    try {
      await this.recursiveDelete(dir)
      console.log(`🗑️ Git repo deleted: ${dir}`)
      return true
    } catch (error) {
      console.error(`Failed to delete project repo ${projectId}:`, error)
      return false
    }
  }

  /**
   * Recursively delete a directory
   */
  private async recursiveDelete(path: string): Promise<void> {
    const fs = getFS()
    
    try {
      const stat = await fs.stat(path)
      
      if (stat.isDirectory()) {
        const entries = await fs.readdir(path)
        for (const entry of entries) {
          await this.recursiveDelete(`${path}/${entry}`)
        }
        await fs.rmdir(path)
      } else {
        await fs.unlink(path)
      }
    } catch {
      // Ignore errors for non-existent paths
    }
  }

  /**
   * Sanitize a string to be used as a git tag
   */
  private sanitizeTagName(name: string): string {
    return name
      .trim()
      .replace(/[\s~^:?*\[\]\\]+/g, '-')
      .replace(/\.{2,}/g, '.')
      .replace(/^-+|-+$/g, '')
      .replace(/-{2,}/g, '-')
      || 'snapshot'
  }
}

// Singleton instance
let historyManagerInstance: GitHistoryManager | null = null

export function getHistoryManager(): GitHistoryManager {
  if (!historyManagerInstance) {
    historyManagerInstance = new GitHistoryManager()
  }
  return historyManagerInstance
}
