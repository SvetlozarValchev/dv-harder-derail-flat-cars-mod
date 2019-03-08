using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;
using System.Reflection.Emit;

namespace HarderDerailFlatCarsMod
{
    public class Main
    {
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // Something
            return true; // If false the mod will show an error.
        }
    }

    [HarmonyPatch(typeof(Bogie))]
    [HarmonyPatch("FixedUpdate")]
    public class Bogie_FixedUpdate_Patcher
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);
            
            int insertionIndex = -1;

            Label labelnext = il.DefineLabel();
            Label labelSetValue = il.DefineLabel();
            byte position = 0;

            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Ldfld && (FieldInfo)code[i].operand == AccessTools.Field(typeof(Bogie), "simManager") &&
                    code[i + 1].opcode == OpCodes.Ldfld && (FieldInfo)code[i + 1].operand == AccessTools.Field(typeof(SimManager), "derailBuildUpThreshold"))
                {
                    insertionIndex = i + 3;

                    code[i + 3].labels.Add(labelnext);

                    position = (byte)((LocalBuilder)code[i + 2].operand).LocalIndex;
                }
            }

            if (insertionIndex != -1)
            {
                var instructionsToInsert = new List<CodeInstruction>();
                var setValue = new CodeInstruction(OpCodes.Ldc_R4, 3.5f);
                setValue.labels.Add(labelSetValue);

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Bogie), "Car").GetGetMethod()));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TrainCar), "carType")));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldc_I4, 200));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Beq_S, labelSetValue));

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Bogie), "Car").GetGetMethod()));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TrainCar), "carType")));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldc_I4, 201));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Bne_Un_S, labelnext));

                instructionsToInsert.Add(setValue);
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Stloc_S, position));

                code.InsertRange(insertionIndex, instructionsToInsert);
            }

            return code;
        }
    }
}