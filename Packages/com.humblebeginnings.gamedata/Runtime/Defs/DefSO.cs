using UnityEngine;

namespace HumbleBeginnings.GameData.Core
{
    /// <summary>
    /// Base class for all game definition ScriptableObjects.
    /// Definitions are immutable authored data and referenced by stable IDs.
    /// </summary>
    public abstract class DefSO : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        [Tooltip("Stable unique identifier. Never change after first use.")]
        private string id;

        [SerializeField]
        [Tooltip("Human-readable name shown in UI.")]
        private string displayName;

        public string Id => id;
        public string DisplayName => displayName;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-time validation to prevent empty IDs.
        /// Runtime should never mutate defs.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning(
                    $"{name} ({GetType().Name}) has an empty Id. " +
                    $"This will break saves and references.",
                    this);
            }
        }
#endif
    }
}
