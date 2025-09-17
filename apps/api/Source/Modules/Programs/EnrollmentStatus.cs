using System.ComponentModel;


namespace GameGuild;

public enum EnrollmentStatus { [Description("Open for enrollment")] Open, [Description("Closed for enrollment")] Closed, [Description("Invite only")] InviteOnly, [Description("Waitlist available")] Waitlist }
