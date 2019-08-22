using ARPeerToPeerSample.Network;
using System;
using System.Text;
using UnityEngine;

namespace ARPeerToPeerSample.Game
{
    public class GameController : MonoBehaviour
    {
        private NetworkManagerBase _networkManager;
        private bool hasNetworkAuthority = false;
        private bool hostEstablished = false;

        [SerializeField, Tooltip("Wifi object for Android")]
        private GameObject _androidWifiObject;

        [SerializeField, Tooltip("Menu view logic object")]
        private MenuViewLogic _menuViewLogic;

        [SerializeField, Tooltip("Cube object")]
        private GameObject _cube;

        private void Awake()
        {
#if UNITY_ANDROID
            GameObject androidNetworkGO = Instantiate(_androidWifiObject);
            _networkManager = new NetworkManagerAndroid(androidNetworkGO.GetComponent<WifiDirectImpl>());
#elif UNITY_IOS
            _networkManager = new NetworkManageriOS();
#endif
           // _networkManager = new NetworkManageriOS(); //REMOVE (ONLY FOR DEBUG)
            _networkManager.ServiceFound += OnServiceFound;
            _networkManager.ConnectionEstablished += OnConnectionEstablished;
            _networkManager.MessageReceived += OnMessageReceived;
            _networkManager.Start();

            _menuViewLogic.ConnectionButtonPressed += OnConnectionButtonPressed;
            _menuViewLogic.ChangeColorButtonPressed += OnChangeColorAndSendMessage;
            _menuViewLogic.HostButtonPressed += OnHostSendMessage;
        }

        private void OnServiceFound(string serviceAddress)
        {
            _menuViewLogic.SetConnectionName(serviceAddress);
        }

        private void OnConnectionButtonPressed()
        {
            _networkManager.Connect();
        }

        private void OnConnectionEstablished()
        {
            _menuViewLogic.SetStateConnectionEstablished();
        }

        private void OnMessageReceived(byte[] message)
        {
            byte messageType = message[0];
            byte[] messageBytes = new byte[message.Length - 1];
            Array.Copy(message, 1, messageBytes, 0, message.Length - 1);

            string debugInfo = Encoding.UTF8.GetString(message);
            _menuViewLogic.SetStateDebugInfo("got: " + messageType + " should be: " + NetworkManagerBase.NET_MESSAGE_TYPES.SendColor);

            switch (messageType)
            {
                case (int)NetworkManagerBase.NET_MESSAGE_TYPES.SendColor:
                    ReceivedColor(messageBytes);
                    break;
                case (int)NetworkManagerBase.NET_MESSAGE_TYPES.SetHost:
                    ReceivedSetHost(messageBytes);
                    break;
                case (int)NetworkManagerBase.NET_MESSAGE_TYPES.SendMovement:
                    break;
                case (int)NetworkManagerBase.NET_MESSAGE_TYPES.ParticleRPC:
                    break;
                default:
                    break;
            }
        }

        private void ReceivedColor(byte[] message)
        {
            string color = Encoding.UTF8.GetString(message);
            print("received color: " + color);
            SetColor(_cube.GetComponent<Renderer>(), StringToColor(color));
        }

        private void ReceivedSetHost(byte[] message)
        {
            if (hostEstablished)
            {
                return;
            }

            hostEstablished = true;
            hasNetworkAuthority = false;

            SetAuthorityObjects();
        }

        private void OnChangeColorAndSendMessage()
        {
            string colorToSend = string.Empty;
            int colorToSendNum = UnityEngine.Random.Range(0, 3);
            if (colorToSendNum == 0)
            {
                colorToSend = "red";
            }
            else if (colorToSendNum == 1)
            {
                colorToSend = "blue";
            }
            else
            {
                colorToSend = "green";
            }

            SetColor(_cube.GetComponent<Renderer>(), StringToColor(colorToSend));

            byte[] colorToSendBytes = Encoding.UTF8.GetBytes(colorToSend);

            byte[] colorMessage = new byte[colorToSendBytes.Length + 1];
            colorMessage[0] = (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SendColor;

            Buffer.BlockCopy(colorToSendBytes, 0, colorMessage, 0, colorToSendBytes.Length);
            _networkManager.SendMessage(colorMessage);

            _menuViewLogic.SetStateDebugInfo("sending: " + (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SendColor);
        }

        private void OnHostSendMessage()
        {
            if (hostEstablished)
            {
                return;
            }

            byte[] setHostMessage = new byte[] { (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SetHost };
            _networkManager.SendMessage(setHostMessage);

            hostEstablished = true;
            hasNetworkAuthority = true;

            SetAuthorityObjects();
        }

        // todo: this is pretty dumb. just send the color bits
        private Color StringToColor(string color)
        {
            switch (color)
            {
                case "red":
                    return Color.red;
                case "blue":
                    return Color.blue;
                case "green":
                    return Color.green;
                default:
                    return Color.magenta;
            }
        }

        private void SetColor(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();

            // You can look up the property by ID instead of the string to be more efficient.
            block.SetColor("_BaseColor", color);

            // You can cache a reference to the renderer to avoid searching for it.
            renderer.SetPropertyBlock(block);
        }

        private void SetAuthorityObjects()
        {
            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("NetworkMoveObj");

            for(int i = 0; i < gameObjects.Length; i++)
            {
                gameObjects[i].GetComponent<MovementObj>()?.SetNetworkAuthority(hasNetworkAuthority);
            }
        }
    }
}