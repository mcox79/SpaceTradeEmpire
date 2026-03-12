using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.STATION_MEMORY.001
[TestFixture]
public sealed class StationMemoryTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    [Test]
    public void RecordDelivery_CreatesRecord()
    {
        var state = new SimState(42);
        AdvanceTo(state, 10);

        StationMemorySystem.RecordDelivery(state, "star_0", "food", 50);

        Assert.That(state.StationMemory, Has.Count.EqualTo(1));
        string key = StationDeliveryRecord.Key("star_0", "food");
        Assert.That(state.StationMemory.ContainsKey(key), Is.True);

        var record = state.StationMemory[key];
        Assert.That(record.TotalDeliveries, Is.EqualTo(1));
        Assert.That(record.TotalQuantity, Is.EqualTo(50));
        Assert.That(record.FirstDeliveryTick, Is.EqualTo(10));
        Assert.That(record.LastDeliveryTick, Is.EqualTo(10));
    }

    [Test]
    public void RecordDelivery_Increments()
    {
        var state = new SimState(42);
        AdvanceTo(state, 10);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 50);

        AdvanceTo(state, 20);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 30);

        string key = StationDeliveryRecord.Key("star_0", "food");
        var record = state.StationMemory[key];
        Assert.That(record.TotalDeliveries, Is.EqualTo(2));
        Assert.That(record.TotalQuantity, Is.EqualTo(80));
        Assert.That(record.FirstDeliveryTick, Is.EqualTo(10));
        Assert.That(record.LastDeliveryTick, Is.EqualTo(20));
    }

    [Test]
    public void RecordDelivery_RejectsZeroQuantity()
    {
        var state = new SimState(42);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 0);
        Assert.That(state.StationMemory, Is.Empty);
    }

    [Test]
    public void RecordDelivery_RejectsEmptyNodeOrGood()
    {
        var state = new SimState(42);
        StationMemorySystem.RecordDelivery(state, "", "food", 10);
        StationMemorySystem.RecordDelivery(state, "star_0", "", 10);
        Assert.That(state.StationMemory, Is.Empty);
    }

    [Test]
    public void RecordDelivery_RespectsMaxRecords()
    {
        var state = new SimState(42);

        // Fill up to max
        for (int i = 0; i < NarrativeTweaksV0.StationMemoryMaxRecords; i++)
        {
            StationMemorySystem.RecordDelivery(state, $"star_{i}", "food", 10);
        }
        Assert.That(state.StationMemory, Has.Count.EqualTo(NarrativeTweaksV0.StationMemoryMaxRecords));

        // Try to add a new record — should be rejected
        StationMemorySystem.RecordDelivery(state, "star_new", "food", 10);
        Assert.That(state.StationMemory, Has.Count.EqualTo(NarrativeTweaksV0.StationMemoryMaxRecords));

        // But updating an existing record should still work
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 20);
        string key = StationDeliveryRecord.Key("star_0", "food");
        Assert.That(state.StationMemory[key].TotalDeliveries, Is.EqualTo(2));
    }

    [Test]
    public void GetDeliveryCount_ReturnsCorrectValue()
    {
        var state = new SimState(42);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 10);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 20);
        StationMemorySystem.RecordDelivery(state, "star_0", "ore", 5);

        Assert.That(StationMemorySystem.GetDeliveryCount(state, "star_0", "food"), Is.EqualTo(2));
        Assert.That(StationMemorySystem.GetDeliveryCount(state, "star_0", "ore"), Is.EqualTo(1));
        Assert.That(StationMemorySystem.GetDeliveryCount(state, "star_1", "food"), Is.EqualTo(0));
    }

    [Test]
    public void GetTotalQuantity_ReturnsCorrectValue()
    {
        var state = new SimState(42);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 50);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 30);

        Assert.That(StationMemorySystem.GetTotalQuantity(state, "star_0", "food"), Is.EqualTo(80));
        Assert.That(StationMemorySystem.GetTotalQuantity(state, "star_1", "food"), Is.EqualTo(0));
    }

    [Test]
    public void IsReliableAtStation_RespectsThreshold()
    {
        var state = new SimState(42);

        // Below threshold
        for (int i = 0; i < NarrativeTweaksV0.StationmasterReliableThreshold - 1; i++)
        {
            StationMemorySystem.RecordDelivery(state, "star_0", "food", 10);
        }
        Assert.That(StationMemorySystem.IsReliableAtStation(state, "star_0"), Is.False);

        // At threshold
        StationMemorySystem.RecordDelivery(state, "star_0", "food", 10);
        Assert.That(StationMemorySystem.IsReliableAtStation(state, "star_0"), Is.True);
    }

    [Test]
    public void IsReliableAtStation_SumsAcrossGoods()
    {
        var state = new SimState(42);

        // Deliver different goods — total should still count
        for (int i = 0; i < NarrativeTweaksV0.StationmasterReliableThreshold; i++)
        {
            StationMemorySystem.RecordDelivery(state, "star_0", $"good_{i}", 10);
        }
        Assert.That(StationMemorySystem.IsReliableAtStation(state, "star_0"), Is.True);
    }

    [Test]
    public void RecordDelivery_RejectsNegativeQuantity()
    {
        var state = new SimState(42);
        StationMemorySystem.RecordDelivery(state, "star_0", "food", -5);
        Assert.That(state.StationMemory, Is.Empty);
    }

    [Test]
    public void IsReliableAtStation_NoRecords_ReturnsFalse()
    {
        var state = new SimState(42);
        Assert.That(StationMemorySystem.IsReliableAtStation(state, "star_0"), Is.False);
    }

    [Test]
    public void GetDeliveryCount_UnknownStation_ReturnsZero()
    {
        var state = new SimState(42);
        Assert.That(StationMemorySystem.GetDeliveryCount(state, "star_99", "food"), Is.EqualTo(0));
    }
}
