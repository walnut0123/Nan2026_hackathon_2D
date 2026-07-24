using UnityEngine;

/// <summary>Tags a runtime-spawned item pickup (monster drop, player-dropped item) so the
/// save system knows to persist and recreate it. Pre-placed level pickups are tracked via
/// PersistentWorldEntity instead - they already exist by default, so only their removal
/// needs to be recorded, not their re-creation.</summary>
public class DroppedItemMarker : MonoBehaviour
{
}
