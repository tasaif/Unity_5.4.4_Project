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
        $retval = (Invoke-WebRequest -Uri $_uri -TimeoutSec 20)
        return $retval;
    }
    [PSCustomObject] Find($_term){
        return $this.Request("object/find/$($this.Encode($_term))");
    }    
}

$app = [Application]::new("http://localhost:8000");
$tree = $app.Request("object");
$app.Request("object/load?bundle_path=dynamicbundle.json")