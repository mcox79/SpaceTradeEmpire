namespace SimCore.Tweaks
{
    // GATE.S4.CATALOG.EPIC_CLOSE.001: market initialization knobs for base world generation.
    // Only goods requiring universal key seeding live here.
    // food is intentionally excluded: food is a production good requiring agricultural node profiles.
    // Do NOT add food here — seeding it universally at genesis undermines market class distinctness.
    public static class CatalogTweaksV0
    {
        public const int FuelInitialStock  = 500; // Bootstrap supply: prevents economy stall before fuel wells ramp up.
        public const int MetalInitialStock = 0;   // Key presence only; metal must be refined from ore at smelters.
        public const int OreInitialStock   = 0;   // Key presence only; ore must be mined at ore deposits.
    }
}
