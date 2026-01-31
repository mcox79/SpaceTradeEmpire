using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests;

public class SignalTests
{
    [Test]
    public void Signal_TraceAndHeat_ArePersistedInSignature()
    {
        var state = new SimState(123);
        state.Nodes.Add("n1", new Node { Id = "n1", Trace = 0f });
        state.Edges.Add("e1", new Edge { Id = "e1", Heat = 0f });

        string baseHash = state.GetSignature();

        state.Nodes["n1"].Trace = 0.5f;
        string traceHash = state.GetSignature();
        Assert.That(traceHash, Is.Not.EqualTo(baseHash));

        state.Edges["e1"].Heat = 0.8f;
        string heatHash = state.GetSignature();
        Assert.That(heatHash, Is.Not.EqualTo(traceHash));
    }
}