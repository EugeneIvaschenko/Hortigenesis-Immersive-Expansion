using RimWorld;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    public class Plant_Hedgerow : Plant
    {
        // Переменные для хранения унаследованного урожая
        public ThingDef inheritedHarvestDef;
        public float inheritedHarvestYield;
        private HedgerowGrowthStage cachedStage;

        public override float GrowthRate
        {
            get
            {
                // Если здоровье меньше максимального — рост останавливается
                if (this.HitPoints < this.MaxHitPoints)
                {
                    return 0f;
                }

                // Иначе возвращаем стандартный расчет RimWorld
                return base.GrowthRate;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref inheritedHarvestDef, "inheritedHarvestDef");
            Scribe_Values.Look(ref inheritedHarvestYield, "inheritedHarvestYield", 0f);
        }

        public float CustomFillPercent
        {
            get
            {
                var combatComp = this.TryGetComp<CompHedgerowCombat>();
                var modComp = this.TryGetComp<CompHedgerowModifier>();

                if (combatComp?.CurrentStage == null) return this.def.fillPercent;

                var stage = combatComp.CurrentStage;
                float fill = stage.fillPercent;

                if (modComp != null && stage.modOverrides != null)
                {
                    foreach (var over in stage.modOverrides)
                    {
                        if (over.modType == modComp.currentMod && over.fillPercent >= 0f)
                        {
                            fill = over.fillPercent;
                            break;
                        }
                    }
                }
                return fill;
            }
        }

        public int CustomPathCost
        {
            get
            {
                var combatComp = this.TryGetComp<CompHedgerowCombat>();
                var modComp = this.TryGetComp<CompHedgerowModifier>();

                if (combatComp?.CurrentStage == null) return this.def.pathCost;

                var stage = combatComp.CurrentStage;
                int cost = stage.pathCost;

                if (modComp != null && stage.modOverrides != null)
                {
                    foreach (var over in stage.modOverrides)
                    {
                        if (over.modType == modComp.currentMod && over.pathCost >= 0)
                        {
                            cost = over.pathCost;
                            break;
                        }
                    }
                }
                return cost;
            }
        }

        public override void PlantCollected(Pawn by, PlantDestructionMode plantDestructionMode)
        {
            if (inheritedHarvestDef != null && this.Growth >= 0.65f)
            {
                // 1. Спавним унаследованные ягоды/лекарства
                int yieldAmount = GenMath.RoundRandom(this.Growth * inheritedHarvestYield);
                if (yieldAmount > 0)
                {
                    Thing thing = ThingMaker.MakeThing(inheritedHarvestDef);
                    thing.stackCount = yieldAmount;
                    GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near);

                    if (by != null && by.records != null)
                    {
                        by.records.AddTo(RecordDefOf.PlantsHarvested, 1f);
                    }
                }

                // 2. Удаляем свойство урожая навсегда
                inheritedHarvestDef = null;
                inheritedHarvestYield = 0f;

                // 3. Снимаем метку сбора
                Designation des = Map.designationManager.DesignationOn(this, DesignationDefOf.HarvestPlant);
                des?.Delete();

                // 4. Обновляем графику
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);

                // Прерываем базовый метод, чтобы куст не исчез/не откатился к ростку
                return;
            }

            base.PlantCollected(by, plantDestructionMode);
        }

        public override string Label
        {
            get
            {
                string baseLabel = base.Label;
                CompHedgerowModifier comp = this.TryGetComp<CompHedgerowModifier>();

                if (comp != null && comp.currentMod != HedgerowModType.None)
                {
                    switch (comp.currentMod)
                    {
                        case HedgerowModType.Spikes: return baseLabel + " (шипы)";
                        case HedgerowModType.Poison: return baseLabel + " (яд)";
                        case HedgerowModType.Juicy: return "сочный " + baseLabel;
                        case HedgerowModType.Barrier: return "барьерный " + baseLabel;
                    }
                }

                if (inheritedHarvestDef != null)
                {
                    return baseLabel + " (" + inheritedHarvestDef.label + ")";
                }

                return baseLabel;
            }
        }

        // Тернарное выражение вместо лишних if
        public override bool HarvestableNow => inheritedHarvestDef != null ? this.Growth >= 0.65f : base.HarvestableNow;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (inheritedHarvestDef != null && this.HarvestableNow)
            {
                Designation harvestDes = Map.designationManager.DesignationOn(this, DesignationDefOf.HarvestPlant);

                yield return new Command_Toggle
                {
                    defaultLabel = "Собрать урожай",
                    defaultDesc = $"Отметить кустарник для сбора урожая ({inheritedHarvestDef.label}) фермерами.",
                    icon = inheritedHarvestDef.uiIcon,
                    isActive = () => harvestDes != null,
                    toggleAction = () =>
                    {
                        if (harvestDes == null)
                        {
                            Map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.HarvestPlant));
                        }
                        else
                        {
                            harvestDes.Delete();
                        }
                    }
                };
            }
        }

        public override string GetInspectString()
        {
            string str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                str += "\n";
            }

            CompHedgerowModifier comp = this.TryGetComp<CompHedgerowModifier>();
            if (comp != null && comp.currentMod == HedgerowModType.Barrier)
            {
                str += "Проходимость: Экстремальное замедление";
            }
            else
            {
                str += "Проходимость: Сильное замедление";
            }

            return str;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Регистрируем куст на этой карте
            if (!HedgerowMapTracker.bushCounts.ContainsKey(map.uniqueID))
            {
                HedgerowMapTracker.bushCounts[map.uniqueID] = 0;
            }
            HedgerowMapTracker.bushCounts[map.uniqueID]++;
            UpdateCachedStage();
        }

        public override void TickLong()
        {
            base.TickLong(); // Здесь ванильный код прибавляет Growth
            CheckForStageChange(); // А здесь мы проверяем, не перешли ли мы порог
        }

        // Наш кастомный триггер смены стадии
        private void CheckForStageChange()
        {
            if (!this.Spawned) return;

            var comp = this.TryGetComp<CompHedgerowCombat>();
            if (comp == null) return;

            var currentStage = comp.CurrentStage;

            // Если стадия изменилась (перешли порог роста)
            if (cachedStage != currentStage)
            {
                cachedStage = currentStage;

                // Пинаем сетку проходимости для обновления кэша пути
                this.Map.pathing.RecalculatePerceivedPathCostAt(this.Position);

                // В будущем сюда можно добавить эффекты (звук роста, пыль и т.д.)
            }
        }

        private void UpdateCachedStage()
        {
            var comp = this.TryGetComp<CompHedgerowCombat>();
            if (comp != null)
            {
                cachedStage = comp.CurrentStage;
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = this.Map;
            base.DeSpawn(mode);

            // Убираем куст из статистики карты
            if (map != null && HedgerowMapTracker.bushCounts.ContainsKey(map.uniqueID))
            {
                HedgerowMapTracker.bushCounts[map.uniqueID]--;
            }
        }
    }
}