using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RB.TestingRealtime
{
    public struct TripInfo
    {
        public TripInfo(int senderID, int tripInfoID, long tick)
        {
            mSenderID = senderID;
            mTripInfoID = tripInfoID;
            mTick = tick;
        }

        public int mSenderID;
        public int mTripInfoID;
        public long mTick;
    }
}