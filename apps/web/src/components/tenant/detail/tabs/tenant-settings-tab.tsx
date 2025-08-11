'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { 
  Settings, 
  Save, 
  Globe, 
  Shield, 
  Bell,
  Palette,
  Database,
  Key,
  AlertTriangle,
  CheckCircle,
  Loader2
} from 'lucide-react';
import type { Tenant } from '@/lib/api/generated/types.gen';

interface TenantSettingsTabProps {
  tenant: Tenant;
  isAdmin?: boolean;
}

interface TenantSettings {
  // General Settings
  name: string;
  description: string;
  slug: string;
  isActive: boolean;
  
  // Domain Settings
  customDomain?: string;
  subdomainEnabled: boolean;
  sslEnabled: boolean;
  
  // Security Settings
  requireTwoFactor: boolean;
  allowPublicSignup: boolean;
  passwordMinLength: number;
  sessionTimeout: number;
  
  // Notification Settings
  emailNotifications: boolean;
  slackWebhook?: string;
  discordWebhook?: string;
  
  // Feature Settings
  enabledFeatures: string[];
  apiRateLimit: number;
  maxUsers: number;
  maxStorage: number;
  
  // Branding
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  customCss?: string;
}

const availableFeatures = [
  { key: 'projects', name: 'Projects', description: 'Project management features' },
  { key: 'analytics', name: 'Analytics', description: 'Usage analytics and reporting' },
  { key: 'testing_lab', name: 'Testing Lab', description: 'Testing and feedback features' },
  { key: 'user_management', name: 'User Management', description: 'Advanced user management' },
  { key: 'api_access', name: 'API Access', description: 'REST API and webhooks' },
  { key: 'custom_branding', name: 'Custom Branding', description: 'White-label options' },
];

