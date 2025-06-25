using JsonMaker.DataTypes;
using Newtonsoft.Json.Linq;

public class JsonField
{
    public string Name { get; set; }
    public string Type { get; set; }
    public object DefaultValue { get; set; }
    public string[] EnumOptions { get; set; } // optional

    public JsonField(string name, string type, object defaultValue, string[] enumOptions = null)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        EnumOptions = enumOptions;
    }
}

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
                ("rarity", "String", "White"),
                ("durability", "Number", 0),
                ("consumable", "Boolean", false),
                ("projectile", "String", ""),
                ("type", "String", "Material"),
                ("damageType", "String", "Melee"),
                ("damageBoost", "Number", 0),
                ("healthBoost", "Number", 0),
            }
        },
        {
            "Enemy",
            new List<(string name, string type, object defaultValue)>
            {
                ("name", "String", ""),
                ("friendly", "Boolean", false),
                ("hp", "Number", 0),
                ("width", "Number", 16),
                ("height", "Number", 16),
                ("frames", "Number", 1),
                ("damage", "Number", 0),
                ("npc", "Boolean", false),
                ("swarm", "Boolean", false),
                ("spd", "Number", 1),
                ("chaseSpd", "Number", 1),
                ("acc", "Number", 0.3),
                ("attackSight", "Number", 0),
                ("idleAnimation", "Array", new JArray(0, 5, 20, true)),
                ("attackAnimation", "Array", new JArray(0, 5, 20, true)),
                ("shootAnimation", "Array", new JArray(5, 11, 20, false)),
                ("hurtAnimation", "Array", new JArray(0, 5, 24, true)),
                ("aiType", "String", "AngryHornet"),
                ("experience", "Array", new JArray(0, 0)),
                ("lootTable", "String", "")
            }
        },
        {
            "NPC",
            new List<(string name, string type, object defaultValue)>
            {
                ("name", "String", ""),
                ("heldItem", "String", ""),
                ("skin", "String", ""),
                ("friendly", "Boolean", true),
                ("width", "Number", 16),
                ("height", "Number", 16),
                ("frames", "Number", 1),
                ("npc", "Boolean", true),
                ("smart", "Boolean", false),
                ("isShop", "Boolean", false),
                ("hp", "Number", 100),
                ("damage", "Number", 3),
                ("spd", "Number", 1),
                ("chaseSpd", "Number", 2),
                ("acc", "Number", 0.3),
                ("attackCooldown", "Number", 60),
                ("experience", "Array", new JArray(0, 0)),
            }
        },
        {
            "Chestplate",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Chestplate"),
                ("healthBoost", "Number", 6),
                ("damageBoost", "Number", 1),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false),
                ("xoffset", "Number", -2)
            }
        },
        {
            "Helmet",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Helmet"),
                ("healthBoost", "Number", 3),
                ("damageBoost", "Number", 2),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false)
            }
        },
        {
            "Shield",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Shield"),
                ("healthBoost", "Number", 3),
                ("damageBoost", "Number", 1),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false),
                ("xoffset", "Number", -2)
            }
        },
        {
            "Amulet",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Amulet"),
                ("healthBoost", "Number", 5),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false),
                ("xoffset", "Number", -2)
            }
        },
        {
            "Bag",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Bag"),
                ("bagSlots", "Number", 4),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false),
                ("xoffset", "Number", -2)
            }
        },
        {
            "Ring",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("type", "String", "Ring"),
                ("damageBoost", "Number", 2),
                ("rarity", "String", "White"),
                ("stackable", "Boolean", false),
                ("xoffset", "Number", -2)
            }
        },
        {
            "Races",
            new List<(string, string, object)>
            {
                ("name", "String", ""),
                ("description", "String", ""),
                ("background", "String", ""),
                ("skin", "String", ""),
                ("difficulty", "String", ""),
                ("unlocked", "Boolean", false),
                ("hp", "Number", 10),
                ("startingItems", "Array", new JArray
                {
                    new JArray { "WoodenAxe", 1 },
                }),
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

    public class JsonField
    {
        public string Name { get; }
        public string Type { get; }
        public object DefaultValue { get; }
        public string[] EnumOptions { get; }

        public JsonField(string name, string type, object defaultValue, string[] enumOptions = null)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            EnumOptions = enumOptions;
        }
    }

    public static readonly Dictionary<string, List<JsonField>> Properties = new()
    {
        {
            "Item",
            new List<JsonField>
            {
                new("name", "String", ""),
                new("damage", "Number", 0),
                new("rarity", "String", "normal", EnumUtil.ToStringArray<Item.Rarity>()),
                new("color", "String", "white", EnumUtil.ToStringArray<Item.Color>()),
                new("durability", "Number", 0),
                new("maxDurability", "Number", 0),
                new("consumable", "Boolean", false),
                new("faceMouse", "Boolean", false),
                new("projectile", "String", ""),
                new("useSound", "String", "snd_swing"),
                new("shootSound", "String", ""),
                new("frames", "Number", 1),
                new("animationSpeed", "Number", 1),
                new("xoffset", "Number", 0),
                new("yoffset", "Number", 0),
                new("castable", "Boolean", false),
                new("tile", "String", "noone"),
                new("placeable", "Boolean", false),
                new("type", "String", "Material", EnumUtil.ToStringArray<Item.Type>()),
                new("typeName", "String", ""),
                new("damageType", "String", "Melee", EnumUtil.ToStringArray<Item.DamageType>()),
                new("damageTypeName", "String", ""),
                new("damageBoost", "Number", 0),
                new("healthBoost", "Number", 0),
                new("damageModifier", "Number", 0),
                new("healthModifier", "Number", 0),
                new("speedModifier", "Number", 0),
                new("reach", "Number", 6),
                new("toolPower", "Number", 0),
                new("useSpeed", "Number", 60),
                new("stackable", "Boolean", true),
                new("tooltip", "String", ""),
                new("pickaxe", "Boolean", false),
                new("axe", "Boolean", false),
                new("bagSlots", "Number", 0),
                new("sprite", "String", "noSprite"),
                new("useSprite", "String", "noSprite"),
                new("cooldown", "Number", 0),
                new("maxCooldown", "Number", 0),
                new("healthbarScale", "Number", 1),
                new("heldAngleOffset", "Number", 0),
                new("attackBuffs", "String", "noone"),
                new("consumeBuffs", "String", "noone"),
                new("bad", "Boolean", false),
                new("heal", "Number", 0),
                new("regen", "Number", 0),
                new("regenLength", "Number", 0),
                new("alive", "Boolean", false),
                new("consumeOnUse", "Boolean", false),
                new("smeltable", "Boolean", false),
                new("smeltResult", "String", ""),
                new("smeltAmount", "Number", 0),
                new("behavior", "String", "noone")
            }
        },
        {
            "Lifeform",
            new List<JsonField>
            {
                new("name", "String", "Lifeform"),             // display name
                new("hostile", "Boolean", false),              // AI aggression
                new("friendly", "Boolean", true),              // ally flag
                new("npc", "Boolean", false),                  // non-player character
                new("boss", "Boolean", false),                 // is this a boss?
                new("hp", "Number", 5),                        // current health
                new("width", "Number", 32),                     
                new("height", "Number", 32),
                new("frames", "Number", 1),
                new("defense", "Number", 0),                   // flat defense
                new("damage", "Number", 1),                    // base damage
                new("knockBack", "Number", 2),                 // knockback amount
                new("spd", "Number", 0.5),                     // move speed
                new("chaseSpd", "Number", 0.5),                // chase speed
                new("jumpInterval", "Number", 60),             // frames between jumps
                new("jumpSpeed", "Number", 0.5),               // jump velocity
                new("jumpCooldown", "Number", 90),             // cooldown between jumps
                new("maxJumps", "Number", 0),                  // total jumps allowed
                new("doesContactDamage", "Boolean", true),     // touch damage
                new("canAggro", "Boolean", true),              // whether it can be aggroed
                new("aggroMaxTimer", "Number", 240),           // how long aggro lasts
                new("wanderDistance", "Number", 128),          // distance from origin
                new("shootSpeed", "Number", 2),                // projectile speed
                new("projectileDamage", "Number", 2),          // damage from projectile
                new("shootCooldown", "Number", 120),           // cooldown between shots
                new("shootReloadSpeed", "Number", 130),        // reload time
                new("sight", "Number", 128),                   // general sight range
                new("wakeSight", "Number", 128),               // distance to wake from sleep
                new("attackSight", "Number", 96),              // range to attack
                new("strafeSight", "Number", 32),              // for strafing behavior
                new("buffToInflict", "String", ""),            // string ID of buff
                new("color", "String", "Red", EnumUtil.ToStringArray<Item.Color>()),              // color string or hex
                new("aiType", "String", "hornet"),             // string enum AI type
                new("controlType", "String", "interact"),      // string enum
                new("lootTable", "Object", null),              // nested loot table
                new("experience", "Object", null),             // xp loot table
                new("canJump", "Boolean", true),
                new("canSwim", "Boolean", false),
                new("canShoot", "Boolean", true),
                new("state", "String", "idle"),                // enum
                new("segmentSize", "Number", 16),
                new("segments", "Number", 0),
                new("isSegment", "Boolean", false),
                new("deathAnimation", "Boolean", false),
                new("deathSound", "String", ""),               // sound string ID
                new("hurtSound", "Array", null),               // array of sound strings
                new("ignoreKnockback", "Boolean", false),
                new("knockBackResist", "Number", 1.5),
                new("affectedByGravity", "Boolean", true),
                new("waterDestroy", "Boolean", false),
                new("showHealthBar", "Boolean", false),
                new("showName", "Boolean", false)
            }
        },
        {
            "Races",
            new List<JsonField>
            {
                new("name", "String", ""),
                new("description", "String", ""),
                new("attributes", "Object", new JObject())
            }
        },
        {
            "Empty",
            new List<JsonField>
            {
                new("Base", "Object", new JObject())
            }
        }
    };

    /// <summary>
    /// Schemas used to generalize categories of items
    /// </summary>
    public static readonly List<string> Schemas = new List<string>
    {
        "Item",
        "Lifeform",
        "Race"
    };
}


