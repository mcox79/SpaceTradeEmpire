using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests;

public class MarketTests
{
    [Test]
    public void Price_Increases_WithScarcity()
    {
        var market = new Market();
        market.Demand["ore"] = 100;
        
        // Case 1: High Supply (Surplus) -> Low Price
        market.Inventory["ore"] = 200;
        int cheapPrice = market.GetPrice("ore");
        Assert.That(cheapPrice, Is.LessThan(100));

        // Case 2: Low Supply (Shortage) -> High Price
        market.Inventory["ore"] = 10;
        int expensivePrice = market.GetPrice("ore");
        Assert.That(expensivePrice, Is.GreaterThan(100));
    }

    [Test]
    public void Traffic_Generates_Heat()
    {
        var state = new SimState(123);
        state.Edges.Add("e1", new Edge { Id = "e1", Heat = 0f, Distance = 10f });
        
        // Setup Fleet moving on Edge
        var fleet = new Fleet 
        { 
            State = FleetState.Traveling, 
            CurrentEdgeId = "e1", 
            Speed = 1f,
            CurrentJob = new LogisticsJob { Amount = 100 } // Heavy cargo
        };
        state.Fleets.Add("f1", fleet);

        // Act
        MovementSystem.Process(state);

        // Assert
        Assert.That(state.Edges["e1"].Heat, Is.GreaterThan(0f));
    }
}