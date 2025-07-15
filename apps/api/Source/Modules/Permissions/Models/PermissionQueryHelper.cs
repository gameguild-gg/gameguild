namespace GameGuild.Modules.Permissions;

/// <summary>
/// Helper methods for permission queries without fluent API
/// </summary>
public static class PermissionQueryHelper {
  public static IQueryable<T> GetWithPermission<T>(IQueryable<T> query, PermissionType permission)
    where T : WithPermissions {
    var bitPos = (int)permission;

    if (bitPos < 64) {
      var mask = 1UL << bitPos;

      return query.Where(x => (x.PermissionFlags1 & mask) == mask);
    }
    else if (bitPos < 128) {
      var mask = 1UL << (bitPos - 64);

      return query.Where(x => (x.PermissionFlags2 & mask) == mask);
    }

    return query.Where(x => false);
  }

  public static IQueryable<T> GetWithAllPermissions<T>(IQueryable<T> query, params PermissionType[] permissions)
    where T : WithPermissions {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    var result = query;

    if (mask1 > 0) result = result.Where(x => (x.PermissionFlags1 & mask1) == mask1);

    if (mask2 > 0) result = result.Where(x => (x.PermissionFlags2 & mask2) == mask2);

    return result;
  }

  public static IQueryable<T> GetWithAnyPermission<T>(IQueryable<T> query, params PermissionType[] permissions)
    where T : WithPermissions {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    var result = query.Where(x => false); // Start with empty result

    if (mask1 > 0) result = result.Union(query.Where(x => (x.PermissionFlags1 & mask1) != 0));

    if (mask2 > 0) result = result.Union(query.Where(x => (x.PermissionFlags2 & mask2) != 0));

    return result;
  }

  /// <summary>
  /// Calculate permission masks for manual queries
  /// </summary>
  public static (ulong mask1, ulong mask2) CalculatePermissionMasks(params PermissionType[] permissions) {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    return (mask1, mask2);
  }
}
