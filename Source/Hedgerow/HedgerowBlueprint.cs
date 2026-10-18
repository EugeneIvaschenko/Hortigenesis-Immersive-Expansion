using RimWorld;
using UnityEngine;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    // Наш кастомный объект чертежа
    public class HedgerowBlueprint : ThingWithComps
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            yield return new Command_Action
            {
                defaultLabel = "CommandCancelConstructionLabel".Translate(),
                defaultDesc = "CommandCancelConstructionDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                hotKey = KeyBindingDefOf.Designator_Cancel,
                action = () => this.Destroy(DestroyMode.Cancel)
            };
        }
    }
}