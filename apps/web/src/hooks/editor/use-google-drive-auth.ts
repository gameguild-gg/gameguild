"use client"

import { useState, useEffect, useCallback } from "react"
import { toast } from "sonner"
import { GoogleDriveSecurity } from "@/utils/editor/google-drive-security"

interface GoogleDriveAuthState {
  isAuthenticated: boolean
  isLoading: boolean
  selectedFolder: string | null
  folderName: string | null
  accessToken: string | null
  error: string | null
}

interface GoogleDriveFolder {
  id: string
  name: string
  createdTime: string
}

export function useGoogleDriveAuth() {
  const [authState, setAuthState] = useState<GoogleDriveAuthState>({
    isAuthenticated: false,
    isLoading: false,
    selectedFolder: null,
    folderName: null,
    accessToken: null,
    error: null,
  })

  // Google Drive API configuration
  const CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_DRIVE_CLIENT_ID
  const API_KEY = process.env.NEXT_PUBLIC_GOOGLE_DRIVE_API_KEY
  const DISCOVERY_DOC = 'https://www.googleapis.com/discovery/v1/apis/drive/v3/rest'
  
  // Minimal required scopes - only file access within app-created folders
  const SCOPES = [
    'https://www.googleapis.com/auth/drive.file', // Access only to files created by this app
  ]

  // Load Google API client
  const loadGoogleAPI = useCallback(async () => {
    if (typeof window === 'undefined') return false

    try {
      // Load Google API script if not already loaded
      if (!window.gapi) {
        await new Promise<void>((resolve, reject) => {
          const script = document.createElement('script')
          script.src = 'https://apis.google.com/js/api.js'
          script.onload = () => resolve()
          script.onerror = () => reject(new Error('Failed to load Google API'))
          document.head.appendChild(script)
        })
      }

      // Initialize GAPI
      await new Promise<void>((resolve) => {
        window.gapi.load('client:auth2', resolve)
      })

      // Initialize client
      await window.gapi.client.init({
        apiKey: API_KEY,
        clientId: CLIENT_ID,
        discoveryDocs: [DISCOVERY_DOC],
        scope: SCOPES.join(' ')
      })

      return true
    } catch (error) {
      console.error('Failed to load Google API:', error)
      setAuthState(prev => ({ 
        ...prev, 
        error: 'Falha ao carregar API do Google Drive',
        isLoading: false 
      }))
      return false
    }
  }, [API_KEY, CLIENT_ID])

  // Check authentication status
  const checkAuthStatus = useCallback(async () => {
    if (!window.gapi?.auth2) return

    const authInstance = window.gapi.auth2.getAuthInstance()
    const isSignedIn = authInstance.isSignedIn.get()
    
    if (isSignedIn) {
      const user = authInstance.currentUser.get()
      const authResponse = user.getAuthResponse()
      
      // Retrieve saved folder info from localStorage
      const savedFolder = localStorage.getItem('gglexical_google_drive_folder')
      const savedFolderName = localStorage.getItem('gglexical_google_drive_folder_name')
      
      setAuthState(prev => ({
        ...prev,
        isAuthenticated: true,
        accessToken: authResponse.access_token,
        selectedFolder: savedFolder,
        folderName: savedFolderName,
        error: null,
      }))
    }
  }, [])

  // Initialize Google Drive API
  useEffect(() => {
    const initialize = async () => {
      // Validate environment first
      const envValidation = GoogleDriveSecurity.validateEnvironment()
      if (!envValidation.isValid) {
        console.warn('Google Drive API not properly configured:', envValidation.errors)
        return
      }

      // Clean up expired data
      GoogleDriveSecurity.cleanupExpiredData()

      setAuthState(prev => ({ ...prev, isLoading: true }))
      
      const loaded = await loadGoogleAPI()
      if (loaded) {
        await checkAuthStatus()
      }
      
      setAuthState(prev => ({ ...prev, isLoading: false }))
    }

    initialize()
  }, [loadGoogleAPI, checkAuthStatus, CLIENT_ID, API_KEY])

  // Authenticate with Google
  const authenticate = useCallback(async (): Promise<boolean> => {
    if (!window.gapi?.auth2) {
      toast.error('Google API não carregada')
      return false
    }

    try {
      setAuthState(prev => ({ ...prev, isLoading: true, error: null }))
      
      const authInstance = window.gapi.auth2.getAuthInstance()
      const user = await authInstance.signIn({
        prompt: 'select_account'
      })
      
      const authResponse = user.getAuthResponse()
      
      // Validate token before proceeding
      const tokenValidation = await GoogleDriveSecurity.validateToken(authResponse.access_token)
      if (!tokenValidation.isValid) {
        throw new Error(tokenValidation.error || 'Invalid token')
      }
      
      // Store authentication time for cleanup
      localStorage.setItem('gglexical_google_drive_auth_time', Date.now().toString())
      
      setAuthState(prev => ({
        ...prev,
        isAuthenticated: true,
        accessToken: authResponse.access_token,
        isLoading: false,
        error: null,
      }))

      GoogleDriveSecurity.logSecurityEvent('google_drive_auth_success')
      toast.success('Autenticado com Google Drive')
      return true
    } catch (error: any) {
      console.error('Google authentication failed:', error)
      
      let errorMessage = 'Falha na autenticação'
      if (error.error === 'popup_closed_by_user') {
        errorMessage = 'Autenticação cancelada pelo usuário'
      } else if (error.error === 'access_denied') {
        errorMessage = 'Acesso negado pelo usuário'
      }
      
      setAuthState(prev => ({ 
        ...prev, 
        error: errorMessage,
        isLoading: false 
      }))
      
      toast.error(errorMessage)
      return false
    }
  }, [])

  // Create or find GGLexical folder
  const createOrFindFolder = useCallback(async (folderName: string): Promise<string | null> => {
    if (!authState.accessToken) return null

    // Sanitize folder name
    const sanitizedName = GoogleDriveSecurity.sanitizeFileName(folderName.replace('.gglexical.json', ''))
    const finalFolderName = sanitizedName.replace('.gglexical.json', '') // Remove extension for folder name

    try {
      return await GoogleDriveSecurity.throttleApiCall(async () => {
        // First, check if folder already exists
        const searchResponse = await window.gapi.client.drive.files.list({
          q: `name='${finalFolderName}' and mimeType='application/vnd.google-apps.folder' and trashed=false`,
          fields: 'files(id, name, createdTime)',
        })

        if (searchResponse.result.files && searchResponse.result.files.length > 0) {
          const folder = searchResponse.result.files[0]
          
          // Save folder info
          localStorage.setItem('gglexical_google_drive_folder', folder.id!)
          localStorage.setItem('gglexical_google_drive_folder_name', folder.name!)
          
          setAuthState(prev => ({
            ...prev,
            selectedFolder: folder.id!,
            folderName: folder.name!,
          }))
          
          return folder.id!
        }

        // Create new folder if not found
        const createResponse = await window.gapi.client.drive.files.create({
          resource: {
            name: finalFolderName,
            mimeType: 'application/vnd.google-apps.folder',
          },
          fields: 'id, name',
        })

        const folderId = createResponse.result.id!
        
        // Save folder info
        localStorage.setItem('gglexical_google_drive_folder', folderId)
        localStorage.setItem('gglexical_google_drive_folder_name', finalFolderName)
        
        setAuthState(prev => ({
          ...prev,
          selectedFolder: folderId,
          folderName: finalFolderName,
        }))

        toast.success(`Pasta "${finalFolderName}" criada no Google Drive`)
        return folderId
      })
    } catch (error) {
      console.error('Failed to create/find folder:', error)
      GoogleDriveSecurity.logSecurityEvent('folder_creation_failed', { error: String(error) })
      toast.error('Falha ao criar pasta no Google Drive')
      return null
    }
  }, [authState.accessToken])

  // Sign out
  const signOut = useCallback(async () => {
    if (!window.gapi?.auth2) return

    try {
      const authInstance = window.gapi.auth2.getAuthInstance()
      await authInstance.signOut()
      
      // Clear stored data
      localStorage.removeItem('gglexical_google_drive_folder')
      localStorage.removeItem('gglexical_google_drive_folder_name')
      
      setAuthState({
        isAuthenticated: false,
        isLoading: false,
        selectedFolder: null,
        folderName: null,
        accessToken: null,
        error: null,
      })

      toast.success('Desconectado do Google Drive')
    } catch (error) {
      console.error('Sign out failed:', error)
      toast.error('Falha ao desconectar')
    }
  }, [])

  // Get available folders (for selection)
  const getAvailableFolders = useCallback(async (): Promise<GoogleDriveFolder[]> => {
    if (!authState.accessToken) return []

    try {
      const response = await window.gapi.client.drive.files.list({
        q: "mimeType='application/vnd.google-apps.folder' and trashed=false",
        fields: 'files(id, name, createdTime)',
        orderBy: 'name',
      })

      return response.result.files || []
    } catch (error) {
      console.error('Failed to get folders:', error)
      return []
    }
  }, [authState.accessToken])

  return {
    ...authState,
    authenticate,
    signOut,
    createOrFindFolder,
    getAvailableFolders,
    hasValidSetup: authState.isAuthenticated && authState.selectedFolder,
  }
}

// Extend window type for Google API
declare global {
  interface Window {
    gapi: any
  }
}
