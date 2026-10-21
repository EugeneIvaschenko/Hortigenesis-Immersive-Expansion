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

        // Умный геттер, который находит подходящую стадию в зависимости от текущего роста
        public HedgerowGrowthStage CurrentStage
        {
            get
            {
                if (Props.stages.NullOrEmpty()) return null;

                // Получаем доступ к растению
                Plant plant = parent as Plant;
                float currentGrowth = plant != null ? plant.Growth : 1f;

                // Ищем максимальную стадию, до которой дорос куст
                HedgerowGrowthStage activeStage = Props.stages[0];
                foreach (var stage in Props.stages)
                {
                    if (currentGrowth >= stage.minGrowth)
                    {
                        activeStage = stage;
                    }
                }
                return activeStage;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCharges, "currentCharges", 0);
        }

        public void CheckAndTriggerTrap(Pawn p)
        {
            if (currentCharges <= 0) return; // Ловушка пуста, ждем дриаду!
            var modComp = parent.GetComp<CompHedgerowModifier>();
            if (modComp == null || (modComp.currentMod != HedgerowModType.Spikes && modComp.currentMod != HedgerowModType.Poison))
                return;

            if (!IsValidTarget(p)) return;
            currentCharges--;

            float dodgeModifier = p.GetStatValue(StatDefOf.TrapSpringChance);
            float finalChance = CurrentStage.baseTrapChance * dodgeModifier;

            if (Rand.Value > finalChance) return;

            if (modComp.currentMod == HedgerowModType.Spikes)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, CurrentStage.spikeDamage, 0f, -1f, parent);
                p.TakeDamage(dinfo);
            }
            else if (modComp.currentMod == HedgerowModType.Poison)
            {
                HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, CurrentStage.poisonSeverity);
            }
        }

        private bool IsValidTarget(Pawn p)
        {
            Faction mapOwner = parent.Map.ParentFaction;

            // СЦЕНАРИЙ А: Нейтральная территория (Дикое поле, засада)
            if (mapOwner == null)
            {
                // Дикая флора щадит только мирных диких животных
                if (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState)
                    return false;

                // Все остальные (игрок, рейдеры, караваны, бешеные звери) получают урон
                return true;
            }

            // СЦЕНАРИЙ Б: Карта принадлежит кому-либо (Твоя база или база ИИ)

            // 1. Не бьем хозяев карты и их гостей/пленников
            if (p.Faction == mapOwner || p.HostFaction == mapOwner) return false;

            // 2. Не бьем тех, кто нейтрален или в союзе с хозяином карты
            if (p.Faction != null && !p.Faction.HostileTo(mapOwner)) return false;

            // 3. Не бьем мирных диких животных
            if (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) return false;

            return true;
        }

        public override string CompInspectStringExtra()
        {
            var modComp = parent.GetComp<CompHedgerowModifier>();

            // Показываем заряды только если включена боевая мутация
            if (modComp != null && (modComp.currentMod == HedgerowModType.Spikes || modComp.currentMod == HedgerowModType.Poison))
            {
                if (currentCharges <= 0)
                {
                    return $"Заряды ловушки: ИСТОЩЕНЫ (ждет дриаду)";
                }
                return $"Заряды ловушки: {currentCharges} / {CurrentStage.maxCharges}";
            }

            return null;
        }
    }
}