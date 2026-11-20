using System;
namespace RockstarGuardian.Services;
public enum RetryAction { None, Retry, Resume }
public static class RetryPolicy
{
    public static RetryAction Choose(bool retry, bool resume, bool recentError) => retry ? RetryAction.Retry : resume && recentError ? RetryAction.Resume : RetryAction.None;
    public static bool CanInvoke(TimeSpan elapsed) => elapsed >= TimeSpan.FromSeconds(15);
    public static bool HasRecentError(DateTime now, DateTime? errorAt) => errorAt is DateTime at && now >= at && now - at <= TimeSpan.FromSeconds(60);
}
