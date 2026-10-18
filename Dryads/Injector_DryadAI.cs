using HortigenesisIE.Jobs;
using Verse;
using Verse.AI;

namespace HortigenesisIE.Dryads
{
    [StaticConstructorOnStartup]
    public static class AI_Injector
    {
        static AI_Injector()
        {
            ThinkTreeDef dryadTree = DefDatabase<ThinkTreeDef>.GetNamed("Dryad", false);
            if (dryadTree != null && dryadTree.thinkRoot is ThinkNode_Priority priorityNode)
            {
                // Вставляем наш ИИ самым первым приоритетом (индекс 0)
                priorityNode.subNodes.Insert(0, new JobGiver_PlantHedgerow());
                priorityNode.subNodes.Insert(0, new JobGiver_MaintainHedgerow());
            }
        }
    }
}
