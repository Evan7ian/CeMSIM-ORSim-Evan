﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CEMSIM.GameLogic;

namespace CEMSIM
{
    namespace Network
    {
        /// <summary>
        /// This class handles sending packets
        /// </summary>
        public class ClientSend : MonoBehaviour
        {
            #region Basic TCP and UDP packet sending functions
            /// <summary>
            /// Send packet to the server via TCP
            /// </summary>
            /// <param name="_packet">The packet including only the payload</param>
            /// <param name="addTime">Whether to include the generation time of the packet</param>
            private static void SendTCPData(Packet _packet, bool addTime = false)
            {
                //_packet.WriteLength(); // add the Data Length to the packet
                _packet.WriteHeader(addTime);// 
                ClientInstance.instance.tcp.SendData(_packet);
            }

            /// <summary>
            /// Send packet to the server via UDP
            /// </summary>
            /// <param name="_packet">The packet including only the payload</param>
            /// <param name="addTime">Whether to include the generation time of the packet</param>
            private static void SendUDPData(Packet _packet, bool addTime = false)
            {
                _packet.WriteHeader(addTime);// 
                ClientInstance.instance.udp.SendData(_packet);
            }
            #endregion

            #region Generate Packets
            public static void WelcomeReceived()
            {
                using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
                {
                    _packet.Write(ClientInstance.instance.myId);
                    _packet.Write(ClientInstance.instance.myUsername);

                    SendTCPData(_packet);
                }
            }

            public static void WelcomeUDP()
            {
                using(Packet _packet = new Packet((int)ClientPackets.welcomeUDP))
                {
                    SendUDPData(_packet);
                }
            }

            public static void SendTCPPing(string _msg = "")
            {
                using (Packet _packet = new Packet((int)ClientPackets.pingTCP))
                {
                    _packet.Write(_msg);

                    SendTCPData(_packet);
                }
            }

            public static void SendUDPPing(string _msg = "")
            {
                using (Packet _packet = new Packet((int)ClientPackets.pingUDP))
                {
                    _packet.Write(ClientInstance.instance.myId);
                    _packet.Write(_msg);

                    SendUDPData(_packet);
                }
            }

            public static void SendSpawnRequest(string _username, bool _vrEnabled, Roles _role)
            {
                using (Packet _packet = new Packet((int)ClientPackets.spawnRequest))
                {
                    _packet.Write(_username);
                    _packet.Write(_vrEnabled);
                    _packet.Write((int)_role);

                    SendTCPData(_packet);
                }
            }

            /// <summary>
            /// Send out player's movement
            /// </summary>
            /// <param name="_inputs"></param>
            public static void PlayerDesktopMovement(bool[] _inputs)
            {
                using (Packet _packet = new Packet((int)ClientPackets.playerDesktopMovement))
                {
                    // use to identify how many keys are sent
                    _packet.Write(_inputs.Length);
                    foreach (bool _input in _inputs)
                    {
                        _packet.Write(_input);
                    }
                    _packet.Write(GameManager.players[ClientInstance.instance.myId].transform.rotation);

                    SendUDPData(_packet);
                }
            }

            /// <summary>
            /// Send out player's movement
            /// </summary>
            /// <param name="_inputs"></param>
            public static void PlayerVRMovement()
            {
                if (!ClientInstance.instance.isReady)
                {
                    // Current user is not ready for position updating. Maybe in the delayed spawning stage.
                    return;
                }
                if (GameManager.players.ContainsKey(ClientInstance.instance.myId))
                {
                    // get the avatar position
                    //Transform _avatar = GameManager.players[ClientInstance.instance.myId].GetComponent<PlayerVRController>().VRCamera;
                    Transform _avatar = GameManager.players[ClientInstance.instance.myId].GetComponent<PlayerManager>().body.transform;

                    // get the position of both VR controllers
                    Transform _lefthand = GameManager.players[ClientInstance.instance.myId].GetComponent<PlayerManager>().leftHandController.transform;
                    Transform _righthand = GameManager.players[ClientInstance.instance.myId].GetComponent<PlayerManager>().rightHandController.transform;


                    using (Packet _packet = new Packet((int)ClientPackets.playerVRMovement))
                    {
                        // write avatar position

                        _packet.Write(_avatar.position);
                        _packet.Write(_avatar.rotation);

                        // write left and right controller positions
                        _packet.Write(_lefthand.position);
                        _packet.Write(_lefthand.rotation);
                        _packet.Write(_righthand.position);
                        _packet.Write(_righthand.rotation);

                        SendUDPData(_packet);

                        
                    }
                }
                else
                {
                    Debug.Log($"Local Warning: Client ID {ClientInstance.instance.myId} does not exist or has not been added yet");
                }
            }

            public static void SendHeartBeatResponseTCP(long sendTicks)
            {
                using (Packet _packet = new Packet((int)ClientPackets.heartBeatDetectionTCP))
                {
                    _packet.Write(sendTicks);
                    SendTCPData(_packet);
                }
            }

            public static void SendHeartBeatResponseUDP(long sendTicks)
            {
                using (Packet _packet = new Packet((int)ClientPackets.heartBeatDetectionUDP))
                {
                    _packet.Write(sendTicks);
                    SendUDPData(_packet);
                }
            }

            /// <summary>
            /// Send the latest position of the interactable item (owned by the client) to the server.
            /// </summary>
            /// <param name="_item"></param>
            public static void SendItemPosition(GameObject _item, bool isUDP)                             //Send Item position to server via UDP
            {
                ItemController itemCon = _item.GetComponent<ItemController>();
                using (Packet _packet = new Packet((int)ClientPackets.itemState))
                {
                    _packet.Write(itemCon.id);
                    _packet.Write(_item.transform.position);
                    _packet.Write(_item.transform.rotation);
                    
                    if (isUDP)
                        SendUDPData(_packet);
                    else
                        SendTCPData(_packet);


                }
            }

            public static void SendOnwershipChange(GameObject _item, bool _toGrab)                          //Send Item rotation to server via TCP
            {
                using (Packet _packet = new Packet((int)ClientPackets.itemOwnershipChange))
                {
                    ItemController itemCon = _item.GetComponent<ItemController>();
                    _packet.Write(itemCon.id);
                    _packet.Write(_toGrab); // if ownerId = 0, it only means that the user release the item. The true owner may not be user 0 (server)
                    SendTCPData(_packet);
                }

            }

            public static void SendEnvironmentState(int eventId, byte[] message)
            {
                using (Packet _packet=new Packet((int)ClientPackets.environmentState))
                {
                    _packet.Write(eventId); // id of the environment event
                    _packet.Write(message); // message
                    SendTCPData(_packet);
                }

            }
            #endregion

            public static void SendVoiceChatData(ArraySegment<byte> _voiceData, bool _isUDP=true)
            {
                using (Packet _packet = new Packet((int)ClientPackets.voiceChatData))
                {
                    _packet.Write(_voiceData);
                    if (_isUDP)
                        SendUDPData(_packet);
                    else
                        SendTCPData(_packet);
                }
               
            }

            /// <summary>
            /// Inform server your dissonance player id.
            /// </summary>
            /// <param name="_playerId">The uid of the dissonance player</param>
            public static void SendVoiceChatPlayerId(string _playerId)
            {
                using (Packet _packet = new Packet((int)ClientPackets.voiceChatPlayerId))
                {
                    _packet.Write(_playerId);
                    SendTCPData(_packet);
                }
            }

        }
    }
}
