using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using Photon.Realtime;

namespace RB.TestingRealtime
{
    public class RegionPinger : SerializedMonoBehaviour, IConnectionCallbacks
    {
        readonly LoadBalancingClient _loadBalancingClient = new LoadBalancingClient();
        bool _ended = false;

        [Header("Settings")]
        [SerializeField] Text _text = null;

        [Header("Debug")]
        [SerializeField] Dictionary<string, int> _dicPingResults = new Dictionary<string, int>();
        [SerializeField] bool _pingComplete = false;
        [SerializeField] string _pingResults = string.Empty;
        bool _resultsUpdated = true;
        RegionHandler _regionHandler = null;

        void Start()
        {
            _loadBalancingClient.AddCallbackTarget(this);
            _loadBalancingClient.StateChanged += this.OnStateChange;

            _loadBalancingClient.AppId = "ebb96fdf-fa91-4f88-9e29-aa87bf8f4326";
            _loadBalancingClient.ConnectToNameServer();
        }

        void Update()
        {
            ShowPingResults();

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                if (_pingComplete)
                {
                    StartPinging();
                }
            }
        }

        void FixedUpdate()
        {
            _loadBalancingClient.Service();
        }

        void OnStateChange(ClientState arg1, ClientState arg2)
        {
            UnityEngine.Debug.Log("OnStateChange.." + arg1 + " -> " + arg2);

            if (arg2 == ClientState.ConnectedToNameServer)
            {
                bool canBeSent = _loadBalancingClient.OpGetRegions();
                                
                if (!canBeSent)
                {
                    _text.text = "can't send operation";
                }
                else
                {
                    _text.text = "OpGetRegions..";
                }
            }
        }

        public void OnDisconnected(DisconnectCause cause) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            _regionHandler = regionHandler;

            StartPinging();
        }

        void StartPinging()
        {
            if (_regionHandler != null)
            {
                _text.text = "pinging regions..";
                _pingComplete = false;
                _resultsUpdated = true;

                _regionHandler.PingMinimumOfRegions(OnPingComplete, "");
            }
        }

        void OnPingComplete(RegionHandler regionHandler)
        {
            _dicPingResults = new Dictionary<string, int>();

            List<Region> regions = regionHandler.EnabledRegions;
            
            foreach (Region r in regions)
            {
                if (!_dicPingResults.ContainsKey(r.Code))
                {
                    _dicPingResults.Add(r.Code, r.Ping);
                }
            }

            _pingResults = string.Empty;

            foreach (KeyValuePair<string, int> data in _dicPingResults)
            {
                _pingResults += data.Key + ": " + data.Value;
                _pingResults += "\n";
            }

            _pingComplete = true;
            _resultsUpdated = false;
        }

        void ShowPingResults()
        {
            if (!_resultsUpdated)
            {
                _resultsUpdated = true;
                _text.text = "press f5 to refresh" + "\n\n";
                _text.text += _pingResults;
            }
        }

        public void OnConnected()
        {

        }

        public void OnConnectedToMaster()
        {

        }

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