using HortigenesisIE.Hedgerow;
using RimWorld;
using Verse;
using Verse.AI;

namespace HortigenesisIE.Jobs
{
    // 1. ИИ-УЗЕЛ: Ищет кусты, которым нужно обслуживание
    public class JobGiver_MaintainHedgerow : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.kindDef.defName != "HortigenesisIE_Dryad_Grovekeeper") return null;

            Thing closestBush = null;
            float closestDist = 99999f;

            // Ищем все кусты на карте (оба типа)
            List<Thing> allBushes = new List<Thing>();
            allBushes.AddRange(pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("Plant_Hedgerow")));
            allBushes.AddRange(pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("Plant_HedgerowStony")));

            DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("Designation_HedgerowModification");

            foreach (Thing bush in allBushes)
            {
                if (bush.IsForbidden(pawn)) continue;

                bool needsRepair = bush.HitPoints < bush.MaxHitPoints;
                bool needsRecharge = false;
                bool needsMutation = pawn.Map.designationManager.DesignationOn(bush, desDef) != null;

                var combatComp = bush.TryGetComp<CompHedgerowCombat>();
                if (combatComp != null && combatComp.currentCharges < combatComp.Props.maxCharges)
                {
                    needsRecharge = true;
                }

                // Если кусту нужно ХОТЬ ЧТО-ТО из этого списка
                if (needsRepair || needsRecharge || needsMutation)
                {
                    if (pawn.CanReserveAndReach(bush, PathEndMode.Touch, Danger.Deadly))
                    {
                        float dist = pawn.Position.DistanceToSquared(bush.Position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestBush = bush;
                        }
                    }
                }
            }

            if (closestBush != null)
            {
                // Не забудь добавить этот JobDef в твой XML!
                return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Job_MaintainHedgerow"), closestBush);
            }
            return null;
        }
    }

    // 2. ИСПОЛНИТЕЛЬ: Дриада чинит, заряжает и мутирует "не отходя от кассы"
    public class JobDriver_MaintainHedgerow : JobDriver
    {
        private float workProgress = 0f;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

            Toil maintain = ToilMaker.MakeToil("MaintainHedgerow");
            maintain.defaultCompleteMode = ToilCompleteMode.Never;
            maintain.tickAction = delegate
            {
                Pawn actor = maintain.actor;
                Thing bush = TargetThingA;
                actor.rotationTracker.FaceCell(bush.Position);

                var combatComp = bush.TryGetComp<CompHedgerowCombat>();
                var modComp = bush.TryGetComp<CompHedgerowModifier>();
                DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("Designation_HedgerowModification");
                Designation des = actor.Map.designationManager.DesignationOn(bush, desDef);

                // ПРИОРИТЕТ 1: ПОЧИНКА (1 HP за тик)
                if (bush.HitPoints < bush.MaxHitPoints)
                {
                    bush.HitPoints++;
                    return; // Ждем следующего тика
                }

                // ПРИОРИТЕТ 2: ПЕРЕЗАРЯДКА
                if (combatComp != null && combatComp.currentCharges < combatComp.Props.maxCharges)
                {
                    workProgress++;
                    if (workProgress >= combatComp.Props.rechargeTicks)
                    {
                        combatComp.currentCharges++;
                        workProgress = 0f;
                    }
                    return;
                }

                // ПРИОРИТЕТ 3: МУТАЦИЯ АПГРЕЙДА
                if (des != null && modComp != null && modComp.targetMod != HedgerowModType.None)
                {
                    workProgress++;
                    if (workProgress >= modComp.Props.mutateTicks)
                    {
                        // Применяем модификацию!
                        modComp.currentMod = modComp.targetMod;
                        modComp.targetMod = HedgerowModType.None;
                        des.Delete(); // Удаляем метку
                        workProgress = 0f;

                        // Если новая модификация - это шипы или яд, сразу даем полный магазин
                        combatComp?.currentCharges = combatComp.Props.maxCharges;
                    }
                    return;
                }

                // Если код дошел сюда, значит куст полностью цел, заряжен и мутирован.
                actor.jobs.curDriver.ReadyForNextToil();
            };

            yield return maintain;
        }
    }
}