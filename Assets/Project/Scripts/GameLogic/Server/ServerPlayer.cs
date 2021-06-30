﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CEMSIM.Network;

namespace CEMSIM
{
    namespace GameLogic
    {
        public abstract class ServerPlayer : MonoBehaviour
        {
            public int id;
            public string username;
            public Roles role;
            public CharacterController controller;

            [Header("Required")]
            public GameObject body;
            public GameObject leftHandController;
            public GameObject rightHandController;
            public GameObject displayName;
            // controller data


            public void Initialize(int _id, string _username, Roles _role)
            {
                id = _id;
                username = _username;
                role = (Roles)_role;
            }

            public void SendToClients()
            {
                // public the position to every client, but public the facing direction to all but the player
                //ServerSend.PlayerPosition(this);
            }

            public void SetDisplayName(string _name)
            {
                if (displayName != null)
                {
                    displayName.GetComponent<TextMesh>().text = _name;
                }
                else
                {
                    Debug.LogWarning("username is null");
                }
            }
        }
    }
}