public static class ToolTipLibrary
{

    public static readonly Dictionary<string, string> ItemTooltips = new()
    {
        // Lifeform variables
        ["internalName"] = "Internal name used for referencing this lifeform in code.",
        ["idleAnimation"] = "First frame, last frame, speed [0,60], loops",
        ["attackAnimation"] = "First frame, last frame, speed [0,60], loops",
        ["shootAnimation"] = "First frame, last frame, speed [0,60], loops",
        ["deathAnimation"] = "First frame, last frame, speed [0,60], loops",
        ["species"] = "Type, class, or race of the lifeform.",
        ["hostile"] = "Whether the lifeform is aggressive toward the player.",
        ["npc"] = "If true, the lifeform is a non-player character.",
        ["boss"] = "If true, this is a boss-type enemy.",
        ["hp"] = "Current health of the lifeform.",
        ["maxHp"] = "Maximum health capacity.",
        ["baseHp"] = "Base maximum health before modifiers.",
        ["defense"] = "Flat damage reduction applied to incoming attacks.",
        ["damage"] = "Base attack damage dealt by this lifeform.",
        ["knockBack"] = "Strength of knockback applied to targets when hitting.",
        ["spd"] = "Movement speed of the lifeform.",
        ["chaseSpd"] = "Speed used when pursuing targets.",
        ["jumpInterval"] = "Time (in frames) between jumps.",
        ["jumpSpeed"] = "Upward velocity applied when jumping.",
        ["jumpCooldown"] = "Cooldown (in frames) before the lifeform can jump again.",
        ["maxJumps"] = "Maximum number of consecutive jumps allowed.",
        ["doesContactDamage"] = "If true, touching the player causes damage.",
        ["canAggro"] = "If true, the lifeform can become aggressive toward the player.",
        ["aggroMaxTimer"] = "How long (in frames) the lifeform stays aggroed before calming down.",
        ["wanderDistance"] = "Maximum distance the lifeform will wander from its spawn location.",
        ["shootSpeed"] = "Speed at which projectiles are launched by this lifeform.",
        ["projectileDamage"] = "Damage dealt by projectiles fired by the lifeform.",
        ["shootCooldown"] = "Cooldown (in frames) before the lifeform can shoot again.",
        ["shootReloadSpeed"] = "Time (in frames) it takes to reload before shooting.",
        ["shootFrame"] = "Frame of the animation on which the projectile is fired.",
        ["sight"] = "General detection radius for spotting players or targets.",
        ["wakeSight"] = "Distance within which the lifeform wakes up from sleep.",
        ["attackSight"] = "Distance required to begin attacking a target.",
        ["strafeSight"] = "Range within which the lifeform strafes instead of charging.",
        ["buffToInflict"] = "Buff applied to the target when this lifeform attacks.",
        ["color"] = "Visual color used to tint or represent this lifeform.",
        ["aiType"] = "Defines which behavior system the lifeform uses.",
        ["controlType"] = "Specifies how the lifeform interacts with the world.",
        ["lootTable"] = "Loot table dropped when this lifeform is defeated.",
        ["experience"] = "Experience or resources granted upon death. [min][max]",
        ["friendly"] = "If true, the lifeform will not attack the player.",
        ["canJump"] = "If true, the lifeform can jump.",
        ["canSwim"] = "If true, the lifeform can swim.",
        ["canShoot"] = "If true, the lifeform can fire projectiles.",
        ["state"] = "Current state of the lifeform (idle, chase, attack, etc).",
        ["segmentSize"] = "Pixel length of each segment for segmented creatures.",
        ["segments"] = "Number of body segments this lifeform has (if segmented).",
        ["isSegment"] = "If true, this is a segment of a larger entity.",
        ["deathAnimation"] = "If true, plays a custom death animation.",
        ["deathSound"] = "Sound ID played when the lifeform dies.",
        ["hurtSound"] = "List of sound IDs played when damaged.",
        ["ignoreKnockback"] = "If true, the lifeform will not be affected by knockback.",
        ["knockBackResist"] = "Multiplier that reduces knockback force.",
        ["affectedByGravity"] = "If true, the lifeform falls due to gravity.",
        ["waterDestroy"] = "If true, entering water destroys the lifeform.",
        ["showHealthBar"] = "If true, displays the health bar UI above the lifeform.",
        ["showName"] = "If true, displays the name above the lifeform.",
        ["width"] = "Width of the sprite",
        ["height"] = "Height of the sprite",
        // items
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