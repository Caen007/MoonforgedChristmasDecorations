using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    // DISABLED:
    // This patch was causing HarmonyX errors on InventoryGui::DoCrafting (void method patch issue)
    // and did not reliably produce craft SFX/VFX.
    // Craft effects are now handled by wiring CraftingStation effect lists on M_Wrapping_Table.
    internal static class WrappingPaperCraftFX
    {
    }
}
