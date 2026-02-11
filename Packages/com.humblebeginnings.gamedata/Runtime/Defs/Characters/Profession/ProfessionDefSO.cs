using UnityEngine;
using HumbleBeginnings.GameData.Core;

namespace HumbleBeginnings.GameData.Defs.Characters
{
    /// <summary>
    /// Defines a character profession. Professions bias skills, knowledge, and narrative hooks.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Profession",
        menuName = "Humble Beginnings/Game Data/Characters/Profession",
        order = 30)]
    public class ProfessionDefSO : DefSO
    {
        [Header("Description")]
        [TextArea]
        [SerializeField]
        private string description;

        public string Description => description;
    }
}
