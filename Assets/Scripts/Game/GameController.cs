using ARPeerToPeerSample.Network;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace ARPeerToPeerSample.Game
{
    public class GameController : MonoBehaviour
    {
        private NetworkManagerBase _networkManager;
        private bool hasNetworkAuthority = false;
        private bool hostEstablished = false;
        Dictionary<String, GameObject> netObjectsDict = new Dictionary<String, GameObject>();
        Dictionary<String, MovementObj> netObjectScriptDict = new Dictionary<string, MovementObj>();
        List<GameObject> netObjects = new List<GameObject>();
        List<MovementObj> netObjectScripts = new List<MovementObj>();
        private float netTime = 0;
        private float netRate = 0.05f; //in MS
        private float randomParticleTimer = 0.05f;
        private float particleTime = 0f;
        private Vector3 spawnPos = new Vector3(-1, -1, -8);
        public GameObject NetObjectClass;
        private String NetObjBaseName = "NetObj";

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
            //_networkManager = new NetworkManageriOS(); //REMOVE (ONLY FOR DEBUG)
            _networkManager.ServiceFound += OnServiceFound;
            _networkManager.ConnectionEstablished += OnConnectionEstablished;
            _networkManager.MessageReceived += OnMessageReceived;
            _networkManager.Start();

            _menuViewLogic.ConnectionButtonPressed += OnConnectionButtonPressed;
            _menuViewLogic.ChangeColorButtonPressed += OnChangeColorAndSendMessage;
            _menuViewLogic.HostButtonPressed += OnHostSendMessage;
            _menuViewLogic.SpawnButtonPressed += OnSpawnObject;
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
            _menuViewLogic.SetStateDebugInfo("received: " + debugInfo);

            switch (messageType)
            {
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SendColor:
                    ReceivedColor(messageBytes);
                    break;
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SetHost:
                    ReceivedSetHost(messageBytes);
                    break;
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SendMovement:
                    ReceivedMovement(messageBytes);
                    break;
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.ParticleRPC:
                    ReceivedParticleRPC();
                    break;
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SpawnObject:
                    ReceiveNetSpawn(messageBytes);
                    break;
                case (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SpawnObjectReq:
                    RequestSpawnNetObject();
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

            Buffer.BlockCopy(colorToSendBytes, 0, colorMessage, 1, colorToSendBytes.Length); //MUST be offset by one
            _networkManager.SendMessage(colorMessage);
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

        private void ReceivedSetHost(byte[] message)
        {
            if (hostEstablished)
            {
                return;
            }

            hostEstablished = true;
            hasNetworkAuthority = false;

            SetAuthorityObjects();

            _menuViewLogic.SetStateDebugInfo("received: is client");
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

            _menuViewLogic.SetStateDebugInfo("received: is server");
        }


        private void SetAuthorityObjects()
        {
            //netObjects = GameObject.FindGameObjectsWithTag("NetworkMoveObj");
            //netObjectScripts = new MovementObj[netObjects.Length];

            //for (int i = 0; i < netObjects.Length; i++)
            //{
            //    netObjectScripts[i] = netObjects[i].GetComponent<MovementObj>();
            //    netObjectScripts[i].SetNetworkAuthority(hasNetworkAuthority);
            //}
        }

        private void OnSpawnObject()
        {
            if(!hostEstablished)
            {
                return;
            }

            if(hasNetworkAuthority)
            {
                SpawnNetObject();
            }
            else
            {
                RequestSpawnNetObject();
            }
        }

        private void SpawnNetObject()
        {
            GameObject netObj = Instantiate(NetObjectClass, spawnPos, Quaternion.identity);
            netObj.name = NetObjBaseName + (netObjects.Count);

            MovementObj netScript = netObj.GetComponent<MovementObj>();
            netScript.SetNetworkAuthority(hasNetworkAuthority);

            netObjectsDict.Add(netObj.name, netObj);
            netObjects.Add(netObj);
            netObjectScriptDict.Add(netObj.name, netScript);
            netObjectScripts.Add(netScript);

            spawnPos.z -= 8f;

            SendNetSpawn(netObj);
        }

        private void RequestSpawnNetObject()
        {
            SpawnNetObject();
        }

        private void SendNetSpawn(GameObject netObj)
        {
            byte[] objName = Encoding.UTF8.GetBytes(netObj.name);
            int nameLength = objName.Length; //convert to bytes + padding for sending byte length
            byte[] objBytes = new byte[nameLength + 1 + 13]; //bytes + 1 + 4*3 + name length + padding for sending name length 

            objBytes[0] = (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SpawnObject;
            objBytes[1] = (byte)nameLength;

            Buffer.BlockCopy(objName, 0, objBytes, 2, nameLength); //write obj name

            //obj position
            Buffer.BlockCopy(BitConverter.GetBytes(netObj.transform.position.x), 0, objBytes, (2 + nameLength) + (0 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(netObj.transform.position.y), 0, objBytes, (2 + nameLength) + (1 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(netObj.transform.position.z), 0, objBytes, (2 + nameLength) + (2 * sizeof(float)), sizeof(float)); //offset by 1

            _networkManager.SendMessage(objBytes);
        }

        private void ReceiveNetSpawn(byte[] message)
        {
            int nameLength = message[0];
            String netObjName = Encoding.UTF8.GetString(message, 1, nameLength);

            byte[] buff = message;
            spawnPos.x = BitConverter.ToSingle(buff, (1 + nameLength) + (0 * sizeof(float)));
            spawnPos.y = BitConverter.ToSingle(buff, (1 + nameLength) + (1 * sizeof(float)));
            spawnPos.z = BitConverter.ToSingle(buff, (1 + nameLength) + (2 * sizeof(float)));

            GameObject netObj = Instantiate(NetObjectClass, spawnPos, Quaternion.identity);
            netObj.name = netObjName;

            MovementObj netScript = netObj.GetComponent<MovementObj>();
            netScript.SetNetworkAuthority(hasNetworkAuthority);

            netObjectsDict.Add(netObjName, netObj);
            netObjects.Add(netObj);
            netObjectScriptDict.Add(netObjName, netScript);
            netObjectScripts.Add(netScript);
        }

        private void Update()
        {
            netTime += Time.deltaTime;
            particleTime += Time.deltaTime;

            if (netTime > netRate && hostEstablished && hasNetworkAuthority)
            {
                if(particleTime > randomParticleTimer)
                {
                    particleTime = 0f;
                    randomParticleTimer = UnityEngine.Random.Range(1f, 5f);
                    SendParticleRPC();
                }

                foreach(GameObject netObj in netObjects)
                {
                    SendMovement(netObj.name, netObj.transform.position, netObj.transform.Find("Pivot").rotation);
                }

                netTime = 0;
            }
        }

        private void ReceivedParticleRPC()
        {
            for (int i = 0; i < netObjects.Count; i++)
            {
                netObjectScripts[i].ActivateParticles();
            }
        }

        private void SendParticleRPC()
        {
            byte[] setParticleRPC = new byte[] { (byte)NetworkManagerBase.NET_MESSAGE_TYPES.ParticleRPC };
            _networkManager.SendMessage(setParticleRPC);

            for (int i = 0; i < netObjects.Count; i++)
            {
                netObjectScripts[i].ActivateParticles();
            }
        }

        private void SendMovement(String netObjName, Vector3 pos, Quaternion turretRot)
        {
            byte[] objName = Encoding.UTF8.GetBytes(netObjName);
            int nameLength = objName.Length; //convert to bytes + padding for sending byte length

            byte[] posBytes = new byte[nameLength + 1 + 29]; //1 + 4*3 + 4*4

            posBytes[0] = (byte)NetworkManagerBase.NET_MESSAGE_TYPES.SendMovement;
            posBytes[1] = (byte)nameLength;

            Buffer.BlockCopy(objName, 0, posBytes, 2, nameLength); //write obj name

            //obj position
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, posBytes, (2 + nameLength) + (0 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, posBytes, (2 + nameLength) + (1 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(pos.z), 0, posBytes, (2 + nameLength) + (2 * sizeof(float)), sizeof(float)); //offset by 1

            //turret rotation
            Buffer.BlockCopy(BitConverter.GetBytes(turretRot.x), 0, posBytes, (2 + nameLength) + (3 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(turretRot.y), 0, posBytes, (2 + nameLength) + (4 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(turretRot.z), 0, posBytes, (2 + nameLength) + (5 * sizeof(float)), sizeof(float)); //offset by 1
            Buffer.BlockCopy(BitConverter.GetBytes(turretRot.w), 0, posBytes, (2 + nameLength) + (6 * sizeof(float)), sizeof(float)); //offset by 1

            _networkManager.SendMessage(posBytes);
        }

        private void ReceivedMovement(byte[] message)
        {
            int nameLength = message[0];
            String NetObjName = Encoding.UTF8.GetString(message, 1, nameLength);

            byte[] buff = message;
            Vector3 vect = Vector3.zero;
            vect.x = BitConverter.ToSingle(buff, (1 + nameLength) + (0 * sizeof(float)));
            vect.y = BitConverter.ToSingle(buff, (1 + nameLength) + (1 * sizeof(float)));
            vect.z = BitConverter.ToSingle(buff, (1 + nameLength) + (2 * sizeof(float)));

            Quaternion turretRot = Quaternion.identity;
            turretRot.x = BitConverter.ToSingle(buff, (1 + nameLength) + (3 * sizeof(float)));
            turretRot.y = BitConverter.ToSingle(buff, (1 + nameLength) + (4 * sizeof(float)));
            turretRot.z = BitConverter.ToSingle(buff, (1 + nameLength) + (5 * sizeof(float)));
            turretRot.w = BitConverter.ToSingle(buff, (1 + nameLength) + (6 * sizeof(float)));

            netObjectScriptDict[NetObjName]?.NetUpdate(vect, turretRot);
        }
    }
}