using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionCatalog", menuName = "BaHanda/Mission Catalog")]
public class MissionCatalog : ScriptableObject
{
    [SerializeField] private MissionData[] missions;

    public MissionData[] Missions => missions;

    public static IReadOnlyList<MissionData> LoadMissions()
    {
        MissionCatalog catalog = Resources.Load<MissionCatalog>("MissionCatalog");
        if (catalog != null && catalog.missions != null && catalog.missions.Length > 0)
        {
            return catalog.missions
                .Where(mission => mission != null)
                .ToArray();
        }

        return Resources.FindObjectsOfTypeAll<MissionData>()
            .Where(mission => mission != null)
            .OrderBy(mission => mission.phase)
            .ThenBy(mission => mission.sortOrder)
            .ToArray();
    }
}