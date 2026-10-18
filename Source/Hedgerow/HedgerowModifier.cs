using RimWorld;
using UnityEngine;
using Verse;

namespace HortigenesisIE.Hedgerow
{
    public enum HedgerowModType { None, Spikes, Poison, Juicy, Barrier }

    // Класс для хранения точных значений из XML
    public class ModStatProfile
    {
        public HedgerowModType modType;
        public string label;
        public string description;
        public string iconPath; // Путь к текстуре в папке Textures
        public float coverEffectiveness = -1f; // -1 значит не трогать укрытие
        public List<StatModifier> statOverrides = new List<StatModifier>();
    }

    public class CompProperties_HedgerowModifier : CompProperties
    {
        public List<ModStatProfile> profiles = new List<ModStatProfile>();

        public int mutateTicks = 300; // 5 секунд

        public CompProperties_HedgerowModifier()
        {
            compClass = typeof(CompHedgerowModifier);
        }
    }

    // Этот класс теперь хранит только текущий статус и кнопки
    public class CompHedgerowModifier : ThingComp
    {
        public HedgerowModType currentMod = HedgerowModType.None;
        // Модификация, которую игрок заказал, но дриада еще не применила
        public HedgerowModType targetMod = HedgerowModType.None;
        public CompProperties_HedgerowModifier Props => (CompProperties_HedgerowModifier)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentMod, "currentMod", HedgerowModType.None);
            Scribe_Values.Look(ref targetMod, "targetMod", HedgerowModType.None);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // СИНХРОНИЗАЦИЯ: Если ванильная кнопка "Отмена" удалила метку, сбрасываем нашу цель
            DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("Designation_HedgerowModification");
            if (targetMod != HedgerowModType.None && parent.Map.designationManager.DesignationOn(parent, desDef) == null)
            {
                targetMod = HedgerowModType.None;
            }
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;

            if (Props.profiles.NullOrEmpty()) yield break;

            foreach (ModStatProfile profile in Props.profiles)
            {
                Command_Action btn = new Command_Action();

                // Загружаем иконку по строковому пути из XML. 
                // ContentFinder.Get работает как с ванильными путями, так и с папкой Textures твоего мода.
                if (!profile.iconPath.NullOrEmpty())
                {
                    btn.icon = ContentFinder<Texture2D>.Get(profile.iconPath, true);
                }
                else
                {
                    btn.icon = BaseContent.BadTex; // Заглушка, если забыл указать иконку
                }

                HedgerowModType type = profile.modType;

                // Логика подписей статуса
                if (targetMod == type)
                {
                    btn.defaultLabel = profile.label + " (Заказано)";
                    btn.defaultDesc = profile.description + "\n\nНажмите еще раз, чтобы ОТМЕНИТЬ работу дриады.";
                }
                else if (currentMod == type && targetMod == HedgerowModType.None)
                {
                    btn.defaultLabel = profile.label + " (Активно)";
                    btn.defaultDesc = profile.description + "\n\nЭта модификация уже применена.";
                }
                else
                {
                    btn.defaultLabel = profile.label;
                    btn.defaultDesc = profile.description + "\n\nНажмите, чтобы дриада-хранительница начала работу.";
                }

                // Логика нажатия (работает через целевую метку, как мы обсуждали)
                btn.action = delegate
                {
                    DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("Designation_HedgerowModification");

                    if (targetMod == type)
                    {
                        targetMod = HedgerowModType.None;
                        Designation existingDes = parent.Map.designationManager.DesignationOn(parent, desDef);
                        if (existingDes != null)
                        {
                            parent.Map.designationManager.RemoveDesignation(existingDes);
                        }
                        return;
                    }

                    if (currentMod == type && targetMod == HedgerowModType.None) return;

                    targetMod = type;
                    if (parent.Map.designationManager.DesignationOn(parent, desDef) == null)
                    {
                        parent.Map.designationManager.AddDesignation(new Designation(parent, desDef));
                    }
                };

                yield return btn;
            }
        }

        public override string CompInspectStringExtra()
        {
            string text = "";

            if (currentMod != HedgerowModType.None)
            {
                // Ищем нормальное русское название из профиля
                var profile = Props.profiles.Find(p => p.modType == currentMod);
                string label = profile != null ? profile.label : currentMod.ToString();
                text += $"Модификация: {label}";
            }

            if (targetMod != HedgerowModType.None)
            {
                if (text != "") text += "\n";
                text += "Ожидается работа дриады...";
            }

            return text != "" ? text : null;
        }
    }
}