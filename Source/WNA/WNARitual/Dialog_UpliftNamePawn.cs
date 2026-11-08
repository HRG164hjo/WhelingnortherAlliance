using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNARitual
{
    // Naming dialog of a completed uplifting. Besides the name fields of the vanilla
    // rename window it lets the player pick the xenotype whose endogenes the pawn takes.
    public class Dialog_UpliftNamePawn : Window
    {
        private const float PortraitSize = 128f;
        private const float HumanPortraitVerticalOffset = -18f;
        private const float RowHeight = 30f;
        private const float ButtonHeight = 35f;
        private const float LabelFieldGap = 4f;
        private const float FieldButtonGap = 17f;
        private const float VerticalGap = 17.5f;
        private const float HeaderGap = 4f;
        private const float RandomizeButtonWidth = 114f;
        private const float IconSize = 24f;

        private class NameField
        {
            public string label;
            public string text;
            public int maximumLength;
            public int nameIndex;
            public string controlName;
            public float labelWidth;
            public float fieldWidth;
        }

        private readonly Pawn pawn;
        private readonly List<NameField> fields = new List<NameField>();
        private readonly List<XenotypeDef> xenotypes = new List<XenotypeDef>();
        private readonly string renameText;
        private readonly string descriptionText;
        private readonly string genderText;
        private readonly Rot4 portraitDirection;
        private readonly string xenotypeLabel;
        private readonly string xenotypeTooltip;
        private readonly float labelWidth;
        private readonly float fieldWidth;
        private XenotypeDef selectedXenotype;
        private Vector2 size;
        private bool firstCall = true;

        public override Vector2 InitialSize => size;

        public Dialog_UpliftNamePawn(Pawn pawn, XenotypeDef defaultXenotype)
        {
            this.pawn = pawn;
            selectedXenotype = defaultXenotype ?? pawn.genes?.Xenotype;

            if (pawn.RaceProps.Humanlike)
            {
                renameText = "RenamePerson".Translate().CapitalizeFirst();
                portraitDirection = Rot4.South;
            }
            else
            {
                renameText = "RenameAnimal".Translate().CapitalizeFirst();
                portraitDirection = Rot4.East;
            }
            descriptionText = pawn.KindLabelIndefinite().CapitalizeFirst();
            genderText = $"{"Gender".Translate().CapitalizeFirst()}: {pawn.GetGenderLabel().CapitalizeFirst()}";

            NameTriple triple = pawn.Name as NameTriple;
            if (triple != null)
            {
                fields.Add(NewField("FirstName", "FirstName", 0, triple.First, 12));
                fields.Add(NewField("NickName", "NickName", 1, triple.NickSet ? triple.Nick : "", 16));
                fields.Add(NewField("LastName", "LastName", 2, triple.Last, 12));
            }
            else
            {
                string current = pawn.Name == null ? "" : pawn.Name.ToStringShort;
                fields.Add(NewField("NickName", "NickName", 1, current, 16));
            }
            xenotypeLabel = "WNA_UpliftXenotype".Translate().CapitalizeFirst() + ":";
            xenotypeTooltip = "WNA_UpliftXenotypeTooltip".Translate();

            labelWidth = Mathf.Ceil(Text.CalcSize(xenotypeLabel).x);
            for (int i = 0; i < fields.Count; i++)
            {
                labelWidth = Mathf.Max(labelWidth, fields[i].labelWidth);
                fieldWidth = Mathf.Max(fieldWidth, fields[i].fieldWidth);
            }
            foreach (NameField field in fields)
            {
                field.labelWidth = labelWidth;
                field.fieldWidth = fieldWidth;
            }

            foreach (XenotypeDef def in DefDatabase<XenotypeDef>.AllDefsListForReading)
            {
                if (def != null)
                    xenotypes.Add(def);
            }
            xenotypes.Sort((a, b) => b.displayPriority.CompareTo(a.displayPriority));

            float contentHeight = PortraitSize + HeaderGap + fields.Count * RowHeight + 2f * VerticalGap + RowHeight + ButtonHeight;
            size = new Vector2(2f * Margin + labelWidth + fieldWidth + FieldButtonGap + RandomizeButtonWidth,
                2f * Margin + contentHeight);
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = true;
            resizeable = false;
            doCloseX = false;
        }

        private static NameField NewField(string controlName, string labelKey, int nameIndex, string current, int maximumLength)
        {
            NameField field = new NameField();
            field.controlName = controlName;
            field.nameIndex = nameIndex;
            field.text = current;
            field.maximumLength = maximumLength;
            field.label = labelKey.Translate().CapitalizeFirst() + ":";
            field.labelWidth = Mathf.Ceil(Text.CalcSize(field.label).x);
            field.fieldWidth = Mathf.Ceil(Text.CalcSize(new string('W', maximumLength + 2)).x);
            return field;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, PortraitSize);
            DrawPortrait(header);

            float y = header.yMax + HeaderGap;
            for (int i = 0; i < fields.Count; i++)
            {
                DrawNameField(new Rect(inRect.x, y, inRect.width, RowHeight), fields[i]);
                y += RowHeight;
            }
            y += VerticalGap;

            DrawXenotypeField(new Rect(inRect.x, y, inRect.width, RowHeight));
            y += RowHeight + VerticalGap;

            Rect footer = new Rect(inRect.x, y, inRect.width, ButtonHeight);
            float buttonWidth = Mathf.Floor((footer.width - FieldButtonGap) / 2f);
            if (Widgets.ButtonText(new Rect(footer.x, footer.y, buttonWidth, ButtonHeight), "Cancel".Translate().CapitalizeFirst()))
            {
                Close();
                return;
            }
            if (Widgets.ButtonText(new Rect(footer.xMax - buttonWidth, footer.y, buttonWidth, ButtonHeight), "Accept".Translate().CapitalizeFirst()))
                Accept();

            if (Event.current.type == EventType.Layout && firstCall && fields.Count > 0)
            {
                GUI.FocusControl(fields[0].controlName);
                firstCall = false;
            }
        }

        private void DrawPortrait(Rect header)
        {
            RenderTexture image = PortraitsCache.Get(pawn, new Vector2(PortraitSize, PortraitSize), portraitDirection,
                default(Vector3), 1f, supersample: true, compensateForUIScale: true, renderHeadgear: true, renderClothes: true,
                null, null, stylingStation: false, PawnHealthState.Mobile);
            Rect portrait = new Rect(header.x, header.y + (pawn.RaceProps.Humanlike ? HumanPortraitVerticalOffset : 0f), PortraitSize, PortraitSize);
            GUI.DrawTexture(portrait, image);

            Rect info = new Rect(header.x + PortraitSize + FieldButtonGap, header.y,
                header.width - PortraitSize - FieldButtonGap, PortraitSize);
            float lineHeight = Text.CalcSize(renameText).y;
            Rect genderRect = new Rect(info.xMax - lineHeight, info.y, lineHeight, lineHeight);
            GUI.DrawTexture(genderRect, pawn.gender.GetIcon());
            TooltipHandler.TipRegion(genderRect, genderText);

            Text.Font = GameFont.Medium;
            Rect title = new Rect(info.x, info.y, info.width - lineHeight - LabelFieldGap, lineHeight);
            Widgets.Label(title, Text.ClampTextWithEllipsis(title, renameText));
            Text.Font = GameFont.Small;

            Rect description = new Rect(info.x, info.y + lineHeight + HeaderGap, info.width, info.height - lineHeight);
            Widgets.Label(description, descriptionText);
        }

        private void DrawNameField(Rect row, NameField field)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect label = new Rect(row.x, row.y, field.labelWidth, row.height);
            Widgets.Label(label, field.label);

            Rect input = new Rect(label.xMax + LabelFieldGap, row.y, field.fieldWidth, row.height);
            GUI.SetNextControlName(field.controlName);
            CharacterCardUtility.DoNameInputRect(input, ref field.text, field.maximumLength);

            Rect randomize = new Rect(input.xMax + FieldButtonGap, row.y, RandomizeButtonWidth, row.height);
            if (Widgets.ButtonText(randomize, "Randomize".Translate().CapitalizeFirst()))
                Randomize(field);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void Randomize(NameField field)
        {
            Name generated = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null, forceNoNick: false, selectedXenotype);
            if (generated is NameTriple triple)
            {
                field.text = triple[field.nameIndex];
            }
            else if (generated is NameSingle single)
            {
                field.text = single.Name;
            }
        }

        private void DrawXenotypeField(Rect row)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect label = new Rect(row.x, row.y, labelWidth, row.height);
            Widgets.Label(label, xenotypeLabel);

            Rect button = new Rect(label.xMax + LabelFieldGap, row.y, row.width - label.width - LabelFieldGap, row.height);
            bool clicked = Widgets.ButtonText(button, string.Empty);
            Rect icon = new Rect(button.x + 6f, button.y + (button.height - IconSize) * 0.5f, IconSize, IconSize);
            if (selectedXenotype != null && selectedXenotype.Icon != null)
                GUI.DrawTexture(icon, selectedXenotype.Icon);
            Rect text = new Rect(icon.xMax + 6f, button.y, button.width - (IconSize + 18f), button.height);
            Widgets.Label(text, Text.ClampTextWithEllipsis(text, XenotypeLabel(selectedXenotype)));
            TooltipHandler.TipRegion(button, CurXenotypeTooltip);
            if (clicked)
                OpenXenotypeMenu();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void OpenXenotypeMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>(xenotypes.Count);
            foreach (XenotypeDef xenotype in xenotypes)
            {
                XenotypeDef local = xenotype;
                options.Add(new FloatMenuOption(local.LabelCap, delegate
                {
                    selectedXenotype = local;
                }, local.Icon, Color.white, mouseoverGuiAction: delegate(Rect rect)
                {
                    TooltipHandler.TipRegion(rect, XenotypeTipOf(local));
                }));
            }
            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
        }

        private TaggedString CurXenotypeTooltip
        {
            get
            {
                if (selectedXenotype == null)
                    return xenotypeTooltip;
                return XenotypeTipOf(selectedXenotype) + "\n\n" + xenotypeTooltip;
            }
        }

        private static string XenotypeLabel(XenotypeDef xenotype)
        {
            if (xenotype == null)
                return "None".Translate().CapitalizeFirst();
            return xenotype.LabelCap;
        }

        private static TaggedString XenotypeTipOf(XenotypeDef xenotype)
        {
            if (xenotype == null)
                return "None".Translate();
            string description = xenotype.descriptionShort.NullOrEmpty() ? xenotype.description : xenotype.descriptionShort;
            if (description.NullOrEmpty())
                return xenotype.LabelCap;
            return xenotype.LabelCap + "\n\n" + description;
        }

        private void Accept()
        {
            Name name = BuildName();
            if (name != null)
            {
                if (!name.IsValid)
                {
                    Messages.Message("NameInvalid".Translate(), pawn, MessageTypeDefOf.NeutralEvent, historical: false);
                    return;
                }
                bool renamed = pawn.Name == null || pawn.Name.ToStringFull != name.ToStringFull;
                pawn.Name = name;
                pawn.babyNamingDeadline = -1;
                if (renamed)
                {
                    string nick = name is NameTriple triple ? triple.Nick : name.ToStringFull;
                    Messages.Message("PawnGainsName".Translate(nick, pawn.story?.Title, pawn.Named("PAWN")).AdjustedFor(pawn),
                        pawn, MessageTypeDefOf.PositiveEvent, historical: false);
                }
            }
            ApplyXenotype();
            Close();
        }

        private Name BuildName()
        {
            if (fields.Count == 3)
                return new NameTriple(fields[0].text?.Trim(), fields[1].text?.Trim(), fields[2].text?.Trim());
            if (fields.Count == 1)
                return new NameSingle(fields[0].text?.Trim());
            return null;
        }

        private void ApplyXenotype()
        {
            Pawn_GeneTracker genes = pawn.genes;
            if (genes == null || selectedXenotype == null)
                return;
            genes.SetXenotype(selectedXenotype);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            pawn.Notify_DisabledWorkTypesChanged();
        }
    }
}
