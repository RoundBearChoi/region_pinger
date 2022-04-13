using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace RB.TestingRealtime
{
    public class RoomHandler : SerializedMonoBehaviour, IConnectionCallbacks, IMatchmakingCallbacks, IOnEventCallback
    {
        [SerializeField] Dictionary<int, TripInfo> _dicRoundTrips = new Dictionary<int, TripInfo>();

        LoadBalancingClient _loadBalancingClient = null;
        bool _initialized = false;

        public void Init(LoadBalancingClient client)
        {
            _initialized = true;

            _loadBalancingClient = client;
            client.AddCallbackTarget(this);
        }

        void Update()
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                if (_initialized)
                {
                    if (_loadBalancingClient.InRoom)
                    {
                        int randomID = Random.Range(int.MinValue, int.MaxValue);

                        while(randomID == 0)
                        {
                            randomID = Random.Range(int.MinValue, int.MaxValue);
                        }

                        _RaiseRoundTrip(_loadBalancingClient.LocalPlayer.ActorNumber, randomID);
                    }
                }
            }
        }

        public void CreateRoom()
        {
            _CreateRoom("111", 2);
        }

        void _CreateRoom(string roomName, byte maxPlayers)
        {
            UnityEngine.Debug.Log("creating room " + roomName + " with max players: " + maxPlayers);
        
            EnterRoomParams enterRoomParams = new EnterRoomParams();
            enterRoomParams.RoomOptions = new RoomOptions();

            enterRoomParams.RoomName = roomName;
            enterRoomParams.RoomOptions.MaxPlayers = maxPlayers;
            enterRoomParams.JoinMode = JoinMode.CreateIfNotExists;

            _loadBalancingClient.OpCreateRoom(enterRoomParams);
        }

        public void JoinRoom(string roomName)
        {
            UnityEngine.Debug.Log("joining room " + roomName);

            EnterRoomParams enterRoomParams = new EnterRoomParams();
            RoomOptions roomOptions = new RoomOptions();

            enterRoomParams.RoomName = roomName;
            enterRoomParams.JoinMode = JoinMode.Default;

            _loadBalancingClient.OpJoinRoom(enterRoomParams);
        }

        void _GetPlayerList()
        {
            UnityEngine.Debug.Log("---player list---");

            foreach (KeyValuePair<int, Player> data in _loadBalancingClient.CurrentRoom.Players)
            {
                UnityEngine.Debug.Log("player: " + data.Value.ActorNumber);
            }
        }

        void _RaisePlayerJoined()
        {
            byte eventType = System.Convert.ToByte(EventCodeType.PLAYER_JOINED);

            ExitGames.Client.Photon.Hashtable hashTable = new ExitGames.Client.Photon.Hashtable();

            hashTable.Add((int)5, (int)50);

            _loadBalancingClient.OpRaiseEvent(eventType, hashTable, RaiseEventOptions.Default, SendOptions.SendReliable);
        }

        void _RaiseRoundTrip(int senderID, int tripInfoID)
        {
            TripInfo tripInfo = new TripInfo(senderID, tripInfoID, System.DateTime.Now.Ticks);

            if (_RecordTripStart(tripInfo) || tripInfoID == 0)
            {
                UnityEngine.Debug.Log("local player " + _loadBalancingClient.LocalPlayer.ActorNumber + " sends roundtrip: " + tripInfo.mTripInfoID);

                byte eventType = System.Convert.ToByte(EventCodeType.ROUND_TRIP);

                SendOptions sendOptions = new SendOptions();
                sendOptions.DeliveryMode = DeliveryMode.Unreliable;

                ExitGames.Client.Photon.Hashtable h = _GetTripHashTable(tripInfo);

                _loadBalancingClient.OpRaiseEvent(eventType, h, RaiseEventOptions.Default, sendOptions);
            }
        }

        ExitGames.Client.Photon.Hashtable _GetTripHashTable(TripInfo tripInfo)
        {
            ExitGames.Client.Photon.Hashtable hashTable = new ExitGames.Client.Photon.Hashtable();

            byte[] senderID = System.BitConverter.GetBytes(tripInfo.mSenderID);
            byte[] tripInfoID = System.BitConverter.GetBytes(tripInfo.mTripInfoID);

            hashTable.Add((byte)0, senderID);
            hashTable.Add((byte)1, tripInfoID);

            return hashTable;
        }

        bool _RecordTripStart(TripInfo tripInfo)
        {
            if (!_dicRoundTrips.ContainsKey(tripInfo.mTripInfoID))
            {
                _dicRoundTrips.Add(tripInfo.mTripInfoID, tripInfo);
                return true;
            }

            return false;
        }

        //connection callbacks
        public void OnConnected() { }
        public void OnConnectedToMaster() { }
        public void OnDisconnected(DisconnectCause cause) { }
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }

        //matchmaking callbacks
        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {

        }

        public void OnCreatedRoom()
        {
            UnityEngine.Debug.Log("created room: " + _loadBalancingClient.CurrentRoom.Name);
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {

        }

        public void OnJoinedRoom()
        {
            UnityEngine.Debug.Log("player joined room.. local userID is " + _loadBalancingClient.LocalPlayer.UserId);

            _GetPlayerList();

            _RaisePlayerJoined();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {

        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {

        }

        public void OnLeftRoom()
        {

        }

        //events callbacks
        void IOnEventCallback.OnEvent(EventData photonEvent)
        {
            int nEventCode = System.Convert.ToInt32(photonEvent.Code);
            EventCodeType eventCodeType = (EventCodeType)nEventCode;

            UnityEngine.Debug.Log("received event: " + eventCodeType.ToString());

            if (eventCodeType == EventCodeType.PLAYER_JOINED)
            {
                _GetPlayerList();
            }

            else if (eventCodeType == EventCodeType.ROUND_TRIP)
            {
                int index0 = 0;
                int index1 = 0;

                ExitGames.Client.Photon.Hashtable table = (ExitGames.Client.Photon.Hashtable)photonEvent.CustomData;

                int senderID = System.BitConverter.ToInt32((byte[])table[0], index0);
                int tripInfoID = System.BitConverter.ToInt32((byte[])table[1], index1);

                UnityEngine.Debug.Log("localplayer " + _loadBalancingClient.LocalPlayer.ActorNumber + " received tripInfoID: " + tripInfoID + " from player " + senderID);

                if (senderID != _loadBalancingClient.LocalPlayer.ActorNumber)
                {
                    _RaiseRoundTrip(senderID, tripInfoID);
                }
                else
                {
                    if (_dicRoundTrips.ContainsKey(tripInfoID))
                    {
                        long dif = System.DateTime.Now.Ticks - _dicRoundTrips[tripInfoID].mTick;
                        UnityEngine.Debug.Log("tick difference is " + dif + ".. half is " + (double)dif / (double)2 + "..");
                    }
                }

                if (_dicRoundTrips.ContainsKey(tripInfoID))
                {
                    _dicRoundTrips.Remove(tripInfoID);
                }
            }
        }
    }
}