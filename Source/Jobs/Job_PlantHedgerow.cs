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

                // Если чертежей такого типа на карте нет, защищаемся от ошибки
                if (blueprints == null) continue;

                foreach (Thing bp in blueprints)
                {
                    // Пропускаем, если игрок нажал "Запретить" (крестик)
                    if (bp.IsForbidden(pawn)) continue;

                    // ВАЖНО: Добавлена проверка pawn.CanReserve(bp). 
                    // Это предотвращает бронирование одного чертежа несколькими дриадами.
                    if (pawn.CanReserveAndReach(bp.Position, PathEndMode.Touch, Danger.Deadly) && pawn.CanReserve(bp))
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
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. Защита от ошибок: прерываем работу, если чертеж исчез или был запрещен
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // 2. Идем к чертежу (TargetA)
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // 3. Получаем деф создаваемого объекта из чертежа
            ThingDef plantDef = null;
            if (TargetA.Thing is Blueprint blueprint)
            {
                plantDef = blueprint.def.entityDefToBuild as ThingDef;
            }

            // Если по какой-то причине это не чертеж, используем фоллбэк
            if (plantDef == null)
            {
                plantDef = ThingDef.Named("Plant_Hedgerow");
            }

            // 4. Достаем наши настройки из XML (DefModExtension)
            var stats = plantDef.GetModExtension<DefModExt_HedgerowStats>();

            // Если расширение забыли прописать в XML, используем безопасные значения по умолчанию
            int workTicks = stats != null ? stats.plantWorkTicks : 300;
            float growth = stats != null ? stats.initialGrowth : 0.05f;

            // 5. Процесс посадки (пешка стоит на месте с полоской прогресса)
            Toil plantToil = new Toil();
            plantToil.defaultCompleteMode = ToilCompleteMode.Delay;
            plantToil.defaultDuration = workTicks; // Используем значение из XML
            plantToil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return plantToil;

            // 6. Завершение работы: удаляем чертеж и спавним сам куст
            Toil finalizePlanting = new Toil();
            finalizePlanting.initAction = delegate
            {
                // Создаем куст на клетке чертежа
                Thing plant = GenSpawn.Spawn(plantDef, TargetA.Cell, Map, WipeMode.Vanish);

                // Устанавливаем стартовый рост из XML
                if (plant is Plant p)
                {
                    p.Growth = growth;
                }

                // Удаляем чертеж
                TargetA.Thing?.Destroy(DestroyMode.Vanish);
            };
            finalizePlanting.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return finalizePlanting;
        }
    }
}