using Verse;

namespace HortigenesisIE.Hedgerow
{
    // Класс для точечного переопределения статов при конкретной модификации
    public class HedgerowModOverride
    {
        public HedgerowModType modType = HedgerowModType.None;

        // Значение -1 означает "взять базовое значение стадии"
        public float fillPercent = -1f;
        public int pathCost = -1;
        public float baseTrapChance = 0f;
        public float spikeDamage = -1f;
        public float poisonSeverity = -1f;
        public int maxCharges = -1;
        public int rechargeTicks = -1;
    }

    // Класс стадии с новыми полями
    public class HedgerowGrowthStage
    {
        public float minGrowth = 0f;

        public float baseTrapChance = 0f;
        public float spikeDamage = 0f;
        public float poisonSeverity = 0f;
        public int maxCharges = 0;
        public int rechargeTicks = 300;

        // Базовые параметры укрытия и проходимости для этой стадии (если нет мутации)
        public float fillPercent = 0f;
        public int pathCost = 0;

        // Список переопределений
        public List<HedgerowModOverride> modOverrides = new List<HedgerowModOverride>();
    }
}