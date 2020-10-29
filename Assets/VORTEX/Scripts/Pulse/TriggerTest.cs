﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pulse.CDM
{
    public class TriggerTest : MonoBehaviour
    {
        public PulseEventManager eventManager;

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Respawn")
            {
                eventManager.TriggerPulseAction(PulseAction.StopAirwayObstruction);
            }
        }
    }
}