using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

namespace Moonforged.ChristmasDecorations
{
    public class RelicRegistration
    {
        public string PrefabName;
        public string DisplayName;
        public RequirementConfig[] Requirements;
        public string Description;
        public string Category;
        public int Comfort;
        public string CraftingStation;

        public RelicRegistration(
            string prefab, string display, RequirementConfig[] reqs, string desc, string cat,
            int comfort = 0, string craftingStation = "Workbench")
        {
            PrefabName = prefab;
            DisplayName = display;
            Requirements = reqs;
            Description = desc;
            Category = cat;
            Comfort = comfort;
            CraftingStation = craftingStation;
        }
    }

    public static class RelicRegistrar
    {
        private static bool wasAlreadyRegistered = false;
        private static bool _starRegistered = false;

        public static readonly List<RelicRegistration> AllRegistrations = new List<RelicRegistration>
{
    // ===================== TREES =====================

    // #1 (removed 5th: PineCone)
    new RelicRegistration("M_Christmas_Tree_1", "Town Square Christmas Tree", new[] {
        new RequirementConfig("Wood", 30),
        new RequirementConfig("Raspberry", 20),
        new RequirementConfig("Blueberries", 20),
        new RequirementConfig("Coal", 20)
    }, "Large town square Christmas Tree, craft a Star in the workbench and place it on the top.", "building", 0, "Workbench"),

    // #2 (removed 5th: PineCone)
    new RelicRegistration("M_Christmas_Tree_Small", "Small Christmas Tree", new[] {
        new RequirementConfig("Wood", 10),
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("Blueberries", 5),
        new RequirementConfig("Coal", 5)
    }, "A small Christmas Tree, craft a Star in the workbench and place it on the top.", "building", 0, "Workbench"),


    // ===================== GARLANDS, WREATH & MISTLETOE =====================

    // #3
    new RelicRegistration("M_Garland", "Green Christmas Garland", new[] {
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("Raspberry", 2)
    }, "A traditional Holiday decoration, you can link them with bows.", "building", 0, "Workbench"),

    // #4
    new RelicRegistration("M_Garland_White", "White Christmas Garland", new[] {
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("Blueberries", 2)
    }, "A traditional Holiday decoration, you can link them with bows.", "building", 0, "Workbench"),

    // #5
    new RelicRegistration("M_Garland_Spiral_Green", "Christmas Spiral Garland (Green)", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("PineCone", 1)
    }, "They can be used arround pillars and poles.", "building", 0, "Workbench"),

    // #6
    new RelicRegistration("M_Garland_Spiral_White", "Christmas Spiral Garland (White)", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("PineCone", 1)
    }, "They can be used arround pillars and poles.", "building", 0, "Workbench"),

    // #7
    new RelicRegistration("M_Christmas_Wreath", "Christmas Wreath", new[] {
        new RequirementConfig("FineWood", 1),
        new RequirementConfig("FirCone", 4),
        new RequirementConfig("Raspberry", 10)
    }, "Brings a traditional feeling in your homes.", "building", 0, "Workbench"),

    // #8
    new RelicRegistration("M_mistletoe", "Christmas Mistletoe", new[] {
        new RequirementConfig("FineWood", 1),
        new RequirementConfig("JuteRed", 1)
    }, "Kiss your partner or the troll under it.", "building", 0, "Workbench"),


    // ===================== LIGHTS & CHASERS =====================

    // #9
    new RelicRegistration("Christmas_Lights1", "Christmas Star Lights", new[] {
        new RequirementConfig("Crystal", 5)
    }, "Nice Twinkly Star shaped Lights.", "building", 0, "Workbench"),

    // #10
    new RelicRegistration("Christmas_Lights2", "Christmas Snowflake Lights", new[] {
        new RequirementConfig("Crystal", 5)
    }, "Nice Twinkly Snowflake shaped Lights", "building", 0, "Workbench"),

    // #11
    new RelicRegistration("MChristmas_Lights1", "Christmas Lights", new[] {
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("Blueberries", 5),
        new RequirementConfig("Coal", 5)
    }, "Nice Twinkly Lights", "building", 0, "Workbench"),

    // #12
    new RelicRegistration("MChristmas_LongLights1", "Christmas Long Lights", new[] {
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("Blueberries", 5),
        new RequirementConfig("Coal", 5)
    }, "Nice Twinkly Lights", "building", 0, "Workbench"),

    // #13
    new RelicRegistration("M_Icicle_Lamp", "Icicle Christmas Lights", new[] {
        new RequirementConfig("Crystal", 5)
    }, "Glowing icicle light string that drips with blue light.", "building", 0, "Workbench"),

        // #13.1
    new RelicRegistration("MultiStar_Light", "Star Lights", new[] {
        new RequirementConfig("Crystal", 5)
    }, "Glowing Multi Star lights.", "building", 0, "Workbench"),

            // #13.2
    new RelicRegistration("8Meter_Town_Light", "8 Meter Town Square Light", new[] {
        new RequirementConfig("Crystal", 5)
    }, "Town Square Star Light Decoration.", "building", 0, "Workbench"),


