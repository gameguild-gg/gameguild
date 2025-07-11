export interface User {
  id: string;
  email: string;
  username: string;
  firstName: string;
  lastName: string;
  avatar: string | null;
  role: 'admin' | 'instructor' | 'student';
  emailVerified: boolean;
  createdAt: Date;
  updatedAt: Date;
  lastLoginAt: Date | null;
  isActive: boolean;
  preferences: UserPreferences;
  profile: UserProfile;
}

export interface UserPreferences {
  theme: 'light' | 'dark' | 'system';
  language: string;
  timezone: string;
  notifications: NotificationSettings;
  privacy: PrivacySettings;
}

export interface NotificationSettings {
  email: boolean;
  push: boolean;
  courseUpdates: boolean;
  marketing: boolean;
  security: boolean;
}

export interface PrivacySettings {
  profileVisibility: 'public' | 'private' | 'friends';
  showEmail: boolean;
  showProgress: boolean;
  allowMessages: boolean;
}

export interface UserProfile {
  bio: string | null;
  website: string | null;
  location: string | null;
  skills: string[];
  interests: string[];
  socialLinks: SocialLinks;
}

export interface SocialLinks {
  github: string | null;
  linkedin: string | null;
  twitter: string | null;
  portfolio: string | null;
}

export interface AuthSession {
  user: User;
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
}

export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterData {
  email: string;
  username: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

export interface ResetPasswordData {
  email: string;
}

export interface ChangePasswordData {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface AuthError {
  code: string;
  message: string;
  field?: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: AuthError | null;
}

export interface LoginFormProps {
  onSubmit: (credentials: LoginCredentials) => Promise<void>;
  loading?: boolean;
  error?: AuthError | null;
  redirectUrl?: string;
}

export interface RegisterFormProps {
  onSubmit: (data: RegisterData) => Promise<void>;
  loading?: boolean;
  error?: AuthError | null;
  redirectUrl?: string;
}

export interface UserAvatarProps {
  user: User;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  showStatus?: boolean;
  fallbackIcon?: boolean;
}

export interface ProfileSettingsProps {
  user: User;
  onUpdate: (updates: Partial<User>) => Promise<void>;
  loading?: boolean;
}
