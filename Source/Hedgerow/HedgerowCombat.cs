using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    public class CompProperties_HedgerowCombat : CompProperties
    {
        public List<HedgerowGrowthStage> stages = new List<HedgerowGrowthStage>();

        public CompProperties_HedgerowCombat()
        {
            this.compClass = typeof(CompHedgerowCombat);
        }
    }

    public class CompHedgerowCombat : ThingComp
    {
        public int currentCharges = 0;

        public CompProperties_HedgerowCombat Props => (CompProperties_HedgerowCombat)this.props;

        // ПУБЛИЧНЫЙ ИНДЕКС (безопасно для использования растением)
        public int CurrentStageIndex
        {
            get
            {
                if (Props.stages.NullOrEmpty()) return 0;

                Plant plant = parent as Plant;
                float currentGrowth = plant != null ? plant.Growth : 1f;

                int activeIndex = 0;
                for (int i = 0; i < Props.stages.Count; i++)
                {
                    if (currentGrowth >= Props.stages[i].minGrowth)
                    {
                        activeIndex = i;
                    }
                }
                return activeIndex;
            }
        }

        // ПРИВАТНАЯ СТАДИЯ (защита от прямого чтения сырых данных)
        private HedgerowGrowthStage CurrentStage
        {
            get
            {
                if (Props.stages.NullOrEmpty()) return null;
                return Props.stages[CurrentStageIndex];
            }
        }

        // УМНЫЕ ГЕТТЕРЫ (Всегда отдают приоритет мутациям)
        public int MaxCharges => GetModdedIntValue(stage => stage.maxCharges, over => over.maxCharges);
        public int RechargeTicks => GetModdedIntValue(stage => stage.rechargeTicks, over => over.rechargeTicks);
        public float BaseTrapChance => GetModdedFloatValue(stage => stage.baseTrapChance, over => over.baseTrapChance);
        public float SpikeDamage => GetModdedFloatValue(stage => stage.spikeDamage, over => over.spikeDamage);
        public float PoisonSeverity => GetModdedFloatValue(stage => stage.poisonSeverity, over => over.poisonSeverity);
        public float FillPercent => GetModdedFloatValue(stage => stage.fillPercent, over => over.fillPercent);
        public int PathCost => GetModdedIntValue(stage => stage.pathCost, over => over.pathCost);

        private int GetModdedIntValue(System.Func<HedgerowGrowthStage, int> baseSelector, System.Func<HedgerowModOverride, int> modSelector)
        {
            var stage = CurrentStage;
            if (stage == null) return 0;

            int val = baseSelector(stage);
            var modComp = parent.TryGetComp<CompHedgerowModifier>();

            if (modComp != null && modComp.currentMod != HedgerowModType.None && stage.modOverrides != null)
            {
                foreach (var over in stage.modOverrides)
                {
                    if (over.modType == modComp.currentMod && modSelector(over) >= 0)
                    {
                        val = modSelector(over);
                        break;
                    }
                }
            }
            return val;
        }

        private float GetModdedFloatValue(System.Func<HedgerowGrowthStage, float> baseSelector, System.Func<HedgerowModOverride, float> modSelector)
        {
            var stage = CurrentStage;
            if (stage == null) return 0f;

            float val = baseSelector(stage);
            var modComp = parent.TryGetComp<CompHedgerowModifier>();

            if (modComp != null && modComp.currentMod != HedgerowModType.None && stage.modOverrides != null)
            {
                foreach (var over in stage.modOverrides)
                {
                    if (over.modType == modComp.currentMod && modSelector(over) >= 0f)
                    {
                        val = modSelector(over);
                        break;
                    }
                }
            }
            return val;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCharges, "currentCharges", 0);
        }

        public void CheckAndTriggerTrap(Pawn p)
        {
            if (currentCharges <= 0) return;
            var modComp = parent.GetComp<CompHedgerowModifier>();
            if (modComp == null || (modComp.currentMod != HedgerowModType.Spikes && modComp.currentMod != HedgerowModType.Poison))
                return;

            if (!IsValidTarget(p)) return;
            currentCharges--;

            float dodgeModifier = p.GetStatValue(StatDefOf.TrapSpringChance);

            // ИСПОЛЬЗУЕМ УМНЫЕ ГЕТТЕРЫ, А НЕ СЫРУЮ СТАДИЮ
            float finalChance = BaseTrapChance * dodgeModifier;

            if (Rand.Value > finalChance) return;

            if (modComp.currentMod == HedgerowModType.Spikes)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, SpikeDamage, 0f, -1f, parent);
                p.TakeDamage(dinfo);
            }
            else if (modComp.currentMod == HedgerowModType.Poison)
            {
                HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, PoisonSeverity);
            }
        }

        private bool IsValidTarget(Pawn p)
        {
            Faction mapOwner = parent.Map.ParentFaction;

            if (mapOwner == null)
            {
                if (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState)
                    return false;
                return true;
            }

            if (p.Faction == mapOwner || p.HostFaction == mapOwner) return false;
            if (p.Faction != null && !p.Faction.HostileTo(mapOwner)) return false;
            if (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) return false;

            return true;
        }

        public override string CompInspectStringExtra()
        {
            var modComp = parent.GetComp<CompHedgerowModifier>();
            if (modComp != null && (modComp.currentMod == HedgerowModType.Spikes || modComp.currentMod == HedgerowModType.Poison))
            {
                if (currentCharges <= 0) return $"Заряды ловушки: ИСТОЩЕНЫ (ждет дриаду)";
                return $"Заряды ловушки: {currentCharges} / {MaxCharges}";
            }
            return null;
        }
    }
}