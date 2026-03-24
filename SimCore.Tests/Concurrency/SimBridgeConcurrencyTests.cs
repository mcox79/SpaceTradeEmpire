using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimCore.Tests.Concurrency;

/// <summary>
/// Concurrency stress tests for SimCore's thread-safety contracts.
/// These tests verify that concurrent reads during simulation stepping
/// do not corrupt state, and that the locking patterns used in SimBridge
/// are safe when replicated.
///
/// Note: Full Coyote systematic testing requires the Coyote rewriter tool
/// (coyote rewrite) for deterministic thread interleaving exploration.
/// These tests use traditional stress testing as a first layer.
/// </summary>
[TestFixture]
public class SimBridgeConcurrencyTests
{
    private const int StarCount = 12;
    private const float Radius = 100f;

    /// <summary>
    /// Simulates the SimBridge pattern: one writer thread stepping the sim,
    /// multiple reader threads querying state via a ReaderWriterLockSlim.
    /// Verifies no exceptions and no corrupted reads.
    /// </summary>
    [Test]
    public void ConcurrentReadsAndWrites_DoNotCorrupt_StateSnapshots()
    {
        const int totalTicks = 500;
        const int readerCount = 4;

        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, StarCount, Radius);

        var rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        var cts = new CancellationTokenSource();
        var exceptions = new List<Exception>();
        var readCount = 0;
        var writesDone = 0;

        // Writer thread: steps sim under write lock (mirrors SimBridge.SimLoop)
        var writer = Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < totalTicks; i++)
                {
                    rwLock.EnterWriteLock();
                    try
                    {
                        sim.Step();
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                    Interlocked.Increment(ref writesDone);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
            finally
            {
                cts.Cancel();
            }
        });

        // Reader threads: snapshot state under read lock (mirrors SimBridge UI queries)
        var readers = new Task[readerCount];
        for (int r = 0; r < readerCount; r++)
        {
            readers[r] = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        if (rwLock.TryEnterReadLock(5))
                        {
                            try
                            {
                                // Snapshot operations that SimBridge performs on the main thread
                                var credits = sim.State.PlayerCredits;
                                var loc = sim.State.PlayerLocationNodeId;
                                var tick = sim.State.Tick;
                                var fleetCount = sim.State.Fleets.Count;
                                var marketCount = sim.State.Markets.Count;

                                // Verify basic consistency
                                Assert.That(credits, Is.GreaterThanOrEqualTo(0),
                                    $"Credits negative during concurrent read at tick ~{tick}");
                                Assert.That(fleetCount, Is.GreaterThanOrEqualTo(0));
                                Assert.That(marketCount, Is.GreaterThanOrEqualTo(0));

                                Interlocked.Increment(ref readCount);
                            }
                            finally
                            {
                                rwLock.ExitReadLock();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
        }

        writer.Wait();
        Task.WaitAll(readers);
        rwLock.Dispose();

        Assert.That(exceptions, Is.Empty,
            $"Concurrent access threw exceptions:\n{string.Join("\n", exceptions)}");
        Assert.That(Volatile.Read(ref writesDone), Is.EqualTo(totalTicks));
        Assert.That(Volatile.Read(ref readCount), Is.GreaterThan(0),
            "No concurrent reads were performed — test is not exercising concurrency");
    }

    /// <summary>
    /// Tests the non-blocking TryEnterReadLock pattern used in SimBridge
    /// for UI snapshots that must not stall the game frame.
    /// With NoRecursion policy, TryEnterReadLock from a DIFFERENT thread
    /// must fail when a write lock is held, returning cached values.
    /// </summary>
    [Test]
    public void NonBlockingReadLock_Returns_CachedValues_When_WriteLockHeld()
    {
        var rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        long cachedCredits = 999;
        long readValue = -1;
        var writeLockAcquired = new ManualResetEventSlim(false);
        var readAttemptDone = new ManualResetEventSlim(false);

        // Writer thread holds write lock (simulates sim stepping)
        var writer = Task.Run(() =>
        {
            rwLock.EnterWriteLock();
            writeLockAcquired.Set();
            // Wait for reader to attempt and finish
            readAttemptDone.Wait(TimeSpan.FromSeconds(5));
            rwLock.ExitWriteLock();
        });

        // Reader thread attempts non-blocking read (simulates UI query)
        var reader = Task.Run(() =>
        {
            writeLockAcquired.Wait(TimeSpan.FromSeconds(5));
            // With write lock held on another thread, TryEnterReadLock(0) should fail
            if (rwLock.TryEnterReadLock(0))
            {
                try { Volatile.Write(ref readValue, -1); }
                finally { rwLock.ExitReadLock(); }
            }
            else
            {
                Volatile.Write(ref readValue, Volatile.Read(ref cachedCredits));
            }
            readAttemptDone.Set();
        });

        Task.WaitAll(writer, reader);
        writeLockAcquired.Dispose();
        readAttemptDone.Dispose();
        rwLock.Dispose();

        Assert.That(Volatile.Read(ref readValue), Is.EqualTo(999),
            "Non-blocking read should return cached value when write lock is held on another thread");
    }

    /// <summary>
    /// Verify that volatile read/write patterns used for bridge readiness flags
    /// are visible across threads without explicit locking.
    /// </summary>
    [Test]
    public void VolatileFlags_AreVisibleAcrossThreads()
    {
        int flag = 0;
        bool seen = false;

        var writer = Task.Run(() =>
        {
            Thread.Sleep(10);
            Volatile.Write(ref flag, 1);
        });

        var reader = Task.Run(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 2000)
            {
                if (Volatile.Read(ref flag) == 1)
                {
                    seen = true;
                    break;
                }
                Thread.SpinWait(100);
            }
        });

        Task.WaitAll(writer, reader);

        Assert.That(seen, Is.True,
            "Volatile write was not visible to reader thread within 2 seconds");
    }

    /// <summary>
    /// Stress test: multiple threads calling GetSignature concurrently under read lock.
    /// Signature computation touches every collection in SimState; concurrent reads
    /// must not throw or produce garbled output.
    /// </summary>
    [Test]
    public void ConcurrentSignatureReads_ProduceConsistentResults()
    {
        const int readerCount = 4;
        const int readsPerThread = 50;

        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, StarCount, Radius);
        for (int i = 0; i < 100; i++) sim.Step();

        var rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string expectedSig;
        rwLock.EnterReadLock();
        try { expectedSig = sim.State.GetSignature(); }
        finally { rwLock.ExitReadLock(); }

        var exceptions = new List<Exception>();
        var mismatches = 0;

        var tasks = new Task[readerCount];
        for (int r = 0; r < readerCount; r++)
        {
            tasks[r] = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < readsPerThread; i++)
                    {
                        rwLock.EnterReadLock();
                        try
                        {
                            var sig = sim.State.GetSignature();
                            if (sig != expectedSig)
                                Interlocked.Increment(ref mismatches);
                        }
                        finally
                        {
                            rwLock.ExitReadLock();
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
        }

        Task.WaitAll(tasks);
        rwLock.Dispose();

        Assert.That(exceptions, Is.Empty,
            $"Concurrent signature reads threw exceptions:\n{string.Join("\n", exceptions)}");
        Assert.That(mismatches, Is.EqualTo(0),
            "Concurrent signature reads produced inconsistent results");
    }
}
