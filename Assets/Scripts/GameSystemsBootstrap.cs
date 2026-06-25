using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GameSystemsBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (RoomManager.Instance == null && GetComponent<RoomManager>() == null)
            gameObject.AddComponent<RoomManager>();

        if (GameManager.Instance != null && GameManager.Instance.useNetworkMode && MapManager.Instance != null)
            MapManager.Instance.autoBuildOnStart = false;
    }
}
