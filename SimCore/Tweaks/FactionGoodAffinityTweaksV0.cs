using SimCore.Content;
using System.Collections.Generic;

namespace SimCore.Tweaks;

// GATE.X.MARKET_PRICING.AFFINITY_WIRE.001: Per-(faction, good) price modifier in basis points.
// Encodes the pentagon dependency ring as economic differentiation:
// - Supplier factions sell their export good cheaper (negative bps = discount).
// - Consumer factions pay more for their import need (positive bps = surcharge).
// Creates natural trade lanes along the pentagon ring.
public static class FactionGoodAffinityTweaksV0
{
	// Primary pentagon ring affinities (large effect).
	public const int SupplierExportBps = -1500;  // -15% on export specialty
	public const int ConsumerImportBps = 800;    // +8% on import need

	// Secondary thematic affinities (smaller effect).
	public const int SecondarySupplierBps = -500; // -5% secondary export
	public const int SecondaryConsumerBps = 500;  // +5% secondary import
	public const int MinorAffinityBps = 300;      // +3% minor affinity

	private static readonly Dictionary<(string FactionId, string GoodId), int> AffinityTable = new()
	{
		// ── Pentagon ring: each faction exports one good and imports another ──
		// Concord exports Food, imports Composites
		{ (FactionTweaksV0.ConcordId, WellKnownGoodIds.Food), SupplierExportBps },
		{ (FactionTweaksV0.ConcordId, WellKnownGoodIds.Composites), ConsumerImportBps },

		// Weavers export Composites, import Electronics
		{ (FactionTweaksV0.WeaversId, WellKnownGoodIds.Composites), SupplierExportBps },
		{ (FactionTweaksV0.WeaversId, WellKnownGoodIds.Electronics), ConsumerImportBps },

		// Chitin exports Electronics, imports Rare Metals
		{ (FactionTweaksV0.ChitinId, WellKnownGoodIds.Electronics), SupplierExportBps },
		{ (FactionTweaksV0.ChitinId, WellKnownGoodIds.RareMetals), ConsumerImportBps },

		// Valorin exports Rare Metals, imports Exotic Crystals
		{ (FactionTweaksV0.ValorinId, WellKnownGoodIds.RareMetals), SupplierExportBps },
		{ (FactionTweaksV0.ValorinId, WellKnownGoodIds.ExoticCrystals), ConsumerImportBps },

		// Communion exports Exotic Crystals, imports Food
		{ (FactionTweaksV0.CommunionId, WellKnownGoodIds.ExoticCrystals), SupplierExportBps },
		{ (FactionTweaksV0.CommunionId, WellKnownGoodIds.Food), ConsumerImportBps },

		// ── Secondary affinities (thematic flavor) ──
		// Valorin: military supplier — munitions cheaper
		{ (FactionTweaksV0.ValorinId, WellKnownGoodIds.Munitions), SecondarySupplierBps },

		// Chitin: tech manufacturer — components cheaper
		{ (FactionTweaksV0.ChitinId, WellKnownGoodIds.Components), SecondarySupplierBps },

		// Concord: agricultural base — organics cheaper
		{ (FactionTweaksV0.ConcordId, WellKnownGoodIds.Organics), SecondarySupplierBps },

		// Communion: scarce spiritual economy — salvaged tech costs more
		{ (FactionTweaksV0.CommunionId, WellKnownGoodIds.SalvagedTech), SecondaryConsumerBps },

		// Weavers: raw material consumer for weaving — ore slightly more expensive
		{ (FactionTweaksV0.WeaversId, WellKnownGoodIds.Ore), MinorAffinityBps },
	};

	/// <summary>
	/// Returns the affinity modifier in basis points for a faction+good pair.
	/// Negative = discount (faction produces this good), positive = surcharge (faction needs this good).
	/// Returns 0 for unmapped pairs (no affinity).
	/// </summary>
	public static int GetAffinityBps(string factionId, string goodId)
	{
		if (string.IsNullOrEmpty(factionId) || string.IsNullOrEmpty(goodId)) return 0; // STRUCTURAL: null guard
		return AffinityTable.TryGetValue((factionId, goodId), out int bps) ? bps : 0; // STRUCTURAL: default
	}
}
