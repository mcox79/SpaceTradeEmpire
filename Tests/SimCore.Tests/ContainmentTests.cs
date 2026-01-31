using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests;

public class ContainmentTests
{
    [Test]
    public void Containment_DecaysTrace_OverTime()
    {
        var state = new SimState(111);
        state.Nodes.Add("n1", new Node { Id = "n1", Trace = 1.0f });

        // Act: Process 1 Tick
        ContainmentSystem.Process(state);

        // Assert: Trace reduced by 0.05
        Assert.That(state.Nodes["n1"].Trace, Is.EqualTo(0.95f).Within(0.001f));
    }

    [Test]
    public void Containment_ClampsTrace_ToZero()
    {
        var state = new SimState(222);
        state.Nodes.Add("n1", new Node { Id = "n1", Trace = 0.02f });

        // Act: Process Tick (Decay 0.05 > 0.02)
        ContainmentSystem.Process(state);

        // Assert: Clamped
        Assert.That(state.Nodes["n1"].Trace, Is.EqualTo(0f));
    }
}