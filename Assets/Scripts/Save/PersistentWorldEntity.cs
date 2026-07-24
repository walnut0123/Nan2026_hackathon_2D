using UnityEngine;

/// <summary>Marks a pre-placed scene object (monster, level-design item pickup, etc.) whose
/// removal - killed or picked up - should persist across saves. The id defaults to the
/// object's scene hierarchy path, computed lazily, so no manual per-object setup is needed
/// as long as the object isn't moved to a different parent/name between sessions.</summary>
public class PersistentWorldEntity : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(id))
                id = BuildPathId();
            return id;
        }
    }

    private string BuildPathId()
    {
        var path = gameObject.name;
        for (var t = transform.parent; t != null; t = t.parent)
            path = t.name + "/" + path;
        return path;
    }
}
