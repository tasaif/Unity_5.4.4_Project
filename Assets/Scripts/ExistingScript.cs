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
        http_server = new Mod.HttpServer();
        http_server.Start();
        string object_name = "building003";
        string fname = object_name + ".json";
        GameObject map = GameObject.Find(object_name);
        Mod.DynamicBundle dynamicBundle = new Mod.DynamicBundle();
        dynamicBundle.initialize(map);
        File.Delete(fname);
        File.WriteAllText(fname, JsonConvert.SerializeObject(dynamicBundle));
        map.SetActive(false);
        Mod.DynamicBundle imported_obj;
        string json = File.ReadAllText(fname);
        try
        {
            imported_obj = JsonConvert.DeserializeObject<Mod.DynamicBundle>(json);
            imported_obj.Load();
        } catch (System.Exception e)
        {
            Debug.Log(json.Length.ToString() + " characters in json file");
            Debug.Log(e.ToString());
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //repl.Update();
        http_server.Update();
    }

    private void OnDestroy()
    {
        http_server.Destroy();
    }
}
