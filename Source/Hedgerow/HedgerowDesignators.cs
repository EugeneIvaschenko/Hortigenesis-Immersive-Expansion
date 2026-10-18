using RimWorld;
using UnityEngine;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    public class Designator_PlaceHedgerow : Designator_HedgerowBase
    {
        protected override bool RequiresFertility => true; // Требует почву
        protected override string BlueprintDefName => "HedgerowProject_Blueprint";

        public Designator_PlaceHedgerow()
        {
            this.defaultLabel = "Проект: Буйный кустарник";
            this.defaultDesc = "Разместить проект для посадки дриадой.";
            this.icon = DefDatabase<ThingDef>.GetNamed("Plant_Bush").uiIcon;
        }
    }

    public class Designator_PlaceHedgerowStony : Designator_HedgerowBase
    {
        protected override bool RequiresFertility => false; // Растет на камнях!
        protected override string BlueprintDefName => "HedgerowStonyProject_Blueprint";

        public Designator_PlaceHedgerowStony()
        {
            this.defaultLabel = "Проект: Каменистый кустарник";
            this.defaultDesc = "Разместить проект для каменистой почвы.";
            this.icon = DefDatabase<ThingDef>.GetNamed("Plant_Bush").uiIcon;
        }
    }

    // 3. Инструмент разметки проектов (с проверкой на гниение)
    public abstract class Designator_HedgerowBase : Designator
    {

        protected abstract bool RequiresFertility { get; }
        protected abstract string BlueprintDefName { get; }

        // Валидация: можно ли поставить сюда проект? (Зеленая/Красная сетка)
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map)) return false;

            if (c.GetFirstThing(Map, ThingDef.Named("HedgerowProject_Blueprint")) != null)
            {
                return "Проект уже размещен.";
            }

            bool hasCultivatablePlant = false;
            foreach (Thing t in c.GetThingList(Map))
            {
                // Нельзя ставить поверх стен
                if (t.def.category == ThingCategory.Building && t.def.passability == Traversability.Impassable)
                {
                    return "Место занято постройкой.";
                }
                if (t is Plant p)
                {
                    if (p.def.defName == "Plant_Hedgerow" || p.def.defName == "Plant_HedgerowStony") return "Кустарник уже растет здесь.";
                    if (p.def.plant.IsTree) return "Дерево мешает посадке.";
                    if (p.def.plant.harvestedThingDef != null) hasCultivatablePlant = true;
                }
            }

            //Проверяем плодородие только если класс этого требует
            if (!hasCultivatablePlant && this.RequiresFertility)
            {
                if (Map.fertilityGrid.FertilityAt(c) <= 0f) return "Здесь ничего не вырастет.";
            }

            // ПРОВЕРКА НА ГНИЕНИЕ (как в TickLong)
            bool hasEmptyCell = false;
            bool hasArtificialNear = false;

            IntVec3[] dirs = GenAdj.CardinalDirections;
            for (int i = 0; i < dirs.Length; i++)
            {
                IntVec3 neighbor = c + dirs[i];
                if (!neighbor.InBounds(Map)) continue;

                bool isCellEmpty = true;
                List<Thing> things = neighbor.GetThingList(Map);
                for (int j = 0; j < things.Count; j++)
                {
                    Thing t = things[j];
                    if (t.def.category == ThingCategory.Building && t.def.building != null && !t.def.building.isNaturalRock)
                    {
                        hasArtificialNear = true;
                    }
                    if (t.def.category == ThingCategory.Building ||
                        t.def.passability == Traversability.Impassable ||
                        t is Plant_Hedgerow ||
                        t.def.defName == "Plant_HedgerowStony")
                    {
                        isCellEmpty = false;
                    }
                }
                if (isCellEmpty) hasEmptyCell = true;
            }

            // Если условия нарушены — возвращаем текст ошибки (игрок увидит его на экране)
            if (hasArtificialNear) return "Слишком близко к искусственным постройкам (куст сгниет).";
            if (!hasEmptyCell) return "Слишком тесно, нужно оставить хотя бы одну сторону пустой (куст сгниет).";

            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            // Спавним тот чертеж, имя которого указано в наследнике
            GenSpawn.Spawn(ThingDef.Named(this.BlueprintDefName), c, Map);
        }

        public override void SelectedUpdate()
        {
            GenDraw.DrawNoBuildEdgeLines();

            // Рисуем призрака только если мышь на карте (а не в меню)
            if (!ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUIInverted))
            {
                IntVec3 cell = UI.MouseCell();
                if (cell.InBounds(Map))
                {
                    // Движок сам возьмет нужный чертеж (обычный или каменистый) и отрисует его
                    GhostDrawer.DrawGhostThing(cell, Rot4.North, ThingDef.Named(this.BlueprintDefName), null, new Color(1f, 1f, 1f, 0.5f), AltitudeLayer.Blueprint);
                }
            }
        }
    }
}
