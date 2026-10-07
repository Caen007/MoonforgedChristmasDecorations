using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Moonforged.ChristmasDecorations
{
    /// <summary>
    /// Bridges the legacy Jotunn piece category to Valheim 1.0's usage-tag build menu.
    /// All Moonforged Christmas build pieces receive one custom usage tag so they are
    /// grouped under a single category in the new Categories view.
    /// </summary>
    internal static class MoonforgedBuildMenuCategory
    {
        private const string DefaultCategoryName = "Moonforged";

        // Valheim 1.0 currently uses bits 0-19. Custom build-menu tags use bits 20-30.
        // Start high to reduce collisions with frameworks that allocate custom tags upward from bit 20.
        private static readonly int[] CandidateBits =
        {
            29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 30
        };

        private static readonly HashSet<Piece> RegisteredPieces = new HashSet<Piece>();

        private static Piece.UsageTagFlags usageTag = (Piece.UsageTagFlags)(1 << 29);
        private static bool patchInstalled;

        internal static void Install(Harmony harmony)
        {
            if (patchInstalled || harmony == null)
                return;

            Type listType = AccessTools.TypeByName("ByUsagePieceList");
            if (listType == null)
                return;

            ConstructorInfo constructor =
                AccessTools.Constructor(listType, new[] { typeof(string) }) ??
                AccessTools.Constructor(listType, Type.EmptyTypes);

            MethodInfo postfix = AccessTools.Method(
                typeof(MoonforgedBuildMenuCategory),
                nameof(ByUsagePieceListConstructorPostfix));

            if (constructor == null || postfix == null)
                return;

            var harmonyPostfix = new HarmonyMethod(postfix)
            {
                priority = Priority.Last
            };

            harmony.Patch(constructor, postfix: harmonyPostfix);
            patchInstalled = true;
        }

        internal static void Register(Piece piece)
        {
            if (piece == null)
                return;

            RegisteredPieces.Add(piece);
            piece.m_usage = usageTag;
        }

        private static void ByUsagePieceListConstructorPostfix(object __instance)
        {
            if (__instance == null)
                return;

            Type listType = __instance.GetType();
            FieldInfo tagsField = AccessTools.Field(listType, "m_usageTags");
            FieldInfo namesField = AccessTools.Field(listType, "m_usageTagDisplayNames");

            if (tagsField == null || namesField == null)
                return;

            var tags = tagsField.GetValue(__instance) as Piece.UsageTagFlags[];
            var names = namesField.GetValue(__instance) as string[];

            if (tags == null || names == null || tags.Length != names.Length)
                return;

            string categoryName = GetCategoryName();

            Piece.UsageTagFlags existingTag;
            if (TryFindCategoryTag(tags, names, categoryName, out existingTag))
            {
                usageTag = existingTag;
                ApplyTagToRegisteredPieces();
                return;
            }

            if (TagIsAlreadyUsed(tags, usageTag))
            {
                Piece.UsageTagFlags freeTag;
                if (!TryChooseFreeTag(tags, out freeTag))
                    return;

                usageTag = freeTag;
            }

            ApplyTagToRegisteredPieces();

            string displayName = categoryName;

            int oldLength = tags.Length;
            Array.Resize(ref tags, oldLength + 1);
            Array.Resize(ref names, oldLength + 1);

            tags[oldLength] = usageTag;
            names[oldLength] = displayName;

            tagsField.SetValue(__instance, tags);
            namesField.SetValue(__instance, names);
        }

        private static string GetCategoryName()
        {
            if (MoonforgedChristmas.PlayerPreferredCategory == null)
                return DefaultCategoryName;

            string value = MoonforgedChristmas.PlayerPreferredCategory.Value;
            return string.IsNullOrWhiteSpace(value) ? DefaultCategoryName : value.Trim();
        }

        private static bool TryFindCategoryTag(
            Piece.UsageTagFlags[] tags,
            string[] names,
            string categoryName,
            out Piece.UsageTagFlags tag)
        {
            for (int i = 0; i < names.Length; ++i)
            {
                string existingName = names[i] ?? string.Empty;

                if (string.Equals(existingName, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    tag = tags[i];
                    return true;
                }
            }

            tag = 0;
            return false;
        }

        private static bool TagIsAlreadyUsed(Piece.UsageTagFlags[] tags, Piece.UsageTagFlags tag)
        {
            int rawTag = (int)tag;
            if (rawTag == 0)
                return true;

            for (int i = 0; i < tags.Length; ++i)
            {
                if ((((int)tags[i]) & rawTag) != 0)
                    return true;
            }

            return false;
        }

        private static bool TryChooseFreeTag(
            Piece.UsageTagFlags[] tags,
            out Piece.UsageTagFlags tag)
        {
            for (int i = 0; i < CandidateBits.Length; ++i)
            {
                int candidate = 1 << CandidateBits[i];
                bool used = false;

                for (int j = 0; j < tags.Length; ++j)
                {
                    if ((((int)tags[j]) & candidate) != 0)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    tag = (Piece.UsageTagFlags)candidate;
                    return true;
                }
            }

            tag = 0;
            return false;
        }

        private static void ApplyTagToRegisteredPieces()
        {
            foreach (Piece piece in RegisteredPieces)
            {
                if (piece != null)
                {
                    piece.m_usage = usageTag;
                }
            }
        }
    }
}
