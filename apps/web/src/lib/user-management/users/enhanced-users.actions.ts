'use server';

// Re-export all functions from the comprehensive user management system
// This maintains backward compatibility while providing access to enhanced functionality

export {
  // Core CRUD Operations (enhanced versions)
  getUsers as getUsersData,
  getUserById,
  createUser,
  updateUser,
  deleteUser,
  restoreUser,
  // User Management
  updateUserBalance,
  searchUsers,
  getUserStatistics,
  bulkUserOperations,
  // User Achievement Management
  getUserAchievements,
  getUserAchievementProgress,
  getUserAchievementSummary,
  getUserAvailableAchievements,
  updateUserAchievementProgress,
  getUserAchievementPrerequisites,
  markUserAchievementNotified,
  removeUserAchievement,
  // Administrative Functions
  getUserDashboardData,
  activateUsers,
  deactivateUsers,
  generateUserActivityReport,
} from '../comprehensive-users.actions';
