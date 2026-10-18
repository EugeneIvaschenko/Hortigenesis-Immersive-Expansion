using HortigenesisIE.Hedgerow;
using RimWorld;
using Verse;
using Verse.AI;

namespace HortigenesisIE.Jobs
{
    // 1. ИИ-узел: Ищет проекты и выдает приказ
    public class JobGiver_PlantHedgerow : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.kindDef.defName != "HortigenesisIE_Dryad_Grovekeeper") return null;
            if (pawn.Faction != Faction.OfPlayer) return null;

            DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("Designation_HedgerowProject", false);
            if (desDef == null)
            {
                Log.Error("Hortigenesis: Designation_HedgerowProject не найден в DefDatabase!");
                return null;
            }

            // Ищем не метки, а наши объекты-чертежи
            Thing closestBP = null;
            float closestDist = 99999f;

            // 1. Создаем список имен всех наших чертежей
            string[] blueprintDefs = { "HedgerowProject_Blueprint", "HedgerowStonyProject_Blueprint" };

            // 2. Проходимся по каждому типу чертежа
            foreach (string defName in blueprintDefs)
            {
                var blueprints = pawn.Map.listerThings.ThingsOfDef(ThingDef.Named(defName));

                // Если чертежей такого типа на карте нет, listerThings может вернуть null, защищаемся от ошибки
                if (blueprints == null) continue;

                foreach (Thing bp in blueprints)
                {
                    // Пропускаем, если игрок нажал "Запретить" (крестик)
                    if (bp.IsForbidden(pawn)) continue;

                    if (pawn.CanReserveAndReach(bp.Position, PathEndMode.Touch, Danger.Deadly))
                    {
                        float dist = pawn.Position.DistanceToSquared(bp.Position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestBP = bp;
                        }
                    }
                }
            }

            if (closestBP != null)
            {
                return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Job_PlantHedgerow"), closestBP);
            }
            return null;
        }
    }

    // 2. Исполнитель: Сам процесс работы дриады
    public class JobDriver_PlantHedgerow : JobDriver
    {
        private const int WorkTicks = 300; // Длительность работы дриады (5 сек)

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // Шаг 1: Идем к месту проекта
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            // Шаг 2: Процесс работы (заполняется полоска)
            Toil work = ToilMaker.MakeToil("WorkOnHedgerow");
            work.initAction = () => { };
            work.tickAction = () => { pawn.rotationTracker.FaceCell(TargetA.Cell); };
            work.defaultCompleteMode = ToilCompleteMode.Delay;
            work.defaultDuration = WorkTicks;
            work.WithProgressBarToilDelay(TargetIndex.A);
            work.PlaySustainerOrSound(() => SoundDefOf.Interact_Sow);
            yield return work;

            // Шаг 3: Мгновенное завершение работы и спавн объекта
            Toil finish = ToilMaker.MakeToil("FinishHedgerow");
            finish.initAction = () =>
            {
                Thing blueprint = this.TargetThingA;
                string bpName = blueprint.def.defName;
                IntVec3 cell = blueprint.Position;
                Map map = pawn.Map;

                blueprint.Destroy(DestroyMode.Vanish);

                // Защита от внезапных построек
                foreach (Thing t in cell.GetThingList(map))
                {
                    if (t.def.category == ThingCategory.Building || t.def.passability == Traversability.Impassable) return;
                }

                Plant oldPlant = cell.GetPlant(map);

                // Базовые значения для голой земли
                float inheritedGrowth = 0.05f;
                ThingDef harvestDef = null;
                float harvestYield = 0f;

                if (oldPlant != null && oldPlant.def.defName != "Plant_Hedgerow" && oldPlant.def.defName != "Plant_HedgerowStony")
                {
                    if (oldPlant.def.plant.IsTree) return; // Дублирующая защита для ИИ

                    // Наследуем рост В ЛЮБОМ СЛУЧАЕ, если это не дерево
                    inheritedGrowth = oldPlant.Growth;

                    // А вот урожай наследуем только если он есть
                    if (oldPlant.def.plant.harvestedThingDef != null)
                    {
                        harvestDef = oldPlant.def.plant.harvestedThingDef;
                        harvestYield = oldPlant.def.plant.harvestYield;
                    }

                    oldPlant.Destroy(DestroyMode.Vanish);
                }

                // Спавним финальный куст, узнав нужное имя через полиморфизм
                string plantToSpawn = (bpName == "HedgerowStonyProject_Blueprint") ? "Plant_HedgerowStony" : "Plant_Hedgerow";
                Plant_Hedgerow hedgerow = (Plant_Hedgerow)GenSpawn.Spawn(ThingDef.Named(plantToSpawn), cell, map);
                hedgerow.Growth = inheritedGrowth;
                hedgerow.inheritedHarvestDef = harvestDef;
                hedgerow.inheritedHarvestYield = harvestYield;
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}