    // ===================== SNOWFLAKES (HANGING / CRYSTAL) =====================

    // #14
    new RelicRegistration("M_Snowflake", "Christmas Hanging Snowflake Model I", new[] {
        new RequirementConfig("Crystal", 1)
    }, "A Shiny Hanging Snowflake.", "building", 0, "Workbench"),

    // #15
    new RelicRegistration("M_Snowflake2", "Christmas Hanging Snowflake Model II", new[] {
        new RequirementConfig("Crystal", 1)
    }, "A Shiny Hanging Snowflake.", "building", 0, "Workbench"),

    // #16
    new RelicRegistration("M_Snowflake3", "Christmas Hanging Snowflake Model III", new[] {
        new RequirementConfig("Crystal", 1)
    }, "A Shiny Hanging Snowflake.", "building", 0, "Workbench"),

    // #17
    new RelicRegistration("M_Snowflake4", "Christmas Hanging Snowflake Model IV", new[] {
        new RequirementConfig("Crystal", 1)
    }, "A Shiny Hanging Snowflake.", "building", 0, "Workbench"),


    // ===================== New addons =====================

    // #18

    // #19

    // #20

    // #21

    // #22

    // #23

        // ===================== BOWS =====================

    // #24

        new RelicRegistration("M_Golden_Bow", "Golden Christmas Bow", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Blueberries", 2)
    }, "A nice Bow to connect your garlands.", "building", 0, "Workbench"),

    // #25
    new RelicRegistration("M_BigblueChristmasbow", "Big Blue Christmas Bow", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Blueberries", 2)
    }, "A nice Bow to connect your garlands.", "building", 0, "Workbench"),

    // #26
    new RelicRegistration("M_BigredChristmasbow", "Big Red Christmas Bow", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Bloodbag", 1)
    }, "A nice Bow to connect your garlands.", "building", 0, "Workbench"),


    // ===================== SWEETS & DRINKS (CUPS, COOKIES, CAKES, WINE) =====================

    // #27
    new RelicRegistration("M_Cozy_Yule_Cup", "Cozy Yule Hot Cocoa Cup", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Coal", 1)
    }, "", "building", 0, "Workbench"),

    // #28
    new RelicRegistration("M_Cozy_Candy_Cane_Cup", "Hot Cocoa Cup with a Candy Cane", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Coal", 1)
    }, "", "building", 0, "Workbench"),

    // #29
    new RelicRegistration("Christmas_Cups3", "Hot Cocoa Cup with a Chocolate Tree", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Coal", 1)
        }, "", "building", 0, "Workbench"),

    // #30
    new RelicRegistration("M_MilkandCookiesforSanta", "Milk and Cookies for Santa", new[] {
        new RequirementConfig("Resin", 1),
        new RequirementConfig("Coal", 1)
    }, "", "building", 0, "Workbench"),

    // #31
    new RelicRegistration("M_Gingerbread_Man", "Gingerbread Man", new[] {
        new RequirementConfig("Wood", 1)
    }, "We finaly caught him!", "building", 0, "Workbench"),

    // #32
    new RelicRegistration("M_Christmas_Cake", "Christmas Cake on a Plate", new[] {
        new RequirementConfig("Stone", 1),
        new RequirementConfig("Blueberries", 5),
        new RequirementConfig("BarleyFlour", 1)
    }, "Don`t eat it all, leave some for Santa.", "building", 0, "Workbench"),

    // #33
    new RelicRegistration("M_Christmas_Cake2", "Christmas Strawberry Cake on a Plate", new[] {
        new RequirementConfig("Stone", 1),
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("BarleyFlour", 1)
    }, "Don`t eat it all, leave some for Santa.", "building", 0, "Workbench"),

    // #34
    new RelicRegistration("M_YuleLogCake", "Yule Log Cake", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("BarleyFlour", 1)
    }, "Is not a poop is a cake.", "building", 0, "Workbench"),

    // #34.2
        new RelicRegistration("M_Christmas_Celebration_Cake", "Christmas Celebration Cake", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Raspberry", 5),
        new RequirementConfig("BarleyFlour", 1)
    }, "A cake.", "building", 0, "Workbench"),


    // #35
    new RelicRegistration("M_Christmas_Wine", "Bottle of Wine", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Stone", 1),
        new RequirementConfig("Raspberry", 5)
    }, "Moonforged Christmas Home made Wine for you all.", "building", 0, "Workbench"),


    // ===================== CANDY CANES (1 m) =====================

    // #36
    new RelicRegistration("M_Red_Candy_Cane_1m", "Red Candy Cane", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Raspberry", 1),
        new RequirementConfig("Coal", 1)
    }, "Candy Cane decorations.", "building", 0, "Workbench"),

    // #37
    new RelicRegistration("M_Green_Candy_Cane_1m", "Green Candy Cane", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Dandelion", 1)
    }, "Candy Cane decorations.", "building", 0, "Workbench"),

    // #38
    new RelicRegistration("M_RedGreen_Candy_Cane_1m", "Red & Green Candy Cane", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Raspberry", 1),
        new RequirementConfig("Dandelion", 1)
    }, "Candy Cane decorations.", "building", 0, "Workbench"),

    // #39
    new RelicRegistration("M_Green_Red_Candy_Cane_1m", "Green & Red Candy Cane", new[] {
        new RequirementConfig("Wood", 1),
        new RequirementConfig("Raspberry", 1),
        new RequirementConfig("Dandelion", 1)
    }, "Candy Cane decorations.", "building", 0, "Workbench"),

        // ===================== STOCKINGS & SNOWMAN =====================

    // #40
    new RelicRegistration("M_Christmas_Stocking", "Green Christmas Stocking", new[] {
        new RequirementConfig("LeatherScraps", 1),
        new RequirementConfig("Bloodbag", 1)
    }, "", "building", 0, "Workbench"),

    // #41
    new RelicRegistration("M_Christmas_Stocking_2", "Red Christmas Stocking", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),

        // #41.0
    new RelicRegistration("M_Christmas_Stocking_3", "Blue Christmas Stocking", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),
        
    // #41.1
    new RelicRegistration("M_Mitten_Left", "Left hand Mitten", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),

        // #41.2
    new RelicRegistration("M_Mitten_Right", "Right hand Mitten", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),

        // #41.4
    new RelicRegistration("M_Mitten_Left_Red", "Left hand Mitten Red", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),

        // #41.3
    new RelicRegistration("M_Mitten_Right_Red", "Right hand Mitten Red", new[] {
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),


    // #42
    new RelicRegistration("M_Snowman", "Snowman", new[] {
        new RequirementConfig("Wood", 3),
        new RequirementConfig("Coal", 5),
        new RequirementConfig("Carrot", 1)
    }, "", "building", 0, "Workbench"),


    // ===================== SANTA & FRIENDS (FIGURINES, BAG, BOOT, CARVINGS) =====================

    // #43
    new RelicRegistration("M_SantaClaus", "Santa Claus", new[] {
        new RequirementConfig("FineWood", 10),
        new RequirementConfig("LeatherScraps", 10),
        new RequirementConfig("Raspberry", 10),
        new RequirementConfig("Coal", 5)
    }, "Saint Nicholas", "building", 0, "Workbench"),

    // #44
    new RelicRegistration("M_Small_Santa_Claus", "Small Santa Claus Figurine", new[] {
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("LeatherScraps", 3),
        new RequirementConfig("Raspberry", 3),
        new RequirementConfig("Coal", 1)
    }, "He`s always watching, what a creep.", "building", 0, "Workbench"),

    // #45
    new RelicRegistration("M_SantaBag", "Santa’s Bag of Gifts", new[] {
        new RequirementConfig("Wood", 2),
        new RequirementConfig("LeatherScraps", 10)
    }, "Wonder whats inside?", "building", 0, "Workbench"),

    // #46
    new RelicRegistration("M_boot", "Santa’s Boot of Gifts and Flowers", new[] {
        new RequirementConfig("Wood", 2),
        new RequirementConfig("Raspberry", 2)
    }, "You can put gifts or flowers inside the boot.", "building", 0, "Workbench"),

    // #47
    new RelicRegistration("M_Nutcracker", "Nutcracker Figurine", new[] {
        new RequirementConfig("FineWood", 1)
    }, "Wooden figurine with a hinged mouth for cracking nuts.", "building", 0, "Workbench"),

    // #48

    // #49


    // ===================== DEER, SLED & TRAIN =====================

    // #50
    new RelicRegistration("M_Deer", "Santa’s Reindeers", new[] {
        new RequirementConfig("DeerMeat", 4),
        new RequirementConfig("DeerHide", 4)
    }, "Dasher, Dancer, Prancer, Vixen, Comet, Cupid, Donner, Blitzen", "building", 0, "Workbench"),

    // #51
    new RelicRegistration("M_Deer_Rudy", "Rudolph the Red-Nosed Reindeer", new[] {
        new RequirementConfig("DeerMeat", 4),
        new RequirementConfig("DeerHide", 4),
        new RequirementConfig("Raspberry", 1)
    }, "A very shiny nose. You might even say it glows.", "building", 0, "Workbench"),

    // #52
    new RelicRegistration("M_Christmas_Sled", "Santa’s Sled", new[] {
        new RequirementConfig("Wood", 40),
        new RequirementConfig("BronzeNails", 20),
        new RequirementConfig("Bronze", 5)
    }, "Sanat`s Christmas Sled, you can sit in it.", "building", 0, "Forge"),

    // #53
    new RelicRegistration("M_Christmas_Train", "Christmas Train", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Iron", 1)
    }, "A small train that runs around its track and you can ride it round and round.", "building", 0, "Forge"),


    // ===================== BANNERS & THRONE =====================

    // #54 (removed 5th: Coal)
    new RelicRegistration("M_Christmas_Banner_1", "Santa`s Christmas Banner I", new[] {
        new RequirementConfig("LeatherScraps", 6),
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("Raspberry", 2),
        new RequirementConfig("Guck", 1)
    }, "Reindeers under the mistletoe", "building", 0, "Workbench"),

    // #55 (removed 5th: Coal)
    new RelicRegistration("M_Christmas_Banner_2", "Santa`s Christmas Banner II", new[] {
        new RequirementConfig("LeatherScraps", 6),
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("Raspberry", 2),
        new RequirementConfig("Guck", 1)
    }, "Jingle bells", "building", 0, "Workbench"),

    // #56
    new RelicRegistration("M_Christmas_Banner_4", "Santa`s Christmas Banner IV", new[] {
        new RequirementConfig("LeatherScraps", 6),
        new RequirementConfig("FineWood", 2),
        new RequirementConfig("Raspberry", 2),
        new RequirementConfig("Coal", 1)
    }, "Reindeers under the evening star.", "building", 0, "Workbench"),

    // #57
    new RelicRegistration("M_Bench", "Santa`s Christmas Bench", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Iron", 2)
    }, "", "building", 0, "Forge"),

    // #58
    new RelicRegistration("M_Christmas_Throne", "Santa`s Christmas Throne", new[] {
        new RequirementConfig("FineWood", 20),
        new RequirementConfig("BronzeNails", 10),
        new RequirementConfig("JuteRed", 2)
    }, "Sit on Santa`s lap and tell him what you want for christmas.", "building", 0, "Forge"),



    // ===================== STAR (CRAFTED ITEM) =====================

    // #59
    new RelicRegistration("M_Star", "Evening Star", new[] {
        new RequirementConfig("Crystal", 2)
    }, "Can be placed on top of the Christmas tree", "building", 0, "Workbench"),


    // ===================== REGIONAL =====================

    // #60
    new RelicRegistration("Tio_de_Nadal", "Tió de Nadal", new[] {
        new RequirementConfig("RoundLog", 2),
        new RequirementConfig("LeatherScraps", 2),
        new RequirementConfig("Raspberry", 1)
    }, "", "building", 0, "Workbench"),

    
    // #61
    new RelicRegistration("M_icicles", "An Icicle.", new[] {
        new RequirementConfig("Crystal", 1)
    }, "Dripping ice decoration.", "building", 0, "Workbench"),

                // #62
    new RelicRegistration("ChirstmasMozaic_1", "Snowman Stained-Glass Window.", new[] {
        new RequirementConfig("Crystal", 1),
        new RequirementConfig("FineWood", 4)
    }, "", "building", 0, "Workbench"),

                            // #63
    new RelicRegistration("ChirstmasMozaic_2", "Santa`s Stained-Glass Window.", new[] {
        new RequirementConfig("Crystal", 1),
        new RequirementConfig("FineWood", 4)
    }, "", "building", 0, "Workbench"),

                                // #64
    new RelicRegistration("ChristmasHouseMozaic", "Christmas Home Stained-Glass Window.", new[] {
        new RequirementConfig("Crystal", 1),
        new RequirementConfig("FineWood", 4)
    }, "", "building", 0, "Workbench"),

    // #65

    new RelicRegistration("M_Wrapping_Table", "Christmas Wrapping Table.", new[] {
         new RequirementConfig("Wood", 5),
         new RequirementConfig("FineWood", 5)

    }, "From this table you can create christmas gifts and decorations.", "building", 0, ""),

    // #66
    new RelicRegistration("M_Box", "Christmas Music Box", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Bronze", 1)
    }, "A hand-crafted wind-up Christmas music box.", "building", 0, "Workbench"),

                // #67
    new RelicRegistration("M_Sled", "Christmas Wooden Sled Decoration", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Bronze", 1)
    }, ".", "building", 0, "Workbench"),

                    // #68
    new RelicRegistration("M_Elf", "Santa`s Little Helper", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Bronze", 1)
    }, ".", "building", 0, "Workbench"),

            // #69
    new RelicRegistration("M_Elf2", "Santa`s Little Elfy", new[] {
        new RequirementConfig("FineWood", 5),
        new RequirementConfig("Bronze", 1)
    }, ".", "building", 0, "Workbench")

};

        public static IEnumerable<string> GetAllCategories() =>
            AllRegistrations.Select(r => CategoryToTab(r.Category)).Distinct();

        private static string CategoryToTab(string category)
        {
            var lower = category.ToLower();
            switch (lower)
            {
                case "building":
                    return MoonforgedChristmas.PlayerPreferredCategory.Value;
                default:
                    return category;
            }
        }

        public static void RegisterAllRelics(AssetBundle bundle)
        {
            if (wasAlreadyRegistered) return;

            GameObject musicBoxSoundPrefab = bundle != null
                ? bundle.LoadAsset<GameObject>("sfx_M_Box")
                : null;
            MusicBoxInteract.ConfigureSound(musicBoxSoundPrefab);

            GameObject trainSoundPrefab = bundle != null
                ? bundle.LoadAsset<GameObject>("sfx_M_Train")
                : null;
            TrainPivotRotator.ConfigureTrainSound(trainSoundPrefab);

            foreach (var reg in AllRegistrations) RegisterRelic(bundle, reg);

            // Wrapped gifts + wrapping paper are ITEMS (not pieces) and are registered separately
            WrappedGiftItemRegistrar.Register(bundle);
            WrappingPaperRegistrar.Register(bundle);

            wasAlreadyRegistered = true;
        }

        private static void RegisterRelic(AssetBundle bundle, RelicRegistration reg)
        {
            if (bundle == null) return;

            if (reg.PrefabName == "M_Star")
            {
                RegisterCraftedStar(bundle, reg);
                return;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(reg.PrefabName);
            if (prefab == null) return;
            prefab.name = reg.PrefabName;

            if (reg.PrefabName == "M_Wrapping_Table")
                prefab.AddComponent<WrappingBoxProxy>();


            // Deer scaling & marker
            if (reg.PrefabName == "M_Deer" || reg.PrefabName == "M_Deer_Rudy")
            {
                const float deerScale = 0.014f;
                prefab.transform.localScale = Vector3.one * deerScale;

                var enforcer = prefab.GetComponent<ScaleEnforcer>() ?? prefab.AddComponent<ScaleEnforcer>();
                enforcer.desiredScale = Vector3.one * deerScale;

                if (!prefab.GetComponent<DeerMarker>()) prefab.AddComponent<DeerMarker>();
                if (!prefab.GetComponent<ReindeerPetInteract>()) prefab.AddComponent<ReindeerPetInteract>();
            }

            // Tree lights
            if (reg.PrefabName == "M_Christmas_Tree_1")
            {
                var cycler = prefab.GetComponent<ChildLightsCycler>() ?? prefab.AddComponent<ChildLightsCycler>();
                cycler.lightRendererNames = new[] { "Light1", "Light2", "Light3" };
                cycler.intensity = 4.4f;
                cycler.stepSeconds = 1f;
            }

            // Light chasers
            if (reg.PrefabName == "Christmas_Lights1")
                ChristmasLightChaserInstaller.InstallOn(prefab, 1f, 4.4f);
            if (reg.PrefabName == "Christmas_Lights2")
                ChristmasLightChaserInstaller.InstallOn(prefab, 0.8f, 4.4f,
                    new Color[] { Color.white, Color.yellow }, ChristmasLightChaser.AnimationMode.BlinkAll);

            //experimental
            if (reg.PrefabName == "8Meter_Town_Light")
                ChristmasLightChaserInstaller.InstallOn(prefab, 0.8f, 4.4f,
                    new Color[] { Color.white, Color.yellow }, ChristmasLightChaser.AnimationMode.BlinkAll);


            if (reg.PrefabName == "MChristmas_Lights1" || reg.PrefabName == "MChristmas_LongLights1")
                ChristmasLightChaserInstaller.InstallOn(prefab, 1f, 4.4f,
                    new Color[] { Color.red, Color.yellow, Color.blue }, ChristmasLightChaser.AnimationMode.Chase);
            if (reg.PrefabName == "M_Garland_Spiral_Green")
                ChristmasLightChaserInstaller.InstallOn(prefab, 1f, 4.4f,
                    new Color[] { Color.red, Color.yellow, Color.blue }, ChristmasLightChaser.AnimationMode.Chase);
            if (reg.PrefabName == "M_Garland_Spiral_White")
                ChristmasLightChaserInstaller.InstallOn(prefab, 1f, 4.4f,
                    new Color[] { Color.red, Color.yellow, Color.blue }, ChristmasLightChaser.AnimationMode.Chase);
            // Icicle lamp – midpoint-staggered rain, freeze, drops, stars, and sparkles
            if (reg.PrefabName == "M_Icicle_Lamp")
            {
                var flow = prefab.GetComponent<IcicleLampFlow>() ?? prefab.AddComponent<IcicleLampFlow>();
                flow.columnNamePrefix = "Icicle_Lamp_";
                flow.bulbNamePrefix = "Light_";
                flow.raindropStepSeconds = 0.05f;
                flow.randomDropCount = 8;
                flow.pauseBetweenAnimations = 0.60f;
                flow.connectionDistance = 0.75f;
                flow.emissionIntensity = 4.5f;
                flow.dripColor = new Color(0.60f, 0.85f, 1.00f);
            }


            if (reg.PrefabName == "MultiStar_Light")
            {
                var flow = prefab.GetComponent<IcicleFlow>() ?? prefab.AddComponent<IcicleFlow>();
                flow.columnNamePrefix = "MultiStar_Light";
                flow.bulbNamePrefix = "Light";
                flow.dripStepSeconds = 0.35f;
                flow.pauseAfterColumn = 0.60f;
                flow.emissionIntensity = 4.5f;
                flow.dripColor = new Color(0.60f, 0.85f, 1.00f);
                flow.batchCount = 4;
                flow.batchSpacingSeconds = 4.0f;
                flow.autoDetectVerticalAxis = true;
            }

            // Interaction and same-prefab synchronization for animated lights.
            // M_Icicle_Lamp keeps its own IcicleLampFlow interaction menu.
            if (reg.PrefabName == "M_Christmas_Tree_1" ||
                reg.PrefabName == "Christmas_Lights1" ||
                reg.PrefabName == "Christmas_Lights2" ||
                reg.PrefabName == "MChristmas_Lights1" ||
                reg.PrefabName == "MChristmas_LongLights1" ||
                reg.PrefabName == "M_Garland_Spiral_Green" ||
                reg.PrefabName == "M_Garland_Spiral_White" ||
                reg.PrefabName == "MultiStar_Light" ||
                reg.PrefabName == "8Meter_Town_Light")
            {
                var lightControl = prefab.GetComponent<ConnectedLightControl>() ??
                                   prefab.AddComponent<ConnectedLightControl>();
                lightControl.prefabIdentity = reg.PrefabName;
                lightControl.connectionDistance = 0.35f;
                lightControl.connectionRefreshSeconds = 0.25f;
                lightControl.rapidBlinkSeconds = 0.03f;
            }

            // Sled ropes
            if (reg.PrefabName == "M_Christmas_Sled")
            {
                var reins = prefab.GetComponent<SledReinsConnector>() ?? prefab.AddComponent<SledReinsConnector>();
                reins.maxDeer = 9;
                reins.sledAnchorRootName = "RopeAttach";
                reins.deerAnchorName = "ReinAnchor";
                reins.useSingleStart = true;
                reins.verticalLift = 0f;
                reins.lateralSpacing = 0f;
                reins.forwardSpacing = 0f;
                reins.leadExtraForward = 0f;
                reins.ropeWidth = 0.018f;
                reins.ropeSag = 0.15f;
                reins.ropeSegments = 28;
                reins.windStrength = 0.25f;
                reins.jiggleAmplitude = 0.03f;
                reins.jiggleSpeed = 1.1f;
            }

            // Train orbit
            if (reg.PrefabName == "M_Christmas_Train")
                InstallTrainOrbit(prefab, degreesPerSecond: 18f, clockwise: false);

            // ===================== WRAPPING BOX SETUP =====================
            if (reg.PrefabName == "M_Wrapping_Table")
            {
                Transform box = prefab.transform.Find("Wrapping_Box");
                if (box != null)
                {
                    GameObject boxGO = box.gameObject;

                    // Preserve the existing prefab components.
                    // ZNetView and Container are supplied by the prefab.
                    if (boxGO.GetComponent<WrappingBoxProcessor>() == null)
                    {
                        boxGO.AddComponent<WrappingBoxProcessor>();
                    }
                }

                EnsureWrappingTableCraftEffects(prefab);
            }
            // =================== END WRAPPING BOX SETUP ===================

            // Network & piece setup
            var znv = prefab.GetComponent<ZNetView>() ?? prefab.AddComponent<ZNetView>();
            znv.m_persistent = true;
            znv.m_syncInitialScale = true;
            if (!prefab.GetComponent<ZSyncTransform>()) prefab.AddComponent<ZSyncTransform>();

            Piece piece = prefab.GetComponent<Piece>() ?? prefab.AddComponent<Piece>();
            piece.m_name = reg.DisplayName;
            piece.m_description = reg.Description;
            piece.m_groundOnly = false;

            if (StockingGiftStorage.IsStockingPrefabName(reg.PrefabName))
                StockingGiftStorage.Install(prefab);

            if (reg.PrefabName == "M_Box" && prefab.GetComponent<MusicBoxInteract>() == null)
                prefab.AddComponent<MusicBoxInteract>();

            Sprite icon = bundle.LoadAsset<Sprite>(reg.PrefabName);
            if (icon != null) piece.m_icon = icon;

            // ======== SOUND MAPPING ========
            var gifts = new HashSet<string>
            {
                "M_Gift_BlackOrange_Valheim",
                "M_Gift_Yellow_Deco",
                "M_Gift_Red_Blue",
                "M_SnowFlake_Blue",
                "M_SnowFlake_Red",
                "M_Garland",
                "M_Garland_White",
                "M_Garland_Spiral_Green",
                "M_Garland_Spiral_White",
                "M_Gingerbread_Man"
            };

            var cupsAndCakesCrystal = new HashSet<string>
            {
                "M_Cozy_Yule_Cup",
                "M_Cozy_Candy_Cane_Cup",
                "Christmas_Cups3",
                "M_Christmas_Cake",
                "M_Christmas_Cake2",
                "M_icicles",
                "M_Snowflake",
                "M_Snowflake2",
                "M_Snowflake3",
                "M_Snowflake4",
                "M_YuleLogCake"
            };

            var crystalExtras = new HashSet<string>
            {
                "M_BigredChristmasbow",
                "M_BigblueChristmasbow",
                "M_Small_Santa_Claus",
                "M_Christmas_Wine",
                "M_MilkandCookiesforSanta",
                "M_boot",
                "8Meter_Town_Light",
                "MultiStar_Light",
                "Christmas_Lights1",
                "Christmas_Lights2",
                "MChristmas_Lights1",
                "MChristmas_LongLights1",
                "M_Icicle_Lamp"
            };

            var metalCandyCanes = new HashSet<string>
            {
                "M_Red_Candy_Cane_1m",
                "M_Green_Candy_Cane_1m",
                "M_RedGreen_Candy_Cane_1m",
                "M_Green_Red_Candy_Cane_1m"
            };

            var metalOthers = new HashSet<string>
            {
                "M_Christmas_Sled",
                "M_Christmas_Train",
                "M_Bench"
            };

            string name = reg.PrefabName;

            GameObject vfxPlace = null, sfxPlace = ZNetScene.instance?.GetPrefab("sfx_build_hammer_default");
            GameObject destroyVFX = null, destroySFX = ZNetScene.instance?.GetPrefab("sfx_wood_destroyed");

            if (gifts.Contains(name))
            {
                sfxPlace = ZNetScene.instance?.GetPrefab("sfx_build_hammer_default");
                destroySFX = ZNetScene.instance?.GetPrefab("sfx_wood_destroyed");
            }
            else if (cupsAndCakesCrystal.Contains(name) || crystalExtras.Contains(name))
            {
                sfxPlace = ZNetScene.instance?.GetPrefab("sfx_build_hammer_crystal");
                destroySFX = ZNetScene.instance?.GetPrefab("sfx_clay_pot_break");
            }
            else if (metalCandyCanes.Contains(name))
            {
                vfxPlace = ZNetScene.instance?.GetPrefab("vfx_Place_stone");
                sfxPlace = ZNetScene.instance?.GetPrefab("sfx_build_hammer_metal");
                destroyVFX = ZNetScene.instance?.GetPrefab("vfx_destroyed");
                destroySFX = ZNetScene.instance?.GetPrefab("sfx_metal_blocked");
            }
            else if (metalOthers.Contains(name))
            {
                vfxPlace = ZNetScene.instance?.GetPrefab("vfx_Place_stone");
                sfxPlace = ZNetScene.instance?.GetPrefab("sfx_build_hammer_metal");
                destroyVFX = ZNetScene.instance?.GetPrefab("vfx_destroyed");
                destroySFX = ZNetScene.instance?.GetPrefab("sfx_metal_blocked");
            }

            var placeFX = new EffectList();
            var placeArr = new List<EffectList.EffectData>();
            if (vfxPlace != null) placeArr.Add(new EffectList.EffectData { m_prefab = vfxPlace, m_enabled = true });
            if (sfxPlace != null) placeArr.Add(new EffectList.EffectData { m_prefab = sfxPlace, m_enabled = true });
            placeFX.m_effectPrefabs = placeArr.ToArray();
            piece.m_placeEffect = placeFX;

            var wear = prefab.GetComponent<WearNTear>() ?? prefab.AddComponent<WearNTear>();
            wear.m_health = Mathf.Max(wear.m_health, 1000f);
            wear.m_noRoofWear = true;

            var destroyFX = new EffectList();
            var destroyArr = new List<EffectList.EffectData>();
            if (destroyVFX != null) destroyArr.Add(new EffectList.EffectData { m_prefab = destroyVFX, m_enabled = true });
            if (destroySFX != null) destroyArr.Add(new EffectList.EffectData { m_prefab = destroySFX, m_enabled = true });
            destroyFX.m_effectPrefabs = destroyArr.ToArray();
            wear.m_destroyedEffect = destroyFX;

            // Jötunn piece registration
            var config = new PieceConfig
            {
                PieceTable = "Hammer",
                Category = CategoryToTab(reg.Category),
                CraftingStation = reg.CraftingStation,
                Requirements = reg.Requirements
            };

            PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, config));
        }

        private static void EnsureWrappingTableCraftEffects(GameObject wrappingTablePrefab)
        {
            if (wrappingTablePrefab == null) return;

            var station = wrappingTablePrefab.GetComponent<CraftingStation>() ?? wrappingTablePrefab.AddComponent<CraftingStation>();
            station.m_name = "Wrapping Table";


            GameObject sfxCraft =
                ZNetScene.instance?.GetPrefab("sfx_gui_craftitem_workbench");

            GameObject sfxDone =
                ZNetScene.instance?.GetPrefab("sfx_gui_craftitem_workbench_end");


            // Smoke VFX intentionally disabled
            GameObject vfxCraft = null;
            GameObject vfxDone = null;

            EffectList craft = MakeEffectList(vfxCraft, sfxCraft);
            EffectList done = MakeEffectList(vfxDone, sfxDone);

            TrySetEffectListField(station, "m_craftItemEffects", craft);
            TrySetEffectListField(station, "m_craftItemEffect", craft);

            TrySetEffectListField(station, "m_craftDoneEffects", done);
            TrySetEffectListField(station, "m_craftItemDoneEffects", done);
            TrySetEffectListField(station, "m_craftItemDoneEffect", done);

            TrySetEffectListField(station, "m_repairItemEffects", craft);
            TrySetEffectListField(station, "m_repairItemEffect", craft);

        }

        private static void TrySetEffectListField(object target, string fieldName, EffectList value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName) || value == null) return;

            var f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return;
            if (f.FieldType != typeof(EffectList)) return;

            f.SetValue(target, value);
        }

        private static EffectList MakeEffectList(GameObject vfx, GameObject sfx)
        {
            var list = new EffectList();
            var arr = new List<EffectList.EffectData>();

            if (vfx != null) arr.Add(new EffectList.EffectData { m_prefab = vfx, m_enabled = true });
            if (sfx != null) arr.Add(new EffectList.EffectData { m_prefab = sfx, m_enabled = true });

            list.m_effectPrefabs = arr.ToArray();
            return list;
        }

        private static void RegisterCraftedStar(AssetBundle bundle, RelicRegistration reg)
        {
            if (_starRegistered) return;

            GameObject prefab = bundle.LoadAsset<GameObject>(reg.PrefabName);
            if (prefab == null) return;
            prefab.name = reg.PrefabName;

            ZNetView znv = prefab.GetComponent<ZNetView>() ?? prefab.AddComponent<ZNetView>();
            znv.m_persistent = true;
            znv.m_syncInitialScale = true;

            if (!prefab.GetComponent<Rigidbody>())
            {
                Rigidbody rb = prefab.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.mass = 1f;
            }
            if (!prefab.GetComponent<Collider>())
            {
                SphereCollider sc = prefab.AddComponent<SphereCollider>();
                sc.isTrigger = false;
                sc.radius = 0.25f;
            }
            prefab.layer = LayerMask.NameToLayer("item");

            ItemDrop item = prefab.GetComponent<ItemDrop>();
            if (item == null) return;
            if (item.m_itemData == null || item.m_itemData.m_shared == null) return;

            var shared = item.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Trophy;

            Transform attach = prefab.transform.Find("attach");
            GameObject vis = null;
            if (attach != null)
            {
                Transform starT = attach.Find("Star");
                vis = starT != null ? starT.gameObject : attach.gameObject;
            }
            else
            {
                Transform starT = prefab.transform.Find("Star");
                vis = starT != null ? starT.gameObject : null;
            }
            if (vis != null && vis.GetComponent<RainbowGlow>() == null) vis.AddComponent<RainbowGlow>();

            item.m_itemData.m_dropPrefab = prefab;

            Sprite icon = bundle.LoadAsset<Sprite>(reg.PrefabName);
            if (icon != null) shared.m_icons = new Sprite[] { icon };
            if (!string.IsNullOrEmpty(reg.DisplayName)) shared.m_name = reg.DisplayName;
            if (!string.IsNullOrEmpty(reg.Description)) shared.m_description = reg.Description;

            ItemManager.Instance.AddItem(new CustomItem(prefab, fixReference: true));
            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig
            {
                Item = reg.PrefabName,
                Amount = 1,
                CraftingStation = string.IsNullOrEmpty(reg.CraftingStation) ? "Workbench" : reg.CraftingStation,
                Requirements = reg.Requirements
            }));

            _starRegistered = true;
        }

        private static void InstallTrainOrbit(GameObject prefab, float degreesPerSecond = 18f, bool clockwise = true)
        {
            var root = prefab.transform;

            Transform center = root.Find("TrackCenter");
            if (!center)
            {
                Vector3 centerPos = root.position;
                var track = root.Find("track") ?? root.Find("Track");
                if (track)
                {
                    var rs = track.GetComponentsInChildren<Renderer>(true);
                    if (rs.Length > 0)
                    {
                        var b = rs[0].bounds;
                        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                        centerPos = b.center;
                    }
                }
                var go = new GameObject("TrackCenter");
                center = go.transform;
                center.SetParent(root, true);
                center.position = centerPos;
                center.rotation = Quaternion.identity;
            }

            Transform pivot = center.Find("pivot") ?? center.Find("OrbitPivot");
            if (!pivot)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "pivot";
                pivot = ball.transform;
                pivot.SetParent(center, true);
                pivot.position = center.position;
                pivot.rotation = Quaternion.identity;
                pivot.localScale = Vector3.one * 0.05f;
                var col = ball.GetComponent<Collider>(); if (col) Object.Destroy(col);
            }

            var rot = prefab.GetComponent<TrainPivotRotator>() ?? prefab.AddComponent<TrainPivotRotator>();
            rot.ConfigurePivot(pivot);
            rot.degreesPerSecond = clockwise ? Mathf.Abs(degreesPerSecond) : -Mathf.Abs(degreesPerSecond);
        }

        // Keeps deer scale enforced at runtime.
        private class ScaleEnforcer : MonoBehaviour
        {
            public Vector3 desiredScale = Vector3.one;
            void Awake()
            {
                transform.localScale = desiredScale;
            }
        }
    }
}