using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;

public class ExistingScript : MonoBehaviour
{
    Mod.HttpServer http_server;
    
    void Start()
    {
        //PhotonNetwork.ConnectUsingSettings("1");
        //http_server = new Mod.HttpServer();
        //http_server.Start();
        GameObject map = GameObject.Find("building002");
        /*Mod.DynamicBundle dynamicBundle = new Mod.DynamicBundle();
        dynamicBundle.initialize(map);
        File.WriteAllText("dynamicbundle.json", JsonConvert.SerializeObject(dynamicBundle));
        map.SetActive(false);*/
        Mod.DynamicBundle imported_obj;
        imported_obj = JsonConvert.DeserializeObject<Mod.DynamicBundle>(File.ReadAllText("dynamicbundle.json"));
        imported_obj.Load();
    }

    // Update is called once per frame
    void Update()
    {
        //repl.Update();
        //http_server.Update();
    }

    private void OnDestroy()
    {
        //http_server.Destroy();
    }
}
