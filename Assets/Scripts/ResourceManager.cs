using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : NetworkBehaviour
{
    [SerializeField] private List<Resource> resources;
    [SerializeField] private Vector2 spawnRange;
    [SerializeField] private int spawnCount;

    public override void OnStartServer()
    {
        if (isServer)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnResource();
            }
        }
    }

    [Server]
    private void SpawnResource()
    {
        int index = Random.Range(0, resources.Count);
        Resource resourceToSpawn = resources[index];

        float xPosition = Random.Range(-spawnRange.x, spawnRange.x);
        float yPosition = Random.Range(-spawnRange.y, spawnRange.y);
        Vector2 spawnPosition = new Vector2(xPosition, yPosition);

        Resource spawnedResource = Instantiate(resourceToSpawn, spawnPosition, Quaternion.identity);
        spawnedResource.transform.SetParent(transform);
        NetworkServer.Spawn(spawnedResource.gameObject);
    }
}