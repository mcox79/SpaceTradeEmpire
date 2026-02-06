using NUnit.Framework;
using SimCore.Entities;

namespace SimCore.Tests
{
	[TestFixture]
	public class MarketTests
	{
		[Test]
		public void MidPrice_LowSupply_IsHigherThanBase()
		{
			// Arrange
			var market = new Market();
			market.Inventory["fuel"] = 10; // Low supply vs IdealStock=50

			// Act
			int mid = market.GetMidPrice("fuel");

			// Assert
			Assert.That(mid, Is.GreaterThan(Market.BasePrice), "Mid price should be high when scarce.");
		}

		[Test]
		public void MidPrice_HighSupply_IsLowerThanBase()
		{
			// Arrange
			var market = new Market();
			market.Inventory["fuel"] = 100; // High supply vs IdealStock=50

			// Act
			int mid = market.GetMidPrice("fuel");

			// Assert
			Assert.That(mid, Is.LessThan(Market.BasePrice), "Mid price should be low when abundant.");
		}

		[Test]
		public void MidPrice_ZeroSupply_IsHigh()
		{
			// Arrange
			var market = new Market();
			// No inventory set => stock=0

			// Act
			int mid = market.GetMidPrice("gold");

			// Assert
			Assert.That(mid, Is.GreaterThan(Market.BasePrice), "Mid price should be high at zero stock.");
		}

		[Test]
		public void Spread_BuyPrice_IsGreaterThan_SellPrice()
		{
			// Arrange
			var market = new Market();
			market.Inventory["fuel"] = 50; // IdealStock => mid approx BasePrice

			// Act
			int buy = market.GetBuyPrice("fuel");
			int sell = market.GetSellPrice("fuel");
			int mid = market.GetMidPrice("fuel");

			// Assert
			Assert.That(buy, Is.GreaterThan(sell), "BuyPrice must exceed SellPrice due to spread.");
			Assert.That(buy, Is.GreaterThanOrEqualTo(mid), "BuyPrice should be >= mid.");
			Assert.That(sell, Is.LessThanOrEqualTo(mid), "SellPrice should be <= mid.");
		}

		[Test]
		public void Monotonicity_ScarcerStock_IncreasesAllPrices()
		{
			// Arrange
			var market = new Market();
			market.Inventory["fuel"] = 60;
			int midHighStock = market.GetMidPrice("fuel");
			int buyHighStock = market.GetBuyPrice("fuel");
			int sellHighStock = market.GetSellPrice("fuel");

			market.Inventory["fuel"] = 10;
			int midLowStock = market.GetMidPrice("fuel");
			int buyLowStock = market.GetBuyPrice("fuel");
			int sellLowStock = market.GetSellPrice("fuel");

			// Assert: prices rise as stock becomes scarce
			Assert.That(midLowStock, Is.GreaterThan(midHighStock));
			Assert.That(buyLowStock, Is.GreaterThan(buyHighStock));
			Assert.That(sellLowStock, Is.GreaterThan(sellHighStock));
		}
	}
}
