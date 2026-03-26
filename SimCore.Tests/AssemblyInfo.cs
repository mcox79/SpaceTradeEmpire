using NUnit.Framework;

// GATE.T56.FIX.TEST_HANG.001: Global timeout prevents any single test from hanging the suite.
// Individual tests that legitimately need more time should override with [Timeout(N)].
[assembly: Timeout(60_000)]  // 60 seconds default per test
