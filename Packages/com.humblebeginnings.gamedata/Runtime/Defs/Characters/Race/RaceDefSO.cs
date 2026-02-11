using UnityEngine;
using HumbleBeginnings.GameData.Core; 

namespace HumbleBeginnings.GameData.Defs.Characters
{
    /// <summary>
    /// Defines a playable race. Races apply baseline modifiers and tags.
    /// This v1 definition is intentionally minimal.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Race",
        menuName = "Humble Beginnings/Game Data/Characters/Race",
        order = 10)]
    public class RaceDefSO : DefSO
    {
        [Header("Description")]
        [TextArea]
        [SerializeField]
        private string description;

        public string Description => description;
    }
}
