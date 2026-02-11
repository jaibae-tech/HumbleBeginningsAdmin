using System.Collections.Generic;
using UnityEngine;
using HumbleBeginnings.GameData.Defs.Characters;

namespace HumbleBeginnings.GameData.Core
{
    [CreateAssetMenu(
        fileName = "DefinitionRegistry",
        menuName = "Humble Beginnings/Game Data/Definition Registry",
        order = 0)]
    public class DefinitionRegistrySO : ScriptableObject
    {
        [Header("Registry Version")]
        [SerializeField] private string registryVersion = "0.1.0";

        [Header("Character")]
        public List<RaceDefSO> races = new();
        public List<ClassDefSO> classes = new();
        public List<ProfessionDefSO> professions = new();
        public List<BackgroundDefSO> backgrounds = new();
        public List<PerkDefSO> perks = new();
        public List<TitleDefSO> titles = new();

        [Header("Combat")]
        public List<SkillTrackDefSO> skillTracks = new();
        public List<StyleDefSO> styles = new();
        public List<TechniqueDefSO> techniques = new();
        public List<MonsterFamilyDefSO> monsterFamilies = new();

        [Header("Knowledge")]
        public List<UnlockRequirementDefSO> unlockRequirements = new();

        [Header("Items & Wounds")]
        public List<ItemDefSO> items = new();
        public List<WoundTypeDefSO> woundTypes = new();

        [Header("World")]
        public List<RegionDefSO> regions = new();
        public List<NPCDefSO> npcs = new();
        public List<DomainDefSO> domains = new();

        [Header("Presentation")]
        public List<PortraitDefSO> portraits = new();
        public List<ModelDefSO> models = new();
        public List<VoiceDefSO> voices = new();
    }
}
