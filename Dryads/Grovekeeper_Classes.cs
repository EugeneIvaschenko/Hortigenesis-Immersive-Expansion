using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using HortigenesisIE.Hedgerow;

namespace HortigenesisIE.Dryads
{
    // 1. Свойства для XML
    public class CompProperties_Grovekeeper : CompProperties
    {
        public CompProperties_Grovekeeper()
        {
            this.compClass = typeof(CompGrovekeeper);
        }
    }

    // 2. Компонент, добавляющий кнопку (Gizmo) на дриаду
    public class CompGrovekeeper : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            if (parent.Faction == Faction.OfPlayer)
            {
                // Выдаем кнопку для обычной почвы
                yield return new Designator_PlaceHedgerow();

                // Выдаем кнопку для каменистой почвы
                yield return new Designator_PlaceHedgerowStony();
            }
        }
    }


}