import type { ProjectMode, NodeRestrictions } from "./project-modes"

// Project-level preferences structure
export interface ProjectPreferences {
  global: {
    mode?: ProjectMode
    restrictions?: NodeRestrictions
  }
  nodes: {
    [nodeType: string]: {
      mode?: ProjectMode
      restrictions?: NodeRestrictions
    }
  }
}

// Type for keys that can be overridden at node level
type NodeOverridableKey = keyof ProjectPreferences['nodes'][string]

// Default preferences
export const DEFAULT_PROJECT_PREFERENCES: ProjectPreferences = {
  global: {
    mode: "free-page",
    restrictions: {
      blocks: {
        b1: [null, null]
      }
    },
  },
  nodes: {}
}

// Helper functions to work with project preferences

/**
 * Get preference value for a specific node type
 * Priority: node-specific > global
 */
export function getProjectPreference<K extends NodeOverridableKey>(
  preferences: ProjectPreferences | undefined,
  nodeType: string,
  key: K
): ProjectPreferences['global'][K] {
  if (!preferences) {
    return DEFAULT_PROJECT_PREFERENCES.global[key]
  }

  // Check node-specific preference first
  if (preferences.nodes[nodeType]?.[key] !== undefined) {
    return preferences.nodes[nodeType][key] as ProjectPreferences['global'][K]
  }

  // Fallback to global preference
  if (preferences.global[key] !== undefined) {
    return preferences.global[key]
  }

  // Fallback to default
  return DEFAULT_PROJECT_PREFERENCES.global[key]
}

/**
 * Set a global preference (applies to all nodes unless overridden)
 */
export function setGlobalProjectPreference<K extends keyof ProjectPreferences['global']>(
  preferences: ProjectPreferences | undefined,
  key: K,
  value: ProjectPreferences['global'][K]
): ProjectPreferences {
  const prefs = preferences || { ...DEFAULT_PROJECT_PREFERENCES }
  
  return {
    ...prefs,
    global: {
      ...prefs.global,
      [key]: value
    }
  }
}

/**
 * Set a node-specific preference (overrides global)
 */
export function setNodeProjectPreference<K extends NodeOverridableKey>(
  preferences: ProjectPreferences | undefined,
  nodeType: string,
  key: K,
  value: ProjectPreferences['nodes'][string][K]
): ProjectPreferences {
  const prefs = preferences || { ...DEFAULT_PROJECT_PREFERENCES }
  
  return {
    ...prefs,
    nodes: {
      ...prefs.nodes,
      [nodeType]: {
        ...prefs.nodes[nodeType],
        [key]: value
      }
    }
  }
}

/**
 * Check if a node has a specific preference override
 */
export function hasNodeProjectPreference<K extends NodeOverridableKey>(
  preferences: ProjectPreferences | undefined,
  nodeType: string,
  key: K
): boolean {
  return !!(preferences?.nodes[nodeType]?.[key] !== undefined)
}

/**
 * Clear a node-specific preference (will fallback to global)
 */
export function clearNodeProjectPreference<K extends NodeOverridableKey>(
  preferences: ProjectPreferences | undefined,
  nodeType: string,
  key: K
): ProjectPreferences {
  const prefs = preferences || { ...DEFAULT_PROJECT_PREFERENCES }
  
  if (!prefs.nodes[nodeType]) {
    return prefs
  }

  const newNodePrefs = { ...prefs.nodes[nodeType] }
  delete newNodePrefs[key]

  return {
    ...prefs,
    nodes: {
      ...prefs.nodes,
      [nodeType]: newNodePrefs
    }
  }
}
