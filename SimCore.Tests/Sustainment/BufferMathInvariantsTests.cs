using System.Collections.Generic;
using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Sustainment;

public class BufferMathInvariantsTests
{
    [Test]
    public void BufferTarget_Is_Zero_When_BufferDays_Is_Zero()
    {
        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 2 } },
            BufferDays = 0
        };

        int target = IndustrySystem.ComputeBufferTargetUnits(site, "ore");
        Assert.That(target, Is.EqualTo(0));
    }

    [Test]
    public void BufferTarget_Scales_Linearly_With_Days()
    {
        var site1 = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 3 } },
            BufferDays = 1
        };

        var site3 = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 3 } },
            BufferDays = 3
        };

        int t1 = IndustrySystem.ComputeBufferTargetUnits(site1, "ore");
        int t3 = IndustrySystem.ComputeBufferTargetUnits(site3, "ore");

        Assert.That(t3, Is.EqualTo(3 * t1));
        Assert.That(t1, Is.EqualTo(3 * IndustrySystem.TicksPerDay));
    }

    [Test]
    public void BufferTarget_Scales_Linearly_With_PerTick_Input()
    {
        var site2 = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 2 } },
            BufferDays = 2
        };

        var site5 = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 5 } },
            BufferDays = 2
        };

        int t2 = IndustrySystem.ComputeBufferTargetUnits(site2, "ore");
        int t5 = IndustrySystem.ComputeBufferTargetUnits(site5, "ore");

        Assert.That(t5, Is.EqualTo((5 * t2) / 2));
        Assert.That(t2, Is.EqualTo(2 * 2 * IndustrySystem.TicksPerDay));
        Assert.That(t5, Is.EqualTo(5 * 2 * IndustrySystem.TicksPerDay));
    }
}
