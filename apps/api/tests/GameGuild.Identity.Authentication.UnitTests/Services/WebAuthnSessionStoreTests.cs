using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class WebAuthnSessionStoreTests
{
    private static readonly Type SessionStoreType = typeof(AuthAttemptService).Assembly.GetType("GameGuild.Identity.Authentication.WebAuthnSessionStore", throwOnError: true)!;
    private static readonly Type SessionType = SessionStoreType.GetNestedType("WebAuthnSession", BindingFlags.NonPublic)!;

    [Fact]
    public void StoreFindAndRemoveMethods_ShouldManageSessionsByUserAndPredicate()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var session1 = CreateSession(userId1, [1]);
        var session2 = CreateSession(userId2, [2]);

        Store(session1);
        Store(session2);

        FindByUser(userId1, session => HasChallenge(session, 1)).Should().BeSameAs(session1);
        FindFirst(session => HasChallenge(session, 2)).Should().BeSameAs(session2);

        RemoveByUser(userId1);
        FindByUser(userId1, _ => true).Should().BeNull();

        RemoveFirst(session => GetPropertyValue<Guid?>(session, "UserId") == userId2);
        FindByUser(userId2, _ => true).Should().BeNull();

        var missingUserId = Guid.NewGuid();
        RemoveByUser(missingUserId);
        RemoveFirst(session => GetPropertyValue<Guid?>(session, "UserId") == missingUserId);
    }

    [Fact]
    public void Store_ShouldCleanupExpiredSessions()
    {
        var expiredUserId = Guid.NewGuid();
        var activeUserId = Guid.NewGuid();

        Store(CreateSession(expiredUserId, [9], DateTime.UtcNow.AddMinutes(-10)));

        var activeSession = CreateSession(activeUserId, [10], DateTime.UtcNow);

        Store(activeSession);

        FindByUser(expiredUserId, _ => true).Should().BeNull();
        FindByUser(activeUserId, session => HasChallenge(session, 10)).Should().BeSameAs(activeSession);

        RemoveByUser(activeUserId);
    }

    private static object CreateSession(Guid? userId, byte[] challenge, DateTime? createdAt = null)
    {
        var session = Activator.CreateInstance(SessionType)!;
        SetPropertyValue(session, "UserId", userId);
        SetPropertyValue(session, "Challenge", challenge);
        SetPropertyValue(session, "CreatedAt", createdAt ?? DateTime.UtcNow);
        return session;
    }

    private static string Store(object session) => (string)InvokeStoreMethod("Store", session)!;

    private static object? FindByUser(Guid userId, Func<object, bool> predicate) =>
        InvokeStoreMethod("FindByUser", userId, CreateSessionPredicate(predicate));

    private static object? FindFirst(Func<object, bool> predicate) =>
        InvokeStoreMethod("FindFirst", CreateSessionPredicate(predicate));

    private static void RemoveByUser(Guid userId) => InvokeStoreMethod("RemoveByUser", userId);

    private static void RemoveFirst(Func<object, bool> predicate) =>
        InvokeStoreMethod("RemoveFirst", CreateSessionPredicate(predicate));

    private static object InvokeStoreMethod(string methodName, params object[] arguments) =>
        SessionStoreType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, arguments)!;

    private static object CreateSessionPredicate(Func<object, bool> predicate)
    {
        var sessionParameter = Expression.Parameter(SessionType, "session");
        var body = Expression.Invoke(Expression.Constant(predicate), Expression.Convert(sessionParameter, typeof(object)));
        var delegateType = typeof(Func<,>).MakeGenericType(SessionType, typeof(bool));
        return Expression.Lambda(delegateType, body, sessionParameter).Compile();
    }

    private static bool HasChallenge(object session, byte value)
    {
        var challenge = GetPropertyValue<byte[]>(session, "Challenge");
        return challenge.Length == 1 && challenge[0] == value;
    }

    private static T GetPropertyValue<T>(object target, string propertyName) =>
        (T)SessionType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(target)!;

    private static void SetPropertyValue(object target, string propertyName, object? value) =>
        SessionType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
}