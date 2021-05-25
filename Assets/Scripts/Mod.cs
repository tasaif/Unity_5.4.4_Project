/*
 * https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace Mod
{
    public static class Settings
    {
        // Token: 0x04001AFE RID: 6910
        public static string version = "1";

        public static void Log(string msg)
        {
            StreamWriter sw = new StreamWriter("mod.log", true);
            sw.WriteLine(msg);
            sw.Close();
            Debug.Log(msg);
        }
    }

    class Api1
    {
        public HttpServer http_server;

        /*public class SerializablePlayer
        {
            public string name;
            public Dictionary<string, string> customProperties;
            public SerializablePlayer(PhotonPlayer player)
            {
                customProperties = new Dictionary<string, string>();
                name = player.name;
                foreach(var key in player.customProperties.Keys)
                {
                    customProperties[key.ToString()] = player.customProperties[key.ToString()].ToString();
                }
            }
        }

        public class SerializableRoom
        {
            public string name;
            public Dictionary<string, string> customProperties;
            public SerializableRoom(Room room)
            {
                if (room == null) return;
                name = room.name;
                customProperties = new Dictionary<string, string>();
                foreach (var key in room.customProperties.Keys)
                {
                    customProperties[key.ToString()] = room.customProperties[key.ToString()].ToString();
                }
            }
        }

        public class SerializableNetwork
        {
            // Master
            // Room
            // Players
            // Player
            public string CurrentRoomName;
            public string lobbyName;
            public SerializablePlayer LocalPlayer;
            public List<SerializablePlayer> PlayerList;
            public SerializableRoom CurrentRoom;
            public static SerializableNetwork Current()
            {
                SerializableNetwork retval = new SerializableNetwork();
                if (PhotonNetwork.lobby != null) retval.lobbyName = PhotonNetwork.lobby.Name;
                retval.LocalPlayer = new SerializablePlayer(PhotonNetwork.player);
                retval.PlayerList = new List<SerializablePlayer>();
                foreach(var player in PhotonNetwork.otherPlayers)
                {
                    retval.PlayerList.Add(new SerializablePlayer(player));
                }
                retval.CurrentRoom = new SerializableRoom(PhotonNetwork.room);
                return retval;
            }
        }*/

        public class SerializableGameObject
        {
            public int id;
            public string name;
            public Dictionary<string, string> transform;
            public List<string> components;
            public string mesh;
            bool mesh_loaded = false;
            public SerializableGameObject(GameObject gobj)
            {
                id = gobj.transform.GetInstanceID();
                name = gobj.name;
                transform = new Dictionary<string, string>();
                transform["position"] = gobj.transform.position.ToString();
                transform["localScale"] = gobj.transform.localScale.ToString();
                transform["eulerAngles"] = gobj.transform.eulerAngles.ToString();
                components = new List<string>();
                foreach (var component in gobj.GetComponents<Component>())
                {
                    components.Add("'" + component.GetType() + "'");
                }
            }
            
        }

        public static string GameObjectToJSON(GameObject gobj)
        {
            if (gobj == null)
            {
                return "{}";
            }
            SerializableGameObject sgobj = new SerializableGameObject(gobj);
            string retval = JsonConvert.SerializeObject(sgobj, Formatting.Indented);
            return retval;
        }

        public void Process(ref HttpListenerContext context)
        {
            Settings.Log("Request: " + context.Request.RawUrl);
            HttpServer.ApiRequest request = new HttpServer.ApiRequest(ref context);
            http_server.requests_queue.Add(request);
            var request_in_time = DateTime.Now;
            var timeout = DateTime.Now;
            timeout = timeout.AddSeconds(20);
            request.context.Response.ContentType = "text/html";
            request.context.Response.ContentEncoding = Encoding.UTF8;
            while (true)
            {
                if (request.done)
                {
                    break;
                } else if (DateTime.Now > timeout)
                {
                    Settings.Log("Request timed out");
                    request.context.Response.StatusCode = 500;
                    break;
                }
                Thread.Sleep(100);
            }
            byte[] data = Encoding.UTF8.GetBytes(request.string_response);
            request.context.Response.ContentLength64 = data.LongLength;
            request.context.Response.OutputStream.Write(data, 0, data.Length);
            request.context.Response.Close();
            http_server.requests_queue.Remove(request);
        }

        public static List<GameObject> FindRootObjects()
        {
            List<GameObject> retval = new List<GameObject>();
            GameObject[] gameobjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject gameobject in gameobjects)
            {
                if (gameobject.transform.parent == null)
                {
                    retval.Add(gameobject);
                }
            }
            return retval;
        }

        public static List<GameObject> GetChildren(GameObject node)
        {
            List<GameObject> retval = new List<GameObject>();
            foreach (Transform _node in node.transform)
            {
                retval.Add(_node.gameObject);
            }
            return retval;
        }

        private static void get_tree_helper(ref string retval, List<GameObject> nodes)
        {
            List<string> _retval = new List<string>();
            foreach (var node in nodes)
            {
                string node_as_string = GameObjectToJSON(node);
                node_as_string = node_as_string.Trim().TrimEnd('}');
                node_as_string += ", 'children': [";
                get_tree_helper(ref node_as_string, GetChildren(node));
                node_as_string += "]}";
                _retval.Add(node_as_string);
            }
            retval += string.Join(",", _retval.ToArray());
        }

        public static string GetTree()
        {
            string retval = "[";
            List<GameObject> tree = FindRootObjects();
            get_tree_helper(ref retval, tree);
            retval += "]";
            return retval;
        }
    }

    class PositionObjectRequest
    {
        public int id;
        public HttpListenerRequest req;
        public PositionObjectRequest(int _id, HttpListenerRequest _req)
        {
            id = _id;
            req = _req;
        }
    }

    class LoadObjectRequest
    {
        public string obj_path;
        public string tex_path;
        public LoadObjectRequest(string _obj_path, string _tex_path)
        {
            obj_path = _obj_path;
            tex_path = _tex_path;
        }
    }

    class HttpServer
    {
        Api1 api1;
        Thread t;
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public List<ApiRequest> requests_queue;

        public class ApiRequest
        {
            public HttpListenerContext context;
            public bool done = false;
            public string string_response = "";
            public ApiRequest(ref HttpListenerContext _context)
            {
                context = _context;
            }
        }

        public static void HandleIncomingConnections(HttpServer http_server)
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                HttpListenerContext ctx = listener.GetContext();
                byte[] data = Encoding.UTF8.GetBytes("");
                List<string> request_chunks = new List<string>(ctx.Request.Url.AbsolutePath.Trim('/').Split('/'));
                if (request_chunks[0] != "api")
                {
                    ctx.Response.StatusCode = 422;
                }
                else
                {
                    http_server.api1.Process(ref ctx);
                }
            }
        }

        public static void ThreadProc(System.Object http_server)
        {
            HttpServer _http_server = (HttpServer)http_server;
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Settings.Log("Listening for connections on " + url);

            // Handle requests
            HandleIncomingConnections(_http_server);

            // Close the listener
            listener.Close();
        }

        public void Start()
        {
            api1 = new Api1();
            api1.http_server = this;
            requests_queue = new List<ApiRequest>();
            t = new Thread(ThreadProc);
            t.Start(this);
        }

        public void HandleRequest(ref ApiRequest request)
        {
            List<string> request_chunks = new List<string>(request.context.Request.Url.AbsolutePath.Trim('/').Split('/'));
            List<string> chunks = request_chunks.GetRange(2, request_chunks.Count - 2);
            request.context.Response.StatusCode = 501;
            switch (chunks[0])
            {
                /*case "network":
                    switch (chunks.Count)
                    {
                        case 1:
                            request.string_response = JsonConvert.SerializeObject(Api1.SerializableNetwork.Current(), Formatting.Indented);
                            request.context.Response.StatusCode = 200;
                            break;
                    }
                    break;*/
                case "object":
                    int instance_id;
                    GameObject gobj = null;
                    if (chunks.Count > 1 && int.TryParse(chunks[1], out instance_id))
                    {
                        gobj = (GameObject)FindObjectFromInstanceID(instance_id);
                    }
                    switch (chunks.Count)
                    {
                        case 1:
                            request.string_response = Api1.GetTree();
                            request.context.Response.StatusCode = 200;
                            break;
                        case 2:
                            if (gobj != null)
                            {
                                request.string_response = Api1.GameObjectToJSON(gobj);
                                request.context.Response.StatusCode = 200;
                            }
                            else if (chunks[1] == "load")
                            {
                                string bundle_path = request.context.Request.QueryString["bundle_path"];
                                Settings.Log("Loading: " + bundle_path);
                                Mod.DynamicBundle imported_obj;
                                imported_obj = JsonConvert.DeserializeObject<Mod.DynamicBundle>(File.ReadAllText(bundle_path));
                                Settings.Log("deserialized");
                                imported_obj.Load();
                                Settings.Log("creating runtime objects");
                                request.context.Response.StatusCode = 200;
                                /*ObjImporter oi = new ObjImporter();
                                gobj = oi.LoadObject(request.context.Request.QueryString["obj_path"], request.context.Request.QueryString["texture_path"]);
                                request.string_response = Api1.GameObjectToJSON(gobj);
                                request.context.Response.StatusCode = 200;*/
                            }
                            break;
                        case 3:
                            if (chunks[1] == "find")
                            {
                                string find_string = "";
                                try
                                {
                                    find_string = Encoding.Unicode.GetString(Convert.FromBase64String(chunks[2]));
                                }
                                catch
                                {
                                    request.context.Response.StatusCode = 422;
                                }
                                if (request.context.Response.StatusCode != 422)
                                {
                                    gobj = GameObject.Find(find_string);
                                    if (gobj == null)
                                    {
                                        request.context.Response.StatusCode = 404;
                                    }
                                    else
                                    {
                                        request.string_response = Api1.GameObjectToJSON(gobj);
                                        request.context.Response.StatusCode = 200;
                                    }
                                }
                            } else if (chunks[2] == "position")
                            {
                                Vector3 position = gobj.transform.position;
                                var qs = request.context.Request.QueryString;
                                foreach (string key in qs.AllKeys)
                                {
                                    switch (key)
                                    {
                                        case "x":
                                            position.x = float.Parse(qs["x"]);
                                            break;
                                        case "y":
                                            position.y = float.Parse(qs["y"]);
                                            break;
                                        case "z":
                                            position.z = float.Parse(qs["z"]);
                                            break;
                                    }
                                }
                                gobj.transform.position = position;
                                request.context.Response.StatusCode = 200;
                            }
                            else if (chunks[2] == "localEulerAngles")
                            {
                                Vector3 localEulerAngles = gobj.transform.localEulerAngles;
                                var qs = request.context.Request.QueryString;
                                foreach (string key in qs.AllKeys)
                                {
                                    switch (key)
                                    {
                                        case "x":
                                            localEulerAngles.x = float.Parse(qs["x"]);
                                            break;
                                        case "y":
                                            localEulerAngles.y = float.Parse(qs["y"]);
                                            break;
                                        case "z":
                                            localEulerAngles.z = float.Parse(qs["z"]);
                                            break;
                                    }
                                }
                                gobj.transform.localEulerAngles = localEulerAngles;
                                request.context.Response.StatusCode = 200;
                            }
                            if (chunks[2] == "destroy")
                            {
                                Settings.Log("Destroying GameObject " + gobj.name);
                                GameObject.DestroyImmediate(gobj);
                                request.context.Response.StatusCode = 200;
                            }
                            break;
                    }
                    break;
            }
            request.done = true;
        }

        public void HandleRequests()
        {
            List<ApiRequest> thread_safe_queue = new List<ApiRequest>(requests_queue); // Make a copy because the other thread deletes items from this list
            for(int i=0; i<requests_queue.Count; i++)
            {
                ApiRequest api_request = thread_safe_queue[i];
                if (api_request.done) continue;
                try
                {
                    HandleRequest(ref api_request);
                } catch (Exception e)
                {
                    try
                    {
                        api_request.context.Response.StatusCode = 500;
                    } catch (ObjectDisposedException _e)
                    {
                        Settings.Log("Request response doesn't exist anymore");
                    }
                    api_request.string_response = e.ToString();
                    api_request.done = true;
                }
            }
        }

        /*
         * https://answers.unity.com/questions/34929/how-to-find-object-using-instance-id-taken-from-ge.html
         * http://digitalopus.ca/UnityAssets/SelectObjWithGID.cs
         */
        public static UnityEngine.Object FindObjectFromInstanceID(int iid)
        {
            
            UnityEngine.Transform[] all = (UnityEngine.Transform[])UnityEngine.Object.FindObjectsOfType(typeof(UnityEngine.Transform));
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].GetInstanceID() == iid)
                {
                    return all[i].gameObject;
                }
            }
            return null;
        }

        /*public void ShareInformationWithOtherClients()
        {
            if (PhotonNetwork.player.customProperties.ContainsKey("mod_version")) return;
            ExitGames.Client.Photon.Hashtable information = new ExitGames.Client.Photon.Hashtable();
            information["mod_version"] = Settings.version;
            PhotonNetwork.player.SetCustomProperties(information);
        }*/

        public void Update()
        {
            HandleRequests();
            //ShareInformationWithOtherClients();
        }

        public void Destroy()
        {
            t.Abort();
        }
    }

}