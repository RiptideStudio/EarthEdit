using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonMaker.DataTypes
{
    public class Item
    {
        /// <summary>
        /// Type of the item
        /// </summary>
        public enum Type
        {
            Helmet,
            Chestplate,
            Ring,
            Amulet,
            Bag,
            Shield,
            Material,
            Rod,
            Tile
        }

        /// <summary>
        /// Damage type of the item
        /// </summary>
        public enum DamageType
        { 
            Melee,
            Ranged,
            Magic
        }

        public enum Color
        {
            White,
            Blue,
            Green,
            Orange,
            Yellow,
            Purple,
            Red,
            Black,
            Brown
        }

        /// <summary>
        /// Item rarity
        /// </summary>
        public enum Rarity
        {
            White,
            Blue,
            Green,
            Orange,
            Yellow,
            Purple,
            Red,
        }
    }
    public static class EnumUtil
    {
        public static string[] ToStringArray<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T));
        }
    }

}
