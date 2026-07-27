namespace BLL.Helpers
{
    /// <summary>
    /// Returns the current time in Egypt regardless of the server's local timezone.
    /// Handles Egypt's DST automatically (summer = +3, winter = +2).
    /// </summary>
    public static class EgyptTime
    {
        private static readonly TimeZoneInfo _egyptZone = ResolveZone();

        private static TimeZoneInfo ResolveZone()
        {
            // Try Windows ID first, then IANA (Linux / macOS).
            try { return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); }
            catch { }
            try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
            catch { }
            // Fallback: fixed +2 (no DST). Should never happen on supported OS.
            return TimeZoneInfo.CreateCustomTimeZone("Egypt-Fallback", TimeSpan.FromHours(2), "Egypt", "Egypt");
        }

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _egyptZone);
    }
}
