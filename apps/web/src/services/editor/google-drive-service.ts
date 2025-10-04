"use client"

import { HashManager } from "../../lib/sync/editor/hash-manager"

interface GoogleDriveFile {
  id: string
  name: string
  mimeType: string
  createdTime: string
  modifiedTime: string
  size?: string
}

interface GoogleDriveProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash: string
  driveFileId?: string
  storageType: string
}

export class GoogleDriveService {
  private static instance: GoogleDriveService
  private folderId: string | null = null
  private accessToken: string | null = null

  private constructor() {
    // Load saved folder ID
    if (typeof window !== 'undefined') {
      this.folderId = localStorage.getItem('gglexical_google_drive_folder')
    }
  }

  static getInstance(): GoogleDriveService {
    if (!GoogleDriveService.instance) {
      GoogleDriveService.instance = new GoogleDriveService()
    }
    return GoogleDriveService.instance
  }

  // Set authentication token and folder
  setAuth(accessToken: string, folderId: string) {
    this.accessToken = accessToken
    this.folderId = folderId
  }

  // Check if service is ready
  isReady(): boolean {
    const hasToken = !!this.accessToken
    const hasFolder = !!this.folderId
    const hasGapi = !!(window.gapi?.client?.drive)
    
    console.log("GoogleDriveService.isReady() check:", {
      hasToken,
      hasFolder,
      hasGapi,
      accessToken: this.accessToken ? `${this.accessToken.substring(0, 10)}...` : null,
      folderId: this.folderId
    })
    
    return hasToken && hasFolder && hasGapi
  }

  // Set authentication token for GAPI client
  private setAuthToken() {
    if (this.accessToken && window.gapi?.client) {
      window.gapi.client.setToken({ access_token: this.accessToken })
    }
  }

