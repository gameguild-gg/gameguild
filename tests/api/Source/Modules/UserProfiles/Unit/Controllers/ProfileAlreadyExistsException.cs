namespace GameGuild.Tests.Modules.UserProfiles.Unit.Controllers;

public class ProfileAlreadyExistsException(string message) : Exception(message);