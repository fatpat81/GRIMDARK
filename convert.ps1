Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("c:\Users\pat\Documents\Warhammer App\wh40k_logo.png")
$bmp = new-object System.Drawing.Bitmap($img)
$iconStream = new-object System.IO.FileStream("c:\Users\pat\Documents\Warhammer App\icon.ico", [System.IO.FileMode]::Create)
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$icon.Save($iconStream)
$iconStream.Close()
$bmp.Dispose()
$img.Dispose()
