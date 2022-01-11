﻿using CEMSIM.GameLogic;
using CEMSIM.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CEMSIM
{
    namespace GameLogic
    {
        public class ShoeCoverStateManager : ItemStateManager
        {
            public enum ShoeCoverOnFootList
            {
                noneDetermined = 0,
                left,
                right
            }

            private ShoeCoverOnFootList state;
            public static event Action<int, ShoeCoverOnFootList> onShoeCoverOnUpdateTrigger;

            public ShoeCoverStateManager()
            {
                toolCategory = ToolType.N95Mask;
                UpdateState(ShoeCoverOnFootList.noneDetermined); // 
                                                          //Debug.Log($"Initialize {toolCategory} - {state}");
            }

            public override void initializeItem(int _id)
            {
                base.initializeItem(_id);
            }

            public override byte[] GetItemState()
            {
                List<byte> message = new List<byte>();
                message.AddRange(BitConverter.GetBytes((int)state));

                return message.ToArray();
            }

            public override void DigestStateMessage(Packet _remainderPacket)
            {
                int _specId = _remainderPacket.ReadInt32();
                if (!Enum.IsDefined(typeof(ShoeCoverOnFootList), _specId))
                {
                    Debug.LogWarning($"{toolCategory} does't have state {_specId}. State ignored");
                    return;
                }

                UpdateState((ShoeCoverOnFootList)_specId);
            }

            /// <summary>
            /// Update state
            /// </summary>
            public void UpdateState(ShoeCoverOnFootList _newState)
            {
                state = _newState;
                ItemStateUpdateTrigger(itemId, state);

            }

            #region Event System
            public static void ItemStateUpdateTrigger(int _itemId, ShoeCoverOnFootList _state)
            {
                //Debug.LogError($"lalalalala,onPlayerEnterTrigger {onPlayerEnterTrigger}");
                if (onShoeCoverOnUpdateTrigger != null)
                    onShoeCoverOnUpdateTrigger(_itemId, _state);
            }
            #endregion
        }
    }
}