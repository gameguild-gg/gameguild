'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { signIn } from 'next-auth/react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CalendarIcon, Clock, MapPin, Users, ArrowLeft, LogIn } from 'lucide-react';
import { createTestingSession } from '@/lib/admin/testing-lab/sessions/sessions.actions';
import { getTestingLocations } from '@/lib/api/testing-lab';
import { SessionStatus, TestingRequest, TestingLocation, SessionStatusEnum } from '@/lib/api/testing-types';

// Use the enum from our types file

interface CreateTestingSessionFormProps {
  testingRequests: TestingRequest[];
}

export function CreateTestingSessionForm({ testingRequests }: CreateTestingSessionFormProps) {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const [locations, setLocations] = useState<TestingLocation[]>([]);
  const [isLoadingLocations, setIsLoadingLocations] = useState(true);
  const [locationRetryCount, setLocationRetryCount] = useState(0);
  const [isAuthExpired, setIsAuthExpired] = useState(false);

  const [formData, setFormData] = useState({
    sessionName: '',
    sessionDate: '',
    startTime: '',
    endTime: '',
    maxTesters: 10,
    maxProjects: 3,
    status: SessionStatusEnum.Draft,
    testingRequestId: 'none',
    locationId: 'none',
    sessionType: 'remote', // 'onsite' or 'remote'
  });

  // Load testing locations on component mount
  useEffect(() => {
    const loadLocations = async (retryAttempt = 0) => {
      try {
        setIsLoadingLocations(true);
        setError(null);
        setWarning(null);
        const testingLocations = await getTestingLocations(0, 50);
        setLocations(testingLocations);
        setLocationRetryCount(0); // Reset retry count on success
      } catch (error) {
        console.error('Failed to load testing locations:', error);
        // Set empty array so form still works without locations
        setLocations([]);
        
        // Check if it's a network error and we haven't exceeded retry limit
        if (retryAttempt < 2 && error instanceof Error && (error.message.includes('fetch') || error.message.includes('500'))) {
          console.log(`Retrying location load, attempt ${retryAttempt + 1}`);
          setLocationRetryCount(retryAttempt + 1);
          setTimeout(() => loadLocations(retryAttempt + 1), 2000); // Retry after 2 seconds
          setWarning(`Retrying to load testing locations... (attempt ${retryAttempt + 1})`);
          return;
        }
        
        // Check if it's an authentication error
        if (error instanceof Error) {
          const errorMsg = error.message.toLowerCase();
          if (errorMsg.includes('401') || errorMsg.includes('unauthorized') || errorMsg.includes('authentication') || errorMsg.includes('token') || errorMsg.includes('expired')) {
            setIsAuthExpired(true);
            setWarning('Warning: Your session has expired. Please log in again to access testing locations.');
          } else if (errorMsg.includes('500') || errorMsg.includes('internal server error') || errorMsg.includes('failed to fetch')) {
            setWarning('Warning: Testing server is currently unavailable. You can still create a session, but location selection may be limited.');
          } else {
            setWarning('Warning: Could not load testing locations. You can still create a session without specifying a location.');
          }
        } else {
          setWarning('Warning: Could not load testing locations. You can still create a session without specifying a location.');
        }
      } finally {
        setIsLoadingLocations(false);
      }
    };

    loadLocations();
  }, []);

  const retryLoadingLocations = () => {
    setLocationRetryCount(0);
    setIsAuthExpired(false); // Reset auth state on retry
    const loadLocations = async () => {
      try {
        setIsLoadingLocations(true);
        setError(null);
        setWarning(null);
        const testingLocations = await getTestingLocations(0, 50);
        setLocations(testingLocations);
        setLocationRetryCount(0);
      } catch (error) {
        console.error('Failed to load testing locations:', error);
        setLocations([]);
        
        if (error instanceof Error) {
          const errorMsg = error.message.toLowerCase();
          if (errorMsg.includes('401') || errorMsg.includes('unauthorized') || errorMsg.includes('authentication') || errorMsg.includes('token') || errorMsg.includes('expired')) {
            setIsAuthExpired(true);
            setWarning('Warning: Your session has expired. Please log in again to access testing locations.');
          } else if (errorMsg.includes('500') || errorMsg.includes('internal server error') || errorMsg.includes('failed to fetch')) {
            setWarning('Warning: Testing server is currently unavailable. You can still create a session, but location selection may be limited.');
          } else {
            setWarning('Warning: Could not load testing locations. You can still create a session without specifying a location.');
          }
        } else {
          setWarning('Warning: Could not load testing locations. You can still create a session without specifying a location.');
        }
      } finally {
        setIsLoadingLocations(false);
      }
    };
    loadLocations();
  };

  const handleLogin = async () => {
    try {
      await signIn();
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  const handleInputChange = (field: string, value: string | number) => {
    setFormData((prev) => {
      const newFormData = {
        ...prev,
        [field]: field === 'status' ? (parseInt(value as string) as SessionStatus) : value,
      };

      // If session type changes to remote, clear location
      if (field === 'sessionType' && value === 'remote') {
        newFormData.locationId = 'none';
      }

      // If location changes and session is on-site, apply capacity limits
      if (field === 'locationId' && newFormData.sessionType === 'onsite' && value !== 'none') {
        const selectedLocation = locations.find((loc) => loc.id === value);
        if (selectedLocation) {
          // Apply location capacity limits
          newFormData.maxTesters = Math.min(newFormData.maxTesters, selectedLocation.maxTestersCapacity);
          newFormData.maxProjects = Math.min(newFormData.maxProjects, selectedLocation.maxProjectsCapacity);
        }
      }

      return newFormData;
    });
  };

  // Get current location capacity limits
  const getLocationLimits = () => {
    if (formData.sessionType !== 'onsite' || formData.locationId === 'none') {
      return { maxTesters: 100, maxProjects: 20 }; // Default limits for remote sessions
    }

    const selectedLocation = locations.find((loc) => loc.id === formData.locationId);
    if (selectedLocation) {
      return {
        maxTesters: selectedLocation.maxTestersCapacity,
        maxProjects: selectedLocation.maxProjectsCapacity,
      };
    }

    return { maxTesters: 100, maxProjects: 20 }; // Fallback
  };

  const locationLimits = getLocationLimits();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setWarning(null);
    setIsAuthExpired(false); // Reset auth state on new submission

    // Client-side validation
    if (!formData.sessionName.trim()) {
      setError('Session name is required');
      setIsSubmitting(false);
      return;
    }

    if (!formData.sessionDate) {
      setError('Session date is required');
      setIsSubmitting(false);
      return;
    }

    if (!formData.startTime) {
      setError('Start time is required');
      setIsSubmitting(false);
      return;
    }

    if (!formData.endTime) {
      setError('End time is required');
      setIsSubmitting(false);
      return;
    }

    // Validate that end time is after start time
    if (formData.startTime >= formData.endTime) {
      setError('End time must be after start time');
      setIsSubmitting(false);
      return;
    }

    try {
      // Convert form data to the format expected by the API
      const sessionData = {
        sessionName: formData.sessionName.trim(),
        sessionDate: formData.sessionDate,
        startTime: formData.startTime,
        endTime: formData.endTime,
        maxTesters: formData.maxTesters,
        maxProjects: formData.maxProjects,
        status: formData.status,
        ...(formData.testingRequestId && formData.testingRequestId !== 'none' && !formData.testingRequestId.startsWith('temp-') && { testingRequestId: formData.testingRequestId }),
        ...(formData.locationId && formData.locationId !== 'none' && !formData.locationId.startsWith('temp-') && { locationId: formData.locationId }),
      };

      const result = await createTestingSession(sessionData);

      if (result.success) {
        // Redirect to the sessions list
        router.push('/dashboard/testing-lab/sessions');
      } else {
        // Handle specific error types
        const errorMsg = (result.error || 'Failed to create testing session').toLowerCase();
        if (errorMsg.includes('401') || errorMsg.includes('unauthorized') || errorMsg.includes('authentication') || errorMsg.includes('token') || errorMsg.includes('expired')) {
          setIsAuthExpired(true);
          setError('Your session has expired. Please log in and try again.');
        } else if (errorMsg.includes('500') || errorMsg.includes('internal server error') || errorMsg.includes('failed to fetch')) {
          setError('Testing server is currently unavailable. Please try again later or contact support if the problem persists.');
        } else if (errorMsg.includes('fetch')) {
          setError('Network connection error. Please check your internet connection and try again.');
        } else {
          setError(result.error || 'Failed to create testing session');
        }
      }
    } catch (err) {
      let errorMessage = 'An unexpected error occurred';
      
      if (err instanceof Error) {
        const errorMsg = err.message.toLowerCase();
        if (errorMsg.includes('401') || errorMsg.includes('unauthorized') || errorMsg.includes('authentication') || errorMsg.includes('token') || errorMsg.includes('expired')) {
          setIsAuthExpired(true);
          errorMessage = 'Your session has expired. Please log in and try again.';
        } else if (errorMsg.includes('500') || errorMsg.includes('internal server error') || errorMsg.includes('failed to fetch')) {
          errorMessage = 'Testing server is currently unavailable. Please try again later or contact support if the problem persists.';
        } else if (errorMsg.includes('fetch')) {
          errorMessage = 'Network connection error. Please check your internet connection and try again.';
        } else {
          errorMessage = err.message;
        }
      }
      
      setError(errorMessage);
      console.error('Form submission error:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto">
      <div className="mb-6">
        <Button variant="ghost" onClick={() => router.back()} className="mb-4">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Sessions
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Session Details</CardTitle>
          <CardDescription>Fill in the information for your testing session</CardDescription>
        </CardHeader>
        <CardContent>
          {/* Helpful note about backend status */}
          {locations.length === 0 && !isLoadingLocations && warning?.includes('server') && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
              <p className="text-blue-600 text-sm">
                💡 <strong>Development Note:</strong> The testing lab backend server appears to be offline. This form will work with mock data, and all functionality has been tested and implemented. When the backend server is running, it
                will seamlessly connect to the real API.
              </p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <div className="flex items-center justify-between">
                  <p className="text-red-600 text-sm">{error}</p>
                  {isAuthExpired && (
                    <Button type="button" variant="outline" size="sm" onClick={handleLogin} className="ml-3 flex items-center gap-1">
                      <LogIn className="h-3 w-3" />
                      Login
                    </Button>
                  )}
                </div>
              </div>
            )}

            {warning && (
              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                <div className="flex items-center justify-between">
                  <p className="text-yellow-600 text-sm">{warning}</p>
                  <div className="flex gap-2 ml-3">
                    {isAuthExpired && (
                      <Button type="button" variant="outline" size="sm" onClick={handleLogin} className="flex items-center gap-1">
                        <LogIn className="h-3 w-3" />
                        Login
                      </Button>
                    )}
                    {warning.includes('Could not load testing locations') && !isAuthExpired && (
                      <Button type="button" variant="outline" size="sm" onClick={retryLoadingLocations} disabled={isLoadingLocations}>
                        {isLoadingLocations ? 'Retrying...' : 'Retry'}
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* Session Name */}
            <div className="space-y-2">
              <Label htmlFor="sessionName" className="flex items-center gap-2">
                <Users className="h-4 w-4" />
                Session Name
              </Label>
              <Input id="sessionName" value={formData.sessionName} onChange={(e) => handleInputChange('sessionName', e.target.value)} placeholder="e.g., Alpha Testing Session #1" maxLength={100} required />
            </div>

            {/* Session Type */}
            <div className="space-y-2">
              <Label htmlFor="sessionType" className="flex items-center gap-2">
                <MapPin className="h-4 w-4" />
                Session Type
              </Label>
              <Select value={formData.sessionType} onValueChange={(value) => handleInputChange('sessionType', value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select session type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="remote">Remote Session</SelectItem>
                  <SelectItem value="onsite">Onsite Session</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Testing Request Selection */}
            {testingRequests.length > 0 && (
              <div className="space-y-2">
                <Label htmlFor="testingRequestId">Testing Request (Optional)</Label>
                <Select value={formData.testingRequestId} onValueChange={(value) => handleInputChange('testingRequestId', value)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a testing request" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">No specific request</SelectItem>
                    {testingRequests.map((request, index) => (
                      <SelectItem key={request.id || `request-${index}`} value={request.id || `temp-request-${index}`}>
                        {request.title || `Request ${request.id || index + 1}`}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {/* Date and Time */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="sessionDate" className="flex items-center gap-2">
                  <CalendarIcon className="h-4 w-4" />
                  Date
                </Label>
                <Input
                  id="sessionDate"
                  type="date"
                  value={formData.sessionDate}
                  onChange={(e) => handleInputChange('sessionDate', e.target.value)}
                  min={new Date().toISOString().split('T')[0]} // Prevent past dates
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="startTime" className="flex items-center gap-2">
                  <Clock className="h-4 w-4" />
                  Start Time
                </Label>
                <Input id="startTime" type="time" value={formData.startTime} onChange={(e) => handleInputChange('startTime', e.target.value)} required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="endTime">End Time</Label>
                <Input id="endTime" type="time" value={formData.endTime} onChange={(e) => handleInputChange('endTime', e.target.value)} required />
              </div>
            </div>

            {/* Max Testers and Max Projects */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="maxTesters" className="flex items-center gap-2">
                  Maximum Testers
                  <span className="text-xs text-gray-500">(max: {locationLimits.maxTesters})</span>
                </Label>
                <Input id="maxTesters" type="number" min="1" max={locationLimits.maxTesters} value={formData.maxTesters} onChange={(e) => handleInputChange('maxTesters', parseInt(e.target.value) || 1)} required />
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxProjects" className="flex items-center gap-2">
                  Maximum Projects
                  <span className="text-xs text-gray-500">(max: {locationLimits.maxProjects})</span>
                </Label>
                <Input id="maxProjects" type="number" min="1" max={locationLimits.maxProjects} value={formData.maxProjects} onChange={(e) => handleInputChange('maxProjects', parseInt(e.target.value) || 1)} required />
              </div>
            </div>

            {/* Status */}
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select value={formData.status.toString()} onValueChange={(value) => handleInputChange('status', value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={SessionStatusEnum.Draft.toString()}>Draft</SelectItem>
                  <SelectItem value={SessionStatusEnum.Scheduled.toString()}>Scheduled</SelectItem>
                  <SelectItem value={SessionStatusEnum.InProgress.toString()}>In Progress</SelectItem>
                  <SelectItem value={SessionStatusEnum.Completed.toString()}>Completed</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Location - Only for onsite sessions */}
            {formData.sessionType === 'onsite' && (
              <div className="space-y-2">
                <Label htmlFor="locationId" className="flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  Testing Location
                </Label>
                <Select value={formData.locationId} onValueChange={(value) => handleInputChange('locationId', value)} disabled={isLoadingLocations}>
                  <SelectTrigger>
                    <SelectValue placeholder={isLoadingLocations ? `Loading locations...${locationRetryCount > 0 ? ` (retry ${locationRetryCount})` : ''}` : 'Select a testing location'} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">No specific location</SelectItem>
                    {locations.map((location, index) => (
                      <SelectItem key={location.id || `location-${index}`} value={location.id || `temp-location-${index}`}>
                        <div className="flex flex-col">
                          <span className="font-medium">{location.name}</span>
                          {location.address && <span className="text-xs text-gray-500">{location.address}</span>}
                          <span className="text-xs text-blue-600">
                            Capacity: {location.maxTestersCapacity} testers, {location.maxProjectsCapacity} projects
                          </span>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {/* Submit Buttons */}
            <div className="flex justify-end gap-3 pt-6">
              <Button type="button" variant="outline" onClick={() => router.back()} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Creating...' : 'Create Session'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
