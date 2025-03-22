using Newtonsoft.Json.Linq;

static class JsonSchemas
{
    public static readonly Dictionary<string, List<(string name, string type, object defaultValue)>> Presets
        = new Dictionary<string, List<(string, string, object)>>()
    {
        {
            "Item",
            new List<(string name, string type, object defaultValue)>
            {
                ("name", "String", ""),
                ("damage", "Number", 0),
                ("rarity", "String", "normal"),
                ("color", "String", "white"),
                ("durability", "Number", 0),
                ("maxDurability", "Number", 0),
                ("consumable", "Boolean", false),
                ("faceMouse", "Boolean", false),
                ("projectile", "String", ""),
                ("useSound", "String", "snd_swing"),
                ("shootSound", "String", ""),
                ("frames", "Number", 1),
                ("animationSpeed", "Number", 1),
                ("xoffset", "Number", 0),
                ("yoffset", "Number", 0),
                ("castable", "Boolean", false),
                ("tile", "String", "noone"),
                ("placeable", "Boolean", false),
                ("type", "String", "material"),
                ("typeName", "String", ""),
                ("damageType", "String", "melee"),
                ("damageTypeName", "String", ""),
                ("damageBoost", "Number", 0),
                ("healthBoost", "Number", 0),
                ("damageModifier", "Number", 0),
                ("healthModifier", "Number", 0),
                ("speedModifier", "Number", 0),
                ("reach", "Number", 6),
                ("toolPower", "Number", 0),
                ("useSpeed", "Number", 60),
                ("stackable", "Boolean", true),
                ("tooltip", "String", ""),
                ("pickaxe", "Boolean", false),
                ("axe", "Boolean", false),
                ("bagSlots", "Number", 0),
                ("sprite", "String", "noSprite"),
                ("useSprite", "String", "noSprite"),
                ("cooldown", "Number", 0),
                ("maxCooldown", "Number", 0),
                ("healthbarScale", "Number", 1),
                ("heldAngleOffset", "Number", 0),
                ("attackBuffs", "String", "noone"),
                ("consumeBuffs", "String", "noone"),
                ("bad", "Boolean", false),
                ("heal", "Number", 0),
                ("regen", "Number", 0),
                ("regenLength", "Number", 0),
                ("alive", "Boolean", false),
                ("consumeOnUse", "Boolean", false),
                ("potionType", "String", "none"),
                ("potionStrength", "Number", 0),
                ("smeltable", "Boolean", false),
                ("smeltResult", "String", ""),
                ("smeltAmount", "Number", 0),
                ("behavior", "String", "noone")
            }
        },

        {
            "Lifeform",
            new List<(string, string, object)>
            {
                ("id", "String", ""),
                ("species", "String", ""),
                ("hostile", "Boolean", false),
                ("hp", "Number", 100),
                ("speed", "Number", 10)
            }
        },
        {
            "Races",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("description", "String", ""),
                ("attributes", "Object", new JObject())
            }
        },
        {
            "Empty",
            new List<(string, string, object)>
            {
                ("Base", "Object", ""),
            }
        }
    };
}


public static class ToolTipLibrary
{

    public static readonly Dictionary<string, string> ItemTooltips = new()
    {
        ["name"] = "Display name of the item.",
        ["fileName"] = "Internal filename (used for linking assets).",
        ["damage"] = "Base damage dealt by the item.",
        ["rarity"] = "How rare the item is (normal, rare, epic...).",
        ["color"] = "Base color (e.g., white, red).",
        ["durability"] = "Current durability of the item.",
        ["maxDurability"] = "Maximum durability the item can have.",
        ["consumable"] = "If true, the item is consumed on use.",
        ["faceMouse"] = "If true, item faces the mouse when held.",
        ["enchant"] = "ID or name of applied enchantment (if any).",
        ["projectile"] = "Projectile ID this item shoots.",
        ["useSound"] = "Sound played when the item is used.",
        ["shootSound"] = "Sound played when shooting a projectile.",
        ["frames"] = "Number of animation frames.",
        ["animationSpeed"] = "Speed of animation (frames per tick).",
        ["xoffset"] = "Horizontal offset when held.",
        ["yoffset"] = "Vertical offset when held.",
        ["castable"] = "If true, the item casts spells.",
        ["tile"] = "ID of tile placed by this item.",
        ["placeable"] = "If true, item can place tiles.",
        ["type"] = "Functional category (e.g., weapon, tool, material).",
        ["typeName"] = "Display label for type (UI only).",
        ["damageType"] = "What kind of damage this item does (e.g., melee, ranged).",
        ["damageTypeName"] = "Display name for damage type.",
        ["headSprite"] = "Head sprite when equipped.",
        ["bodySprite"] = "Body sprite when equipped.",
        ["handSprite"] = "Hand sprite when equipped.",
        ["shieldSprite"] = "Shield sprite when equipped.",
        ["backSprite"] = "Back sprite (e.g., cape, wings).",
        ["damageBoost"] = "Flat damage added when equipped.",
        ["healthBoost"] = "Extra health granted when equipped.",
        ["damageModifier"] = "Percent-based damage modifier.",
        ["healthModifier"] = "Percent-based health modifier.",
        ["speedModifier"] = "Percent-based speed modifier.",
        ["reach"] = "Reach distance (e.g. for tools).",
        ["toolPower"] = "Tool strength (e.g. mining power).",
        ["useSpeed"] = "Speed at which the item is used.",
        ["stackable"] = "If true, multiple items can stack.",
        ["ID"] = "Internal ID of the item.",
        ["tooltip"] = "Additional description shown in UI.",
        ["pickaxe"] = "If true, this is a pickaxe.",
        ["axe"] = "If true, this is an axe.",
        ["bagSlots"] = "Additional inventory slots granted.",
        ["sprite"] = "Base sprite for the item.",
        ["useSprite"] = "Sprite shown when item is held out.",
        ["cooldown"] = "Current cooldown ticks.",
        ["maxCooldown"] = "Max cooldown ticks before reuse.",
        ["healthbarScale"] = "Scale of cooldown bar UI.",
        ["heldAngleOffset"] = "Angle offset when rendering (for bows, etc).",
        ["attackBuffs"] = "Buff applied to target on attack.",
        ["consumeBuffs"] = "Buff applied to self on consume.",
        ["bad"] = "If true, this is a cursed or harmful item.",
        ["heal"] = "Health restored when consumed.",
        ["regen"] = "Health regenerated per tick.",
        ["regenLength"] = "How long regeneration lasts (ticks).",
        ["alive"] = "Used for living items or test state.",
        ["consumeOnUse"] = "If true, item is consumed when used.",
        ["potionType"] = "Type of potion effect.",
        ["potionStrength"] = "Power level of potion effect.",
        ["smeltable"] = "Whether this item can be smelted.",
        ["smeltResult"] = "Name of the resulting item after smelting.",
        ["smeltAmount"] = "How many units are produced from smelting.",
        ["behavior"] = "Custom behavior script for this item."
    };
}