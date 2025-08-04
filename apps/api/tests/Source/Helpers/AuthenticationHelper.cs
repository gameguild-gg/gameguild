using System.Security.Claims;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using GameGuild.Common;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Helpers
{
    /// <summary>
    /// Helper class to set up authentication context for integration tests
    /// </summary>
    public static class AuthenticationHelper
    {
        /// <summary>
        /// Creates an authenticated user context for testing
        /// </summary>
        /// <param name="serviceProvider">Service provider for accessing services</param>
        /// <param name="userId">Optional user ID to use, creates a new user if not provided</param>
        /// <param name="email">Email for the user</param>
        /// <param name="roles">Roles to assign to the user</param>
        /// <returns>Authentication token and user information</returns>
        public static async Task<(string token, User user)> CreateAuthenticatedUserAsync(
            IServiceProvider serviceProvider,
            Guid? userId = null,
            string? email = null,
            string[]? roles = null)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            // Create or get user
            User user;
            if (userId.HasValue)
            {
                user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && u.DeletedAt == null) 
                    ?? throw new InvalidOperationException($"User with ID {userId} not found");
            }
            else
            {
                user = new User
                {
                    Name = "Test User",
                    Email = email ?? $"test.user.{Guid.NewGuid()}@example.com",
                    IsActive = true,
                    Balance = 100m,
                    AvailableBalance = 100m,
                };
                
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new("UserId", user.Id.ToString()),
            };

            // Add roles if provided
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // Generate JWT token
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Name,
                Email = user.Email,
            };
            
            var rolesArray = roles ?? new[] { "User" };
            var token = jwtService.GenerateAccessToken(userDto, rolesArray, claims);

            return (token, user);
        }

        /// <summary>
        /// Creates an admin user with admin role
        /// </summary>
        public static async Task<(string token, User user)> CreateAdminUserAsync(IServiceProvider serviceProvider)
        {
            return await CreateAuthenticatedUserAsync(serviceProvider, roles: new[] { "Admin" });
        }

        /// <summary>
        /// Sets up an HttpContext with authentication for a given user
        /// </summary>
        public static void SetupHttpContextAuth(HttpContext httpContext, User user, string[]? roles = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new("UserId", user.Id.ToString()),
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            httpContext.User = principal;
        }

        /// <summary>
        /// Creates an authenticated HTTP client that bypasses JWT authentication in tests
        /// </summary>
        /// <param name="factory">WebApplicationFactory instance</param>
        /// <returns>HttpClient configured for test environment with authentication bypass</returns>
        [Obsolete("Use CreateAuthenticatedHttpClient(factory, user) or CreateAnonymousHttpClient(factory) instead")]
        public static HttpClient CreateAuthenticatedHttpClient(WebApplicationFactory<Program> factory)
        {
            // This method is obsolete - use the overload that takes a User parameter for proper JWT authentication
            return factory.CreateClient();
        }

        /// <summary>
        /// Creates an HTTP client with JWT authentication for the given user
        /// </summary>
        public static async Task<HttpClient> CreateAuthenticatedHttpClient(
            WebApplicationFactory<Program> factory, 
            User user)
        {
            // Create JWT token for the user
            var (token, _) = await CreateAuthenticatedUserAsync(factory.Services, user.Id, user.Email, new[] { "User" });
            
            // Create HTTP client with JWT token
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            return client;
        }

        /// <summary>
        /// Creates an HTTP client without authentication (for anonymous requests)
        /// </summary>
        public static HttpClient CreateAnonymousHttpClient(WebApplicationFactory<Program> factory)
        {
            return factory.CreateClient();
        }

        /// <summary>
        /// Creates an authenticated user and returns both the user info and an authenticated HTTP client
        /// </summary>
        /// <param name="factory">WebApplicationFactory instance</param>
        /// <param name="dbContext">Database context for creating the user</param>
        /// <param name="userId">Optional user ID to use, creates a new user if not provided</param>
        /// <param name="email">Email for the user</param>
        /// <param name="roles">Roles to assign to the user</param>
        /// <returns>Tuple containing the HTTP client and user information</returns>
        public static async Task<(HttpClient client, User user)> CreateAuthenticatedUserWithClientAsync(
            WebApplicationFactory<Program> factory,
            ApplicationDbContext dbContext,
            Guid? userId = null,
            string? email = null,
            string[]? roles = null)
        {
            var user = await CreateUserAsync(dbContext, isAdmin: false, userId, email);
            var client = await CreateAuthenticatedHttpClient(factory, user);
        
            return (client, user);
        }

        /// <summary>
        /// Creates an admin user and returns both the user and HTTP client
        /// </summary>
        public static async Task<(HttpClient client, User user)> CreateAdminUserWithClientAsync(
            WebApplicationFactory<Program> factory,
            ApplicationDbContext dbContext)
        {
            var user = await CreateUserAsync(dbContext, isAdmin: true);
            
            // Grant admin permissions using IPermissionService
            using var scope = factory.Services.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            
            // Grant global tenant permissions like the super admin gets
            var globalPermissions = new PermissionType[] { 
                PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete, 
                PermissionType.Publish, PermissionType.Approve, PermissionType.Review,
            };
            
            await permissionService.GrantTenantPermissionAsync(user.Id, null, globalPermissions);
            
            // Grant content type permissions for tenant-related entities
            var contentTypes = new[] { "Project", "TenantDomain", "TenantUserGroup", "TenantUserGroupMembership", "User", "Tenant", "Comment", "Product", "Program" };
            var contentPermissions = new PermissionType[] { PermissionType.Create, PermissionType.Read, PermissionType.Edit, PermissionType.Delete };
            
            foreach (var contentType in contentTypes)
            {
                await permissionService.GrantContentTypePermissionAsync(user.Id, null, contentType, contentPermissions);
            }
            
            // Create JWT token for the admin user
            var (token, _) = await CreateAuthenticatedUserAsync(factory.Services, user.Id, user.Email, new[] { "Admin" });
            
            // Create HTTP client with JWT token
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
            return (client, user);
        }

        /// <summary>
        /// Creates a regular user and returns both the user and HTTP client
        /// </summary>
        public static async Task<(HttpClient client, User user)> CreateRegularUserWithClientAsync(
            WebApplicationFactory<Program> factory,
            ApplicationDbContext dbContext)
        {
            var user = await CreateUserAsync(dbContext, isAdmin: false);
            var client = await CreateAuthenticatedHttpClient(factory, user);
        
            return (client, user);
        }

        /// <summary>
        /// Simple helper to create a user in the database
        /// </summary>
        private static async Task<User> CreateUserAsync(ApplicationDbContext dbContext, bool isAdmin = false, Guid? userId = null, string? email = null)
        {
            var user = new User
            {
                Id = userId ?? Guid.NewGuid(),
                Name = isAdmin ? "Admin User" : "Test User",
                Email = email ?? $"test.user.{Guid.NewGuid()}@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            
            return user;
        }
    }
}
