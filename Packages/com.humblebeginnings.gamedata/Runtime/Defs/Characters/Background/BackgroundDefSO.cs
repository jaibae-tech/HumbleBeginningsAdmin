using UnityEngine;
using HumbleBeginnings.GameData.Core;

namespace HumbleBeginnings.GameData.Defs.Characters
{
    /// <summary>
    /// Defines a character background. Backgrounds represent formative life experiences
    /// and primarily affect narrative context and starting biases.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Background",
        menuName = "Humble Beginnings/Game Data/Characters/Background",
        order = 40)]
    public class BackgroundDefSO : DefSO
    {
        [Header("Description")]
        [TextArea]
        [SerializeField]
        private string description;

        public string Description => description;
    }
}
