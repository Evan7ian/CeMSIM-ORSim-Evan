﻿using UnityEngine;

public class TeleportManager : MonoBehaviour
{

    public GameObject rigToBeTeleported;
    public GameObject anteroomTeleportationPad;
    public GameObject operationRoomTeleportationPad;
    public GameObject teleportationPrompt;
    public Room currentRoom { get; set; } = Room.ANTEROOM;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnAllPPEsEquipped()
    {
        if (currentRoom == Room.ANTEROOM)
        {
            anteroomTeleportationPad.SetActive(true);
        }
    }

    public void OnPPEUnequipped()
    {
        if (currentRoom == Room.ANTEROOM)
        {
            anteroomTeleportationPad.SetActive(false);
        }
    }

    public void OnRSICompleted()
    {
        operationRoomTeleportationPad.SetActive(true);
    }

    public void OperationRoomTeleportationTriggered()
    {
        teleportationPrompt.SetActive(true);    
    }

    public void AnteroomTeleportationTriggered()
    {
        Teleport(operationRoomTeleportationPad.transform, Room.OPERATIONROOM);
    }

    public void TeleportToAnteroom()
    {
        teleportationPrompt.SetActive(false);
        Teleport(anteroomTeleportationPad.transform, Room.ANTEROOM);
    }

    public void StayInOR()
    {
        teleportationPrompt.SetActive(false);
    }

    private void Teleport(Transform destination, Room destinationRoom)
    {
        Collider[] colliders = rigToBeTeleported.GetComponentsInChildren<Collider>();
        SwitchColliders(colliders, false);
        rigToBeTeleported.transform.position = new Vector3(destination.position.x, rigToBeTeleported.transform.position.y, destination.position.z);
        currentRoom = destinationRoom;
        SwitchColliders(colliders, true);
    }

    private void SwitchColliders(Collider[] colliders, bool enabled)
    {
        foreach (Collider c in colliders)
        {
            c.enabled = enabled;
        }
    }
    public enum Room
    {
        ANTEROOM,
        OPERATIONROOM
    }
}