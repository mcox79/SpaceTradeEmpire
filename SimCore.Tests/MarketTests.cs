using NUnit.Framework;
using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Tests
{
    [TestFixture]
    public class MarketTests
    {
        [Test]
        public void GetPrice_LowSupply_ReturnsHighPrice()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 10; // Low supply

            // Act
            int price = market.GetPrice("fuel");

            // Assert (Stub Logic: 100 + (50 - 10) = 140)
            Assert.That(price, Is.GreaterThan(100), "Price should be high when scarce.");
        }

        [Test]
        public void GetPrice_HighSupply_ReturnsLowPrice()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 100; // High supply

            // Act
            int price = market.GetPrice("fuel");

            // Assert (Stub Logic: 100 + (50 - 100) = 50)
            Assert.That(price, Is.LessThan(100), "Price should be low when abundant.");
        }

        [Test]
        public void GetPrice_ZeroSupply_ReturnsMaxPrice()
        {
             // Arrange
            var market = new Market();
            // No inventory set

            // Act
            int price = market.GetPrice("gold");

            // Assert
            Assert.That(price, Is.GreaterThan(100));
        }
    }
}