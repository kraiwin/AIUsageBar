using AIUsageBar.Core;

namespace AIUsageBar.Tests;

internal static class UsageTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return TestCase.Sync("usage/zero-is-real-not-missing", () =>
        {
            Check.Equal(100d, UsagePayloadParser.ParseCodex(Fixture.Codex("0"), Fixture.Now)!.Weekly.RemainingPercent);
            Check.Equal(0d, UsagePayloadParser.ParseClaude(Fixture.Claude("100"), Fixture.Now)!.Weekly.RemainingPercent);
            Check.True(UsagePayloadParser.ParseClaude(Fixture.Utf8("{}"), Fixture.Now) is null);
            Check.True(UsagePayloadParser.ParseCodex(Fixture.Utf8("{}"), Fixture.Now) is null);
        });
        foreach (var invalid in new[] { "true", "\"25\"", "null", "-1", "100.1", "1e999" })
        {
            var value = invalid;
            yield return TestCase.Sync($"usage/strict-percentage-{Array.IndexOf(new[] { "true", "\"25\"", "null", "-1", "100.1", "1e999" }, value)}", () =>
            {
                Check.Throws<CoreException>(() => UsagePayloadParser.ParseClaude(Fixture.Claude(value), Fixture.Now));
                Check.Throws<CoreException>(() => UsagePayloadParser.ParseCodex(Fixture.Codex(value), Fixture.Now));
            });
        }
        foreach (var invalid in new[] { "true", "\"1791273600\"", "null", "0", "-1", "253402300800" })
        {
            var value = invalid;
            yield return TestCase.Sync($"usage/strict-reset-{Array.IndexOf(new[] { "true", "\"1791273600\"", "null", "0", "-1", "253402300800" }, value)}", () =>
            {
                Check.Throws<CoreException>(() => UsagePayloadParser.ParseClaude(Fixture.Claude(reset: value), Fixture.Now));
                Check.Throws<CoreException>(() => UsagePayloadParser.ParseCodex(Fixture.Codex(reset: value), Fixture.Now));
            });
        }
        yield return TestCase.Sync("usage/missing-required-percentage", () =>
        {
            Check.Throws<CoreException>(() => UsagePayloadParser.ParseClaude(Fixture.Utf8("{\"rate_limits\":{\"seven_day\":{\"resets_at\":1791273600}}}"), Fixture.Now));
        });
        yield return TestCase.Sync("usage/select-codex-not-other-bucket", () =>
        {
            var other = Fixture.Utf8("{\"rateLimitsByLimitId\":{\"other\":{\"secondary\":{\"usedPercent\":10,\"resetsAt\":1791273600,\"windowDurationMins\":10080}}}}");
            Check.True(UsagePayloadParser.ParseCodex(other, Fixture.Now) is null);
            var conflict = Fixture.Utf8("{\"rateLimitsByLimitId\":{\"codex\":{\"limitId\":\"other\"}}}");
            Check.Throws<CoreException>(() => UsagePayloadParser.ParseCodex(conflict, Fixture.Now));
        });
        yield return TestCase.Sync("usage/duplicate-weekly-and-fraction-duration-rejected", () =>
        {
            Check.Throws<CoreException>(() => UsagePayloadParser.ParseCodex(Fixture.Codex(duration: "10080.5"), Fixture.Now));
            var duplicate = Fixture.Utf8("{\"rateLimits\":{\"primary\":{\"usedPercent\":10,\"resetsAt\":1791273600,\"windowDurationMins\":10080},\"secondary\":{\"usedPercent\":20,\"resetsAt\":1791273600,\"windowDurationMins\":10080}}}");
            Check.Throws<CoreException>(() => UsagePayloadParser.ParseCodex(duplicate, Fixture.Now));
        });
        yield return TestCase.Sync("usage/root-object-and-duplicate-field", () =>
        {
            foreach (var text in new[] { "[]", "true", "null", "{", "{\"rate_limits\":{},\"rate_limits\":{}}" })
                Check.Throws<CoreException>(() => UsagePayloadParser.ParseClaude(Fixture.Utf8(text), Fixture.Now));
        });
        yield return TestCase.Sync("freshness/snapshot-age-receipt-does-not-imply-server-fresh", () =>
        {
            var reading = UsagePayloadParser.ParseClaude(Fixture.Claude(), Fixture.Now)!;
            Check.Equal(UsageFreshness.Snapshot, Freshness.Evaluate(reading, Fixture.Now, TimeSpan.FromSeconds(300)));
            Check.Equal(UsageFreshness.Snapshot, Freshness.Evaluate(reading, Fixture.Now.AddHours(1), TimeSpan.FromSeconds(300)));
        });
        yield return TestCase.Sync("freshness/observed-boundary-reset-and-future", () =>
        {
            var reading = UsagePayloadParser.ParseCodex(Fixture.Codex(), Fixture.Now)! with { ProviderObservedAt = Fixture.Now };
            Check.Equal(UsageFreshness.Fresh, Freshness.Evaluate(reading, Fixture.Now.AddSeconds(300), TimeSpan.FromSeconds(300)));
            Check.Equal(UsageFreshness.Stale, Freshness.Evaluate(reading, Fixture.Now.AddSeconds(301), TimeSpan.FromSeconds(300)));
            Check.Equal(UsageFreshness.Expired, Freshness.Evaluate(reading, reading.Weekly.ResetsAt, TimeSpan.FromSeconds(300)));
            Check.Equal(UsageFreshness.InvalidTimestamp, Freshness.Evaluate(reading, Fixture.Now.AddSeconds(-1), TimeSpan.FromSeconds(300)));
            Check.Equal(UsageFreshness.InvalidTimestamp, Freshness.Evaluate(reading with { ProviderObservedAt = Fixture.Now.AddSeconds(1) }, Fixture.Now.AddSeconds(2), TimeSpan.FromSeconds(300)));
        });
    }
}
