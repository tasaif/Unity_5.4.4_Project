API
/object
/object/load?bundle_path
/object/find/:term
/object/:id
/object/:id/position?x=0.5&y=1&z=5
/object/:id/localEulerAngles?x=90&y=180&z=270
/object/:id/destroy
/spawn/:prefab?x=0&y=0&z=0

//Net APIs not currently available due to PUN 1 / Unity 5 compatibility
/net	// Gets current: master, room, players
/net/room
/net/room/:id
/net/room/:id/join

//Points of Interest
SpawnTieBreaker : Disable Tiebreaker missile
NetworkRegionSelection : Spawn modded cab
ConnectToRandomLounge : Room connection code
OverwatchGameManager : 
GameBrowser : Lobby shuttles
GroupMatchRing : Party Shuttle Code

//How to add mod to DLL
	// Navigate to PlayArea in dnSpy	

	Mod.HttpServer http_server;
	// Token: 0x06000BE1 RID: 3041 RVA: 0x0003A79C File Offset: 0x0003899C
	private void Start()
	{
		Settings.Log(PhotonNetwork.room.name);
		Settings.Log("Scene: " + PhotonNetwork.room.customProperties["scene"]);
		Settings.Log("Map: " + PhotonNetwork.room.customProperties["map"]);
		if (this.http_server == null && PhotonNetwork.room.customProperties["scene"] != null && PhotonNetwork.room.customProperties["scene"].ToString().Contains("SpacedArena"))
		{
			Settings.Log("Starting HTTP API");
			this.http_server = new HttpServer();
			this.http_server.Start();
		}
		this.hardwareType = VRSDKUtils.GetControllerType();
		try
		{
			SceneContext.Instance.InstanceBinder.Unbind<ControllerType>();
		}
		catch
		{
		}
		SceneContext.Instance.InstanceBinder.Bind<ControllerType>(this.hardwareType);
	}