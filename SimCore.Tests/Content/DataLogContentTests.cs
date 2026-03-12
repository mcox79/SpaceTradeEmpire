using NUnit.Framework;
using SimCore.Content;
using SimCore.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Content;

// GATE.T18.NARRATIVE.DATALOG_CONTENT.001
[TestFixture]
public sealed class DataLogContentTests
{
    [Test]
    public void AllLogs_HaveValidIds()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            Assert.That(log.LogId, Is.Not.Empty, "Log has empty LogId");
            Assert.That(log.LogId, Does.StartWith("LOG."), $"LogId {log.LogId} doesn't start with LOG.");
        }
    }

    [Test]
    public void AllLogs_HaveEntries()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            Assert.That(log.Entries, Is.Not.Empty, $"Log {log.LogId} has no entries");
        }
    }

    [Test]
    public void AllLogs_HaveSpeakers()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            Assert.That(log.Speakers, Is.Not.Empty, $"Log {log.LogId} has no speakers");
            foreach (var speaker in log.Speakers)
            {
                Assert.That(speaker, Is.Not.Empty, $"Log {log.LogId} has empty speaker name");
            }
        }
    }

    [Test]
    public void AllLogs_HaveValidRevelationTier()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            Assert.That(log.RevelationTier, Is.InRange(1, 3),
                $"Log {log.LogId} has invalid RevelationTier {log.RevelationTier}");
        }
    }

    [Test]
    public void AllLogs_HaveAtLeastOnePersonalLine()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            bool hasPersonal = log.Entries.Any(e => e.IsPersonal);
            Assert.That(hasPersonal, Is.True,
                $"Log {log.LogId} has no personal line (design requirement: every log has personal texture)");
        }
    }

    [Test]
    public void AllLogs_EntrySpeakersMatchLogSpeakers()
    {
        foreach (var log in DataLogContentV0.AllLogs)
        {
            var logSpeakers = new HashSet<string>(log.Speakers);
            foreach (var entry in log.Entries)
            {
                Assert.That(logSpeakers, Does.Contain(entry.Speaker),
                    $"Log {log.LogId} entry speaker '{entry.Speaker}' not in log speakers list");
            }
        }
    }

    [Test]
    public void AllLogs_UniqueIds()
    {
        var ids = new HashSet<string>();
        foreach (var log in DataLogContentV0.AllLogs)
        {
            Assert.That(ids.Add(log.LogId), Is.True,
                $"Duplicate log ID: {log.LogId}");
        }
    }

    [Test]
    public void SixThreads_AllRepresented()
    {
        var threads = new HashSet<DataLogThread>();
        foreach (var log in DataLogContentV0.AllLogs)
        {
            threads.Add(log.Thread);
        }

        Assert.That(threads, Does.Contain(DataLogThread.Containment));
        Assert.That(threads, Does.Contain(DataLogThread.Lattice));
        Assert.That(threads, Does.Contain(DataLogThread.Departure));
        Assert.That(threads, Does.Contain(DataLogThread.Accommodation));
        Assert.That(threads, Does.Contain(DataLogThread.Warning));
        Assert.That(threads, Does.Contain(DataLogThread.EconTopology));
    }

    [Test]
    public void FiveVoices_AllPresent()
    {
        var allSpeakers = new HashSet<string>();
        foreach (var log in DataLogContentV0.AllLogs)
        {
            foreach (var s in log.Speakers) allSpeakers.Add(s);
        }

        Assert.That(allSpeakers, Does.Contain("Kesh"));
        Assert.That(allSpeakers, Does.Contain("Vael"));
        Assert.That(allSpeakers, Does.Contain("Tal"));
        Assert.That(allSpeakers, Does.Contain("Oruth"));
        Assert.That(allSpeakers, Does.Contain("Senn"));
    }

    [Test]
    public void LogCount_IsExpected()
    {
        // Design calls for 25 logs across 6 threads
        Assert.That(DataLogContentV0.AllLogs, Has.Count.EqualTo(25));
    }
}
