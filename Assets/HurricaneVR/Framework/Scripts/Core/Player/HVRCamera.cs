﻿using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace HurricaneVR.Framework.Core.Player
{
    [RequireComponent(typeof(Camera))]
    public class HVRCamera : MonoBehaviour
    {
        public Camera Camera { get; private set; }


        private void Start()
        {
            Camera = GetComponent<Camera>();
            if (HVRManager.Instance)
            {
                HVRManager.Instance.Camera = transform;
            }
            gameObject.layer = LayerMask.NameToLayer(HVRLayers.Player.ToString());
        }
    }
}
