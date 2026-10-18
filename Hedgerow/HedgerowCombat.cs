using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HortigenesisIE
{
    public class CompProperties_HedgerowCombat : CompProperties
    {
        public float baseTrapChance = 0.5f; // Базовый шанс 50%
        public float spikeDamage = 5f;
        public float poisonSeverity = 0.1f;

        public int maxCharges = 3;
        public int rechargeTicks = 300; // 5 секунд на восстановление одного заряда

        public CompProperties_HedgerowCombat()
        {
            compClass = typeof(CompHedgerowCombat);
        }
    }

    public class CompHedgerowCombat : ThingComp
    {
        public CompProperties_HedgerowCombat Props => (CompProperties_HedgerowCombat)props;

        // Текущее количество зарядов
        public int currentCharges;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                currentCharges = Props.maxCharges; // Полный магазин при постройке
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // Сохраняем заряды в сейв
            Scribe_Values.Look(ref currentCharges, "currentCharges", Props.maxCharges);
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
            float finalChance = Props.baseTrapChance * dodgeModifier;

            if (Rand.Value > finalChance) return;

            if (modComp.currentMod == HedgerowModType.Spikes)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, Props.spikeDamage, 0f, -1f, parent);
                p.TakeDamage(dinfo);
            }
            else if (modComp.currentMod == HedgerowModType.Poison)
            {
                HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, Props.poisonSeverity);
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
                return $"Заряды ловушки: {currentCharges} / {Props.maxCharges}";
            }

            return null;
        }
    }
}