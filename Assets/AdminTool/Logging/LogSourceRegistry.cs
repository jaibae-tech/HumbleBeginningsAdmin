using System.Collections.Generic;
using UnityEngine;

namespace HumbleBeginnings.Admin.Logging
{
    [CreateAssetMenu(
        fileName = "LogSourceRegistry",
        menuName = "Humble Beginnings/Admin/Log Source Registry",
        order = 1)]
    public sealed class LogSourceRegistry : ScriptableObject
    {
        public List<LogSourceDefinition> Sources = new List<LogSourceDefinition>();
    }
}

