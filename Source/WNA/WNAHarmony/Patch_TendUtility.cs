using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using WNA.ThingCompProp;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public class Patch_TendUtility
    {
        [HarmonyPrefix]
        public static bool prefix(Pawn doctor, Pawn patient, Medicine medicine)
        {
            if (medicine != null)
            {
                MultiUse compUse = medicine.TryGetComp<MultiUse>();
                if (compUse != null)
                {
                    if (!patient.health.HasHediffsNeedingTend())
                        return false;
                    if (medicine != null && medicine.Destroyed)
                    {
                        Log.Warning("Tried to use destroyed medicine.");
                        medicine = null;
                    }
                    float quality = TendUtility.CalculateBaseTendQuality(doctor, patient, medicine?.def);
                    List<Hediff> list = (List<Hediff>)typeof(TendUtility).GetField("tmpHediffsToTend", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                    TendUtility.GetOptimalHediffsToTendWithSingleTreatment(patient, medicine != null, list);
                    float maxQuality = medicine?.def.GetStatValueAbstract(StatDefOf.MedicalQualityMax) ?? 0.7f;
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Tended(quality, maxQuality, i);
                    }
                    if (doctor != null &&
                        doctor.Faction == Faction.OfPlayer &&
                        patient.Faction != doctor.Faction &&
                        !patient.IsPrisoner &&
                        patient.Faction != null)
                        patient.mindState.timesGuestTendedToByPlayer++;
                    if (doctor != null &&
                        doctor.RaceProps.Humanlike &&
                        patient.RaceProps.Animal &&
                        patient.RaceProps.playerCanChangeMaster &&
                        RelationsUtility.TryDevelopBondRelation(doctor, patient, 0.004f) &&
                        doctor.Faction != null &&
                        doctor.Faction != patient.Faction)
                        InteractionWorker_RecruitAttempt.DoRecruit(doctor, patient, useAudiovisualEffects: false);
                    patient.records.Increment(RecordDefOf.TimesTendedTo);
                    doctor?.records.Increment(RecordDefOf.TimesTendedOther);
                    if (doctor == patient && !doctor.Dead)
                    {
                        doctor.mindState.Notify_SelfTended();
                    }
                    if (medicine != null)
                    {
                        if (compUse.Count > 1)
                            compUse.Count--;
                        else if (medicine.stackCount > 1)
                        {
                            medicine.stackCount--;
                            compUse.Count = compUse.Props.uses;
                        }
                        else if (!medicine.Destroyed)
                            medicine.Destroy();
                    }
                    if (ModsConfig.IdeologyActive && doctor?.Ideo != null)
                    {
                        Precept_Role role = doctor.Ideo.GetRole(doctor);
                        if (role?.def.roleEffects != null)
                            foreach (RoleEffect roleEffect in role.def.roleEffects)
                                roleEffect.Notify_Tended(doctor, patient);
                    }
                    if (doctor != null && doctor.Faction == Faction.OfPlayer && doctor != patient)
                        QuestUtility.SendQuestTargetSignals(patient.questTags, "PlayerTended", patient.Named("SUBJECT"));
                    return false;
                }
            }
            return true;
        }
    }
}
