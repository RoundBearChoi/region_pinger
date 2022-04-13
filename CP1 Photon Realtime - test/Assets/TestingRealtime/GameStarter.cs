using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

namespace RB.TestingRealtime
{
    public class GameStarter : MonoBehaviour, IConnectionCallbacks
    {
        [SerializeField]
        int _targetUpdateRate = 0;

        [SerializeField]
        int _targetFixedUpdateRate = 0;

        [SerializeField]
        string _region = string.Empty;

        [SerializeField]
        RoomHandler _roomCreatorPrefab = null;

        readonly LoadBalancingClient _loadBalancingClient = new LoadBalancingClient();
        RoomHandler _roomCreatorInstance = null;
        bool _ended = false;
        
        void Start()
        {
            Application.targetFrameRate = _targetUpdateRate;
            Time.fixedDeltaTime = 1f / (float)_targetFixedUpdateRate;

            _loadBalancingClient.AddCallbackTarget(this);
            _loadBalancingClient.StateChanged += this.OnStateChange;

            AppSettings settings = new AppSettings();
            settings.AppIdRealtime = "ebb96fdf-fa91-4f88-9e29-aa87bf8f4326";
            settings.FixedRegion = _region;
            settings.NetworkLogging = ExitGames.Client.Photon.DebugLevel.ERROR;
            
            _loadBalancingClient.ConnectUsingSettings(settings);
        }

        void Update()
        {
            _loadBalancingClient.Service();
        }

        void FixedUpdate()
        {
            
        }

        void OnStateChange(ClientState arg1, ClientState arg2)
        {
            UnityEngine.Debug.Log("OnStateChange.." + arg1 + " -> " + arg2);
        }

        public void OnConnectedToMaster()
        {
            UnityEngine.Debug.Log("connected to master server " + _loadBalancingClient.LoadBalancingPeer.ServerIpAddress + " region " + _loadBalancingClient.CloudRegion);
            UnityEngine.Debug.Log("total players: " + _loadBalancingClient.PlayersOnMasterCount);

            _roomCreatorInstance = Instantiate(_roomCreatorPrefab);
            _roomCreatorInstance.Init(_loadBalancingClient);
            _roomCreatorInstance.CreateRoom();
        }

        public void OnDisconnected(DisconnectCause cause) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }
        public void OnConnected() { }
        public void OnRegionListReceived(RegionHandler regionHandler) { }

        void OnDestroy()
        {
            End();
        }

        void OnApplicationQuit()
        {
            End();
        }

        void End()
        {
            if (!_ended)
            {
                UnityEngine.Debug.Log("ending " + this.gameObject.name);
                _ended = true;
                _loadBalancingClient.Disconnect();
                _loadBalancingClient.RemoveCallbackTarget(this);
            }
        }
    }
}