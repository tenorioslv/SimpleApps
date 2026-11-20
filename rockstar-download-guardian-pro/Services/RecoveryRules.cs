using System;
namespace RockstarGuardian.Services;
public static class RecoveryRules
{
    public static bool ShouldStart(bool logError, TimeSpan inactivity) => logError || inactivity >= TimeSpan.FromSeconds(25);
    public static bool ShouldEscalate(int level, TimeSpan elapsed) => level == 1 ? elapsed >= TimeSpan.FromSeconds(15) : level == 2 && elapsed >= TimeSpan.FromSeconds(20);
    public static bool IsComplete(bool observedActivity, long bytes, double target, TimeSpan stable, int level) => observedActivity && bytes >= target && stable >= TimeSpan.FromSeconds(60) && level == 0;
}
