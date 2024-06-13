using UnityEngine;
using Mirror;
using Cinemachine;

public class CameraFollow : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>().Follow = transform;
        GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>().LookAt = transform;
    }
}