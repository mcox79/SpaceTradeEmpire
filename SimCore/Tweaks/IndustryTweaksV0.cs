namespace SimCore.Tweaks
{
    // GATE.S4.INDU.MIN_LOOP.001
    // Numeric literal guard: all construction pipeline numeric tokens live in a Tweaks path.
    // IndustrySystem must reference only these constants to avoid introducing new literals in SimCore/Systems.
    public static class IndustryTweaksV0
    {
        public const int Zero = 0;
        public const int One = 1;

        public const int Stage0 = 0;
        public const int Stage1 = 1;

        public const string RecipeId = "CAP_MODULE_V0";

        public const string Stage0Name = "stage0_smelt_ore_to_plates";
        public const string Stage0InGood = "ore";
        public const int Stage0InQty = 10;
        public const int Stage0DurationTicks = 60;
        public const string Stage0OutGood = "plates";
        public const int Stage0OutQty = 5;

        public const string Stage1Name = "stage1_assemble_cap_module";
        public const string Stage1InGood = "plates";
        public const int Stage1InQty = 5;
        public const int Stage1DurationTicks = 120;
        public const string Stage1OutGood = "cap_module";
        public const int Stage1OutQty = 1;
    }
}
