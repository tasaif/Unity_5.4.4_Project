class Application {
    [string]$uri;
    [string]$prefix = "api/1";
    [PSCustomObject]$foo;
    Application([string]$_uri){
        $this.uri = $_uri;
    }
    [string]Encode($text){
        $Bytes = [System.Text.Encoding]::Unicode.GetBytes($Text)
        return [Convert]::ToBase64String($Bytes)
    }
    [PSCustomObject]Request($resource){
        $retval = @{}
        $_uri = "$($this.uri)/$($this.prefix)/$($resource)";
        $retval = (Invoke-WebRequest -Uri $_uri -TimeoutSec 20);
        return $retval;
    }
    [PSCustomObject] Find($_term){
        try {
            $response = $this.Request("object/find/$($this.Encode($_term))");
            return $response.Content | ConvertFrom-Json
        } catch {
            return $null;
        }

    }
    [PSCustomObject] Destroy($_term){
        $obj = $this.Find($_term);
        if ($obj -eq $null){
            Write-Host "Object '$_term' not found";
            return $null;
        }
        $this.Request("object/$($obj.id)/destroy");
        return $obj;
    }
    [PSCustomObject] LoadBundle($fname){
        return $this.Request("object/load?bundle_path=$fname").Content | ConvertFrom-Json
    }
    [PSCustomObject] SetPosition($obj, $x, $y, $z){
        $request = "object/$($obj.id)/position?";
        if ($x -ne $null) {$request = "$request&x=$x"};
        if ($y -ne $null) {$request = "$request&y=$y"};
        if ($z -ne $null) {$request = "$request&z=$z"};
        return $this.Request($request).Content | ConvertFrom-Json;
    }
    [PSCustomObject] SetRotation($obj, $x, $y, $z){
        $request = "object/$($obj.id)/localEulerAngles?";
        if ($x -ne $null) {$request = "$request&x=$x"};
        if ($y -ne $null) {$request = "$request&y=$y"};
        if ($z -ne $null) {$request = "$request&z=$z"};
        return $this.Request($request).Content | ConvertFrom-Json;
    }
    [PSCustomObject] get_tree(){
        $tree = $this.Request("object");
        $tree_json = $tree.Content | convertfrom-json
        return $tree_json
    }
    [void] Dump($objs, $indent = 0){
        for($i=0; $i -lt $objs.Count; $i++){
            $obj = $objs[$i];
            Write-Host "$("`t"*$indent)$($obj.id): $($obj.name)"
            Dump($obj.children, $indent + 1)
        }
    }
}



$app = [Application]::new("http://localhost:8001");
return
$objects_to_delete = @(
    "LobbyRoom",
    "Buildings",
    "Clouds",
    "Clouds (1)",
    "Mirror",
    "StoryCab"
    "ChallengeSign"
)
foreach($object in $objects_to_delete){
    $app.Destroy($object);
}

#$styles = $app.Find("Styles").Content | ConvertFrom-Json # Avatar Picker
$app.LoadBundle("building003.json")
$map = $app.Find("building003")
$tracking_space = $app.Find("TrackingSpace")
$lounge_cab = $app.Find("LoungeCab")
$lounge_sign = $app.Find("MultiplayerSign")
$tutorial_cab = $app.Find("TutorialCab")
$music = $app.Find("Music")
$lighting = $app.Find("Lighting")
$arena = $app.Find("Arena")
$lobby = $app.Find("Lobby")

$app.SetPosition($map, 0, 20, 0)
$app.SetPosition($tracking_space, 0.4, 21.3, -0.1);
$app.SetPosition($lounge_cab, 5.5, 22.0, -0.5);
$app.SetPosition($lounge_sign, 5.5, 25.0, -0.5);
$app.SetPosition($tutorial_cab, -6, 22.0, -0.5);
$app.SetPosition($tutorial_sign, -6, 25, -.5); 
$app.SetRotation($tutorial_cab, 0, -90, 0) 
$app.SetRotation($tutorial_sign, 0, -90, 0) 
$app.SetPosition($arena, 4, 21, 15)
$app.SetPosition($lobby, 0, 10, 0)