export function TenantSettingsTab({ tenant, isAdmin = false }: TenantSettingsTabProps) {
  const [settings, setSettings] = useState<TenantSettings | null>(null);
  const [originalSettings, setOriginalSettings] = useState<TenantSettings | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'success' | 'error'>('idle');

  useEffect(() => {
    const loadTenantSettings = async () => {
      setIsLoading(true);
      try {
        // Mock data - replace with actual API call
        const mockSettings: TenantSettings = {
          name: tenant.name || '',
          description: tenant.description || '',
          slug: tenant.slug || '',
          isActive: tenant.isActive ?? true,
          
          customDomain: 'mycompany.example.com',
          subdomainEnabled: true,
          sslEnabled: true,
          
          requireTwoFactor: false,
          allowPublicSignup: true,
          passwordMinLength: 8,
          sessionTimeout: 24, // hours
          
          emailNotifications: true,
          slackWebhook: '',
          discordWebhook: '',
          
          enabledFeatures: ['projects', 'analytics', 'user_management'],
          apiRateLimit: 1000,
          maxUsers: 50,
          maxStorage: 10, // GB
          
          logoUrl: '',
          primaryColor: '#3B82F6',
          secondaryColor: '#64748B',
          customCss: '',
        };
        
        setSettings(mockSettings);
        setOriginalSettings(mockSettings);
      } catch (error) {
        console.error('Failed to load tenant settings:', error);
      } finally {
        setIsLoading(false);
      }
    };

    if (tenant.id) {
      loadTenantSettings();
    }
  }, [tenant]);

  useEffect(() => {
    if (settings && originalSettings) {
      const changed = JSON.stringify(settings) !== JSON.stringify(originalSettings);
      setHasChanges(changed);
    }
  }, [settings, originalSettings]);

  const handleSave = async () => {
    if (!settings || !isAdmin) return;

    setIsSaving(true);
    setSaveStatus('idle');
    
    try {
      // Mock API call - replace with actual implementation
      console.log('Saving tenant settings:', settings);
      
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setOriginalSettings({ ...settings });
      setHasChanges(false);
      setSaveStatus('success');
    } catch (error) {
      console.error('Failed to save tenant settings:', error);
      setSaveStatus('error');
    } finally {
      setIsSaving(false);
    }
  };

  const handleReset = () => {
    if (originalSettings) {
      setSettings({ ...originalSettings });
      setHasChanges(false);
    }
  };

  const updateSetting = <K extends keyof TenantSettings>(
    key: K,
    value: TenantSettings[K]
  ) => {
    if (settings) {
      setSettings(prev => prev ? { ...prev, [key]: value } : null);
    }
  };

  const toggleFeature = (featureKey: string) => {
    if (settings) {
      const newFeatures = settings.enabledFeatures.includes(featureKey)
        ? settings.enabledFeatures.filter(f => f !== featureKey)
        : [...settings.enabledFeatures, featureKey];
      updateSetting('enabledFeatures', newFeatures);
    }
  };

  if (isLoading || !settings) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        <span className="ml-2 text-gray-600">Loading settings...</span>
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <div className="text-center py-8">
        <Shield className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">Access Restricted</h3>
        <p className="text-gray-500">You need administrator privileges to view tenant settings.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Save Banner */}
      {hasChanges && (
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription className="flex items-center justify-between">
            <span>You have unsaved changes to tenant settings.</span>
            <div className="flex items-center space-x-2">
              <Button variant="outline" size="sm" onClick={handleReset}>
                Reset
              </Button>
              <Button size="sm" onClick={handleSave} disabled={isSaving}>
                {isSaving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                Save Changes
              </Button>
            </div>
          </AlertDescription>
        </Alert>
      )}

      {/* Save Status */}
      {saveStatus === 'success' && (
        <Alert className="border-green-200 bg-green-50">
          <CheckCircle className="h-4 w-4 text-green-600" />
          <AlertDescription className="text-green-800">
            Settings saved successfully!
          </AlertDescription>
        </Alert>
      )}

      {saveStatus === 'error' && (
        <Alert className="border-red-200 bg-red-50">
          <AlertTriangle className="h-4 w-4 text-red-600" />
          <AlertDescription className="text-red-800">
            Failed to save settings. Please try again.
          </AlertDescription>
        </Alert>
      )}

      {/* General Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            General Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="tenant-name">Tenant Name</Label>
              <Input
                id="tenant-name"
                value={settings.name}
                onChange={(e) => updateSetting('name', e.target.value)}
                placeholder="Enter tenant name"
              />
            </div>
            <div>
              <Label htmlFor="tenant-slug">Slug</Label>
              <Input
                id="tenant-slug"
                value={settings.slug}
                onChange={(e) => updateSetting('slug', e.target.value)}
                placeholder="tenant-slug"
              />
            </div>
          </div>
          
          <div>
            <Label htmlFor="tenant-description">Description</Label>
            <Textarea
              id="tenant-description"
              value={settings.description}
              onChange={(e) => updateSetting('description', e.target.value)}
              placeholder="Describe your tenant"
              rows={3}
            />
          </div>
          
          <div className="flex items-center justify-between">
            <div>
              <Label htmlFor="tenant-active">Active Status</Label>
              <p className="text-sm text-gray-500">When disabled, users cannot access this tenant</p>
            </div>
            <Switch
              id="tenant-active"
              checked={settings.isActive}
              onCheckedChange={(checked) => updateSetting('isActive', checked)}
            />
          </div>
        </CardContent>
      </Card>

      {/* Domain Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Globe className="h-5 w-5" />
            Domain Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="custom-domain">Custom Domain</Label>
            <Input
              id="custom-domain"
              value={settings.customDomain || ''}
              onChange={(e) => updateSetting('customDomain', e.target.value)}
              placeholder="mycompany.example.com"
            />
          </div>
          
          <div className="flex items-center justify-between">
            <div>
              <Label>Subdomain Access</Label>
              <p className="text-sm text-gray-500">Allow access via {settings.slug}.yourdomain.com</p>
            </div>
            <Switch
              checked={settings.subdomainEnabled}
              onCheckedChange={(checked) => updateSetting('subdomainEnabled', checked)}
            />
          </div>
          
          <div className="flex items-center justify-between">
            <div>
              <Label>SSL Enabled</Label>
              <p className="text-sm text-gray-500">Force HTTPS connections</p>
            </div>
            <Switch
              checked={settings.sslEnabled}
              onCheckedChange={(checked) => updateSetting('sslEnabled', checked)}
            />
          </div>
        </CardContent>
      </Card>

      {/* Security Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Security Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <Label>Require Two-Factor Authentication</Label>
              <p className="text-sm text-gray-500">Force 2FA for all users</p>
            </div>
            <Switch
              checked={settings.requireTwoFactor}
              onCheckedChange={(checked) => updateSetting('requireTwoFactor', checked)}
            />
          </div>
          
          <div className="flex items-center justify-between">
            <div>
              <Label>Allow Public Signup</Label>
              <p className="text-sm text-gray-500">Users can register without invitation</p>
            </div>
            <Switch
              checked={settings.allowPublicSignup}
              onCheckedChange={(checked) => updateSetting('allowPublicSignup', checked)}
            />
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="password-min-length">Minimum Password Length</Label>
              <Input
                id="password-min-length"
                type="number"
                value={settings.passwordMinLength}
                onChange={(e) => updateSetting('passwordMinLength', parseInt(e.target.value))}
                min={6}
                max={128}
              />
            </div>
            <div>
              <Label htmlFor="session-timeout">Session Timeout (hours)</Label>
              <Input
                id="session-timeout"
                type="number"
                value={settings.sessionTimeout}
                onChange={(e) => updateSetting('sessionTimeout', parseInt(e.target.value))}
                min={1}
                max={168}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Feature Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Database className="h-5 w-5" />
            Features & Limits
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <Label className="text-base font-medium">Enabled Features</Label>
            <p className="text-sm text-gray-500 mb-3">Choose which features are available for this tenant</p>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {availableFeatures.map((feature) => (
                <div key={feature.key} className="flex items-center justify-between p-3 border rounded-lg">
                  <div>
                    <Label className="font-medium">{feature.name}</Label>
                    <p className="text-sm text-gray-500">{feature.description}</p>
                  </div>
                  <Switch
                    checked={settings.enabledFeatures.includes(feature.key)}
                    onCheckedChange={() => toggleFeature(feature.key)}
                  />
                </div>
              ))}
            </div>
          </div>
          
          <Separator />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <Label htmlFor="api-rate-limit">API Rate Limit (per hour)</Label>
              <Input
                id="api-rate-limit"
                type="number"
                value={settings.apiRateLimit}
                onChange={(e) => updateSetting('apiRateLimit', parseInt(e.target.value))}
                min={100}
                max={10000}
              />
            </div>
            <div>
              <Label htmlFor="max-users">Maximum Users</Label>
              <Input
                id="max-users"
                type="number"
                value={settings.maxUsers}
                onChange={(e) => updateSetting('maxUsers', parseInt(e.target.value))}
                min={1}
                max={1000}
              />
            </div>
            <div>
              <Label htmlFor="max-storage">Storage Limit (GB)</Label>
              <Input
                id="max-storage"
                type="number"
                value={settings.maxStorage}
                onChange={(e) => updateSetting('maxStorage', parseInt(e.target.value))}
                min={1}
                max={100}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Notification Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bell className="h-5 w-5" />
            Notifications
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <Label>Email Notifications</Label>
              <p className="text-sm text-gray-500">Send system notifications via email</p>
            </div>
            <Switch
              checked={settings.emailNotifications}
              onCheckedChange={(checked) => updateSetting('emailNotifications', checked)}
            />
          </div>
          
          <div>
            <Label htmlFor="slack-webhook">Slack Webhook URL</Label>
            <Input
              id="slack-webhook"
              value={settings.slackWebhook || ''}
              onChange={(e) => updateSetting('slackWebhook', e.target.value)}
              placeholder="https://hooks.slack.com/services/..."
            />
          </div>
          
          <div>
            <Label htmlFor="discord-webhook">Discord Webhook URL</Label>
            <Input
              id="discord-webhook"
              value={settings.discordWebhook || ''}
              onChange={(e) => updateSetting('discordWebhook', e.target.value)}
              placeholder="https://discord.com/api/webhooks/..."
            />
          </div>
        </CardContent>
      </Card>

      {/* Action Buttons */}
      <div className="flex items-center justify-between pt-6 border-t">
        <div className="text-sm text-gray-500">
          Changes will be applied immediately after saving.
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" onClick={handleReset} disabled={!hasChanges}>
            Reset Changes
          </Button>
          <Button onClick={handleSave} disabled={!hasChanges || isSaving}>
            {isSaving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            <Save className="h-4 w-4 mr-2" />
            Save Settings
          </Button>
        </div>
      </div>
    </div>
  );
}