  // Upload file content as JSON
  private async uploadJsonFile(
    fileName: string, 
    content: object, 
    fileId?: string
  ): Promise<string | null> {
    if (!this.isReady()) {
      throw new Error('Google Drive service not ready')
    }

    const jsonContent = JSON.stringify(content, null, 2)
    const metadata = {
      name: fileName,
      parents: fileId ? undefined : [this.folderId!],
      mimeType: 'application/json',
    }

    try {
      const form = new FormData()
      form.append('metadata', new Blob([JSON.stringify(metadata)], { type: 'application/json' }))
      form.append('file', new Blob([jsonContent], { type: 'application/json' }))

      const url = fileId 
        ? `https://www.googleapis.com/upload/drive/v3/files/${fileId}?uploadType=multipart`
        : 'https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart'

      const method = fileId ? 'PATCH' : 'POST'

      const response = await fetch(url, {
        method,
        headers: {
          'Authorization': `Bearer ${this.accessToken}`,
        },
        body: form,
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const result = await response.json()
      return result.id
    } catch (error) {
      console.error('Failed to upload file:', error)
      throw new Error('Falha ao salvar arquivo no Google Drive')
    }
  }

  // Download file content
  private async downloadJsonFile(fileId: string): Promise<object | null> {
    if (!this.isReady()) {
      throw new Error('Google Drive service not ready')
    }

    try {
      // Set auth token before API call
      this.setAuthToken()
      
      const response = await window.gapi.client.drive.files.get({
        fileId: fileId,
        alt: 'media',
      })

      return JSON.parse(response.body)
    } catch (error) {
      console.error('Failed to download file:', error)
      return null
    }
  }

  // List files in the GGLexical folder
  async listProjects(): Promise<GoogleDriveProjectData[]> {
    if (!this.isReady()) {
      return []
    }

    try {
      // Set auth token before API call
      this.setAuthToken()
      
      const response = await window.gapi.client.drive.files.list({
        q: `'${this.folderId}' in parents and name contains '.gglexical.json' and trashed=false`,
        fields: 'files(id, name, createdTime, modifiedTime, size)',
        orderBy: 'modifiedTime desc',
      })

      const files = response.result.files || []
      const projects: GoogleDriveProjectData[] = []

      // Download and parse each project file
      for (const file of files) {
        try {
          const content = await this.downloadJsonFile(file.id!)
          if (content && this.isValidProjectData(content)) {
            const projectData = content as any
            projects.push({
              ...projectData,
              driveFileId: file.id!,
              size: parseInt(file.size || '0'),
              updatedAt: file.modifiedTime!,
            })
          }
        } catch (error) {
          console.error(`Failed to load project ${file.name}:`, error)
        }
      }

      return projects
    } catch (error) {
      console.error('Failed to list projects:', error)
      return []
    }
  }

  // Save project to Google Drive
  async saveProject(
    id: string, 
    name: string, 
    data: string, 
    tags: string[]
  ): Promise<void> {
    if (!this.isReady()) {
      throw new Error('Google Drive service not ready')
    }

    const hash = await HashManager.generateHash(data)
    
    const projectData: Omit<GoogleDriveProjectData, 'driveFileId' | 'size'> = {
      id,
      name,
      data,
      tags,
      hash,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      storageType: "google-drive",
    }

    // Check if project already exists
    const existingProjects = await this.listProjects()
    const existingProject = existingProjects.find(p => p.id === id)
    
    const fileName = `${name}.gglexical.json`
    const fileId = existingProject?.driveFileId

    try {
      const driveFileId = await this.uploadJsonFile(fileName, projectData, fileId)
      if (!driveFileId) {
        throw new Error('Failed to get file ID after upload')
      }
    } catch (error) {
      console.error('Save project failed:', error)
      throw error
    }
  }

  // Update existing project
  async updateProject(
    projectId: string,
    name: string,
    data: string,
    tags: string[]
  ): Promise<void> {
    const existingProjects = await this.listProjects()
    const project = existingProjects.find(p => p.id === projectId)
    
    if (!project) {
      throw new Error('Project not found')
    }

    const updatedProjectData = {
      ...project,
      name,
      data,
      tags,
      updatedAt: new Date().toISOString(),
    }

    const fileName = `${name}.gglexical.json`
    
    try {
      await this.uploadJsonFile(fileName, updatedProjectData, project.driveFileId)
    } catch (error) {
      console.error('Update project failed:', error)
      throw error
    }
  }

  // Delete project from Google Drive
  async deleteProject(projectId: string): Promise<void> {
    if (!this.isReady()) {
      throw new Error('Google Drive service not ready')
    }

    const existingProjects = await this.listProjects()
    const project = existingProjects.find(p => p.id === projectId)
    
    if (!project) {
      throw new Error('Project not found')
    }

    try {
      // Set auth token before API call
      this.setAuthToken()
      
      await window.gapi.client.drive.files.delete({
        fileId: project.driveFileId!,
      })
    } catch (error) {
      console.error('Delete project failed:', error)
      throw new Error('Falha ao excluir projeto do Google Drive')
    }
  }

  // Get project by ID
  async getProject(projectId: string): Promise<GoogleDriveProjectData | null> {
    const projects = await this.listProjects()
    return projects.find(p => p.id === projectId) || null
  }

  // Validate project data structure
  private isValidProjectData(data: any): boolean {
    return (
      data &&
      typeof data.id === 'string' &&
      typeof data.name === 'string' &&
      typeof data.data === 'string' &&
      Array.isArray(data.tags) &&
      typeof data.createdAt === 'string' &&
      typeof data.updatedAt === 'string'
    )
  }

  // Search projects by name or tags
  async searchProjects(query: string): Promise<GoogleDriveProjectData[]> {
    const allProjects = await this.listProjects()
    
    if (!query.trim()) {
      return allProjects
    }

    const searchTerm = query.toLowerCase()
    
    return allProjects.filter(project => 
      project.name.toLowerCase().includes(searchTerm) ||
      project.tags.some(tag => tag.toLowerCase().includes(searchTerm))
    )
  }

  // Get storage statistics
  async getStorageStats(): Promise<{ totalSize: number; projectCount: number }> {
    const projects = await this.listProjects()
    
    return {
      totalSize: projects.reduce((total, project) => total + (project.size || 0), 0),
      projectCount: projects.length,
    }
  }

  // Cleanup - reset authentication
  cleanup() {
    this.accessToken = null
    this.folderId = null
  }
}
