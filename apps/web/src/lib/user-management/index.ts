// User Management Module - Main Index
// Comprehensive server actions for user lifecycle, profiles, and achievements

// Users Module - Core user management operations
export * from './users/users.actions';

// Profiles Module - User profile management and customization
export * from './profiles/profiles.actions';

// Achievements Module - Achievement tracking and progress management
export * from './achievements/achievements.actions';

/**
 * User Management Modules Overview:
 *
 * Users Module (9 functions):
 * - getUsersData() - Get paginated users with filtering
 * - getUserById() - Get specific user by ID
 * - createUser() - Create new user account
 * - updateUser() - Update user information
 * - deleteUser() - Soft delete user account
 * - restoreUser() - Restore deleted user account
 * - updateUserBalance() - Update user's account balance
 * - searchUsers() - Search users with advanced filtering
 * - getUserStatistics() - Get user statistics and analytics
 *
 * Profiles Module (8 functions):
 * - getUserProfilesData() - Get paginated user profiles
 * - getUserProfileById() - Get specific user profile by ID
 * - getUserProfileByUserId() - Get user profile by user ID
 * - createUserProfile() - Create new user profile
 * - updateUserProfile() - Update user profile information
 * - deleteUserProfile() - Delete user profile
 * - restoreUserProfile() - Restore deleted user profile
 * - getOrCreateUserProfile() - Get or create user profile utility
 * - updateUserProfileWithVersion() - Update profile with version control
 *
 * Achievements Module (11 functions):
 * - getUserAchievements() - Get user's earned achievements
 * - getUserAchievementProgress() - Get achievement progress details
 * - getUserAchievementSummary() - Get achievement summary statistics
 * - getUserAvailableAchievements() - Get achievements user can pursue
 * - updateAchievementProgress() - Update user's achievement progress
 * - getAchievementPrerequisites() - Get achievement requirements
 * - markAchievementAsNotified() - Mark achievement notification as sent
 * - removeUserAchievement() - Remove user's achievement
 * - getComprehensiveUserAchievements() - Get complete achievement overview
 * - canUserPursueAchievement() - Check if user can pursue achievement
 * - getUserAchievementLeaderboard() - Get achievement leaderboard data
 *
 * Total: 28 comprehensive user management functions covering core user lifecycle,
 * profile management, and achievement tracking.
 */
