using System.Collections.Generic;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    // Класс, хранящий статы для одной стадии роста
    public class HedgerowGrowthStage
    {
        public float minGrowth = 0f;
        public float baseTrapChance = 0f;
        public float spikeDamage = 0f;
        public float poisonSeverity = 0f;
        public int maxCharges = 0;
        public int rechargeTicks = 300;
    }
}