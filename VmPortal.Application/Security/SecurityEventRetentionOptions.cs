namespace VmPortal.Application.Security
{
    public sealed class SecurityEventRetentionOptions
    {
        public Int32 Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public String? UserId { get; set; }
        public int RetentionDays { get; set; } = 365;
        public int CleanupIntervalHours { get; set; } = 24;
    }
}
