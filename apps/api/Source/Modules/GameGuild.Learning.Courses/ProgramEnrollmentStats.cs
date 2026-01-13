namespace GameGuild.Programs;

/// <summary> Enrollment statistics for a program </summary>
public class ProgramEnrollmentStats {
    public int TotalEnrollments { get; set; }

    public int ActiveEnrollments { get; set; }

    public int CompletedEnrollments { get; set; }

    public int CancelledEnrollments { get; set; }

    public decimal AverageProgressPercentage { get; set; }

    public decimal CompletionRate { get; set; }

    public decimal? AverageFinalGrade { get; set; }

    public int CertificatesIssued { get; set; }
}