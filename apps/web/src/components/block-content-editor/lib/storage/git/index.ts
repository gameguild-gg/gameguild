// Git Storage Module
// Provides automatic history and snapshots for projects using isomorphic-git

export { getGitFS, getFS, getProjectDir, ensureProjectsDir, projectRepoExists } from './git-fs'

export {
  GitHistoryManager,
  getHistoryManager,
  type CommitInfo,
  type SnapshotInfo
} from './git-history-manager'
