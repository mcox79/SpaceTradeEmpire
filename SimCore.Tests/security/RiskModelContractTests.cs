using NUnit.Framework;
using SimCore;
using SimCore.Events;

namespace SimCore.Tests.security;

public class RiskModelContractTests
{
    [Test]
    public void RiskIncidents_AreDeterministic_ForSameSeedAndTicks()
    {
        var a = new SimKernel(seed: 42);
        var b = new SimKernel(seed: 42);

        for (int i = 0; i < 200; i++)
        {
            a.Step();
            b.Step();
        }

        var ea = a.State.SecurityEventLog ?? new();
        var eb = b.State.SecurityEventLog ?? new();

        Assert.That(eb.Count, Is.EqualTo(ea.Count));

        for (int i = 0; i < ea.Count; i++)
        {
            var x = ea[i];
            var y = eb[i];

            Assert.That(y.Version, Is.EqualTo(x.Version));
            Assert.That(y.Seq, Is.EqualTo(x.Seq));
            Assert.That(y.Tick, Is.EqualTo(x.Tick));
            Assert.That(y.Type, Is.EqualTo(x.Type));

            Assert.That(y.EdgeId, Is.EqualTo(x.EdgeId));
            Assert.That(y.FromNodeId, Is.EqualTo(x.FromNodeId));
            Assert.That(y.ToNodeId, Is.EqualTo(x.ToNodeId));

            Assert.That(y.RiskBand, Is.EqualTo(x.RiskBand));
            Assert.That(y.DelayTicks, Is.EqualTo(x.DelayTicks));
            Assert.That(y.LossUnits, Is.EqualTo(x.LossUnits));
            Assert.That(y.InspectionTicks, Is.EqualTo(x.InspectionTicks));

            Assert.That(y.CauseChain, Is.EqualTo(x.CauseChain));
            Assert.That(y.Note, Is.EqualTo(x.Note));
        }

        var payload = SecurityEvents.BuildPayload(a.State.Tick, ea);
        var json = SecurityEvents.ToDeterministicJson(payload);
        SecurityEvents.ValidateJsonIsSchemaBound(json);
    }

    [Test]
    public void RiskIncidents_SaveLoad_ContinuesDeterministically_MidStream()
    {
        var k1 = new SimKernel(seed: 1337);

        for (int i = 0; i < 50; i++) k1.Step();

        var saved = k1.SaveToString();

        for (int i = 0; i < 150; i++) k1.Step();

        var k2 = new SimKernel(seed: 1337);
        k2.LoadFromString(saved);

        for (int i = 0; i < 150; i++) k2.Step();

        var a = k1.State.SecurityEventLog ?? new();
        var b = k2.State.SecurityEventLog ?? new();

        Assert.That(b.Count, Is.EqualTo(a.Count));

        for (int i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].Seq, Is.EqualTo(a[i].Seq));
            Assert.That(b[i].Tick, Is.EqualTo(a[i].Tick));
            Assert.That(b[i].Type, Is.EqualTo(a[i].Type));
            Assert.That(b[i].EdgeId, Is.EqualTo(a[i].EdgeId));
            Assert.That(b[i].FromNodeId, Is.EqualTo(a[i].FromNodeId));
            Assert.That(b[i].ToNodeId, Is.EqualTo(a[i].ToNodeId));
            Assert.That(b[i].RiskBand, Is.EqualTo(a[i].RiskBand));
            Assert.That(b[i].DelayTicks, Is.EqualTo(a[i].DelayTicks));
            Assert.That(b[i].LossUnits, Is.EqualTo(a[i].LossUnits));
            Assert.That(b[i].InspectionTicks, Is.EqualTo(a[i].InspectionTicks));
            Assert.That(b[i].CauseChain, Is.EqualTo(a[i].CauseChain));
            Assert.That(b[i].Note, Is.EqualTo(a[i].Note));
        }
    }
}
