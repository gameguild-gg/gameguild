// Temporary type definitions until generated types are available
export interface CreateTestingLabSettingsDto {
  labName: string;
  description?: string;
  timezone: string;
  defaultSessionDuration: number;
  allowPublicSignups: boolean;
  requireApproval: boolean;
  enableNotifications: boolean;
  maxSimultaneousSessions: number;
}

export interface UpdateTestingLabSettingsDto {
  labName?: string;
  description?: string;
  timezone?: string;
  defaultSessionDuration?: number;
  allowPublicSignups?: boolean;
  requireApproval?: boolean;
  enableNotifications?: boolean;
  maxSimultaneousSessions?: number;
}

export interface TestingLabSettingsDto {
  id: string;
  labName: string;
  description?: string;
  timezone: string;
  defaultSessionDuration: number;
  allowPublicSignups: boolean;
  requireApproval: boolean;
  enableNotifications: boolean;
  maxSimultaneousSessions: number;
  tenantId?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * API client for TestingLabSettings operations
 */
export class TestingLabSettingsAPI {
  /**
   * Get testing lab settings for the current tenant
   */
  static async getTestingLabSettings(accessToken: string): Promise<TestingLabSettingsDto> {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const response = await fetch(`${apiUrl}/api/testing-lab/settings`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to get settings: ${response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Create or update testing lab settings for the current tenant
   */
  static async createOrUpdateTestingLabSettings(dto: CreateTestingLabSettingsDto, accessToken: string): Promise<TestingLabSettingsDto> {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const response = await fetch(`${apiUrl}/api/testing-lab/settings`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
      body: JSON.stringify(dto),
    });

    if (!response.ok) {
      throw new Error(`Failed to create or update settings: ${response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Update testing lab settings for the current tenant (partial update)
   */
  static async updateTestingLabSettings(dto: UpdateTestingLabSettingsDto, accessToken: string): Promise<TestingLabSettingsDto> {
    // Use internal API URL for server-to-server communication
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const fullUrl = `${apiUrl}/api/testing-lab/settings`;
    
    console.log('Environment API_URL:', process.env.API_URL);
    console.log('Environment NEXT_PUBLIC_API_URL:', process.env.NEXT_PUBLIC_API_URL);
    console.log('Final API URL:', apiUrl);
    console.log('Full URL:', fullUrl);
    console.log('DTO:', dto);
    console.log('Access Token (first 20 chars):', accessToken?.substring(0, 20) + '...');
    
    const response = await fetch(fullUrl, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
      body: JSON.stringify(dto),
    });

    if (!response.ok) {
      console.error('Response not OK:', response.status, response.statusText);
      
      // Try to get the error details from the response body
      let errorDetails = 'No additional details available';
      try {
        const errorBody = await response.text();
        console.error('Error response body:', errorBody);
        errorDetails = errorBody;
      } catch (e) {
        console.error('Could not read error response body:', e);
      }
      
      throw new Error(`Failed to update settings: ${response.statusText} - ${errorDetails}`);
    }

    return await response.json();
  }

    /**
   * Reset testing lab settings to default values for the current tenant
   */
  static async resetTestingLabSettings(accessToken: string): Promise<TestingLabSettingsDto> {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const response = await fetch(`${apiUrl}/api/testing-lab/settings/reset`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to reset settings: ${response.statusText}`);
    }

    return await response.json();
  }

  /**
   * Check if testing lab settings exist for the current tenant
   */
  static async testingLabSettingsExist(accessToken: string): Promise<boolean> {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const response = await fetch(`${apiUrl}/api/testing-lab/settings/exists`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to check settings existence: ${response.statusText}`);
    }

    return await response.json();
  }
}
