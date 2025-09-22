using GameGuild.Tests.Manual;

// Manual test runner for permission system validation
Console.WriteLine("Starting Permission System Validation...");

bool result = await PermissionSystemValidator.ValidatePermissionSystemAsync();

Console.WriteLine($"Validation Result: {(result ? "SUCCESS" : "FAILURE")}");
Environment.Exit(result ? 0 : 1);
