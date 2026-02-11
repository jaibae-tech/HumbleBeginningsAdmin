using UnityEngine;
using HumbleBeginnings.GameData.Core;

namespace HumbleBeginnings.GameData.Defs.Characters
{
    /// <summary>
    /// Defines a character class. Classes influence starting capabilities and narrative options.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Class",
        menuName = "Humble Beginnings/Game Data/Characters/Class",
        order = 20)]
    public class ClassDefSO : DefSO
    {
        [Header("Description")]
        [TextArea]
        [SerializeField]
        private string description;

        public string Description => description;
    }
}

