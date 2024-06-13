using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField]
    private Vector2 spawnArea = new Vector2(100f, 100f);

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector2 randomPosition = new Vector2(
            Random.Range(-spawnArea.x, spawnArea.x),
            Random.Range(-spawnArea.y, spawnArea.y)
        );

        GameObject player = Instantiate(playerPrefab, randomPosition, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}