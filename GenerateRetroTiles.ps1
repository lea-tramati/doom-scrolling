Add-Type -AssemblyName System.Drawing

# ═══════════════════════════════════════════════════════════════════
# Regenerates ONLY the wall tilesheet, swapping the neon-pink corridor
# border for a dark gray/black outline. Deliberately standalone (does
# NOT run the rest of GenerateSprites.ps1) so it can't clobber the
# chibi Player/LikeCreature art with the old downloaded source image.
# ═══════════════════════════════════════════════════════════════════
function C([int]$r,[int]$g,[int]$b,[int]$a=255){ [System.Drawing.Color]::FromArgb($a,$r,$g,$b) }

$Red        = C 0xFF 0x3A 0x5E
$WallFill   = C 0x1C 0x0F 0x30
$FloorColor = C 0x12 0x0F 0x1E

# Dark gray/black outline (replaces the old bright-pink corridor border)
$WallBorder     = C 0x55 0x55 0x60   # bright edge (was $Pink)
$WallBorderDim  = C 0x30 0x30 0x38   # mid gradient (was $PinkDim)
$WallBorderDeep = C 0x12 0x0F 0x1E   # deep/near-black (was $PinkDeep)
$JoinGray       = C 0x2A 0x2A 0x32   # wall-to-wall join color (was $VioletDim)

$outDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\_Sprites"

function NewBmp([int]$w,[int]$h){ [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Save([System.Drawing.Bitmap]$b,[string]$p){ $b.Save($p,[System.Drawing.Imaging.ImageFormat]::Png); Write-Host "  >> $([IO.Path]::GetFileName($p))" }
function Px([System.Drawing.Bitmap]$b,[int]$x,[int]$y,$c){ if($x-ge 0-and$x-lt$b.Width-and$y-ge 0-and$y-lt$b.Height){$b.SetPixel($x,$y,$c)} }
function Rect([System.Drawing.Bitmap]$b,[int]$x,[int]$y,[int]$w,[int]$h,$c){ for($j=$y;$j-lt$y+$h;$j++){for($i=$x;$i-lt$x+$w;$i++){Px $b $i $j $c}} }
function Bord([System.Drawing.Bitmap]$b,[int]$x,[int]$y,[int]$w,[int]$h,$c,[int]$t=1){
    for($n=0;$n-lt$t;$n++){
        for($i=$x+$n;$i-lt$x+$w-$n;$i++){Px $b $i ($y+$n) $c; Px $b $i ($y+$h-1-$n) $c}
        for($j=$y+$n;$j-lt$y+$h-$n;$j++){Px $b ($x+$n) $j $c; Px $b ($x+$w-1-$n) $j $c}
    }
}

function WallTile([System.Drawing.Bitmap]$b,[int]$x0,[int]$y0,[bool]$gT,[bool]$gB,[bool]$gL,[bool]$gR){
    Rect $b $x0 $y0 16 16 $WallFill

    for($i=0;$i-lt 16;$i++){
        Px $b ($x0+$i) $y0      $JoinGray
        Px $b ($x0+$i) ($y0+15) $JoinGray
        Px $b $x0 ($y0+$i)      $JoinGray
        Px $b ($x0+15) ($y0+$i) $JoinGray
    }

    if($gT){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) $y0       $WallBorder; Px $b ($x0+$i) ($y0+1) $WallBorderDim; Px $b ($x0+$i) ($y0+2) $WallBorderDeep } }
    if($gB){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) ($y0+15)  $WallBorder; Px $b ($x0+$i) ($y0+14) $WallBorderDim; Px $b ($x0+$i) ($y0+13) $WallBorderDeep } }
    if($gL){ for($j=0;$j-lt 16;$j++){ Px $b $x0 ($y0+$j)       $WallBorder; Px $b ($x0+1) ($y0+$j) $WallBorderDim; Px $b ($x0+2) ($y0+$j) $WallBorderDeep } }
    if($gR){ for($j=0;$j-lt 16;$j++){ Px $b ($x0+15) ($y0+$j)  $WallBorder; Px $b ($x0+14) ($y0+$j) $WallBorderDim; Px $b ($x0+13) ($y0+$j) $WallBorderDeep } }
}

Write-Host "[1] Tilesheet (dark gray/black wall borders)"
$S = NewBmp 16 224

WallTile $S 0   0  $true  $true  $false $false
WallTile $S 0  16  $false $false $true  $true
WallTile $S 0  32  $true  $false $true  $false
WallTile $S 0  48  $true  $false $false $true
WallTile $S 0  64  $false $true  $true  $false
WallTile $S 0  80  $false $true  $false $true
WallTile $S 0  96  $true  $false $false $false
WallTile $S 0 112  $false $true  $false $false
WallTile $S 0 128  $false $false $true  $false
WallTile $S 0 144  $false $false $false $true
WallTile $S 0 160  $false $false $false $false

$y0=176; Rect $S 0 $y0 16 16 $FloorColor
Px $S 0 $y0 (C 0x1A 0x12 0x26); Px $S 15 $y0 (C 0x1A 0x12 0x26)
Px $S 0 ($y0+15) (C 0x1A 0x12 0x26); Px $S 15 ($y0+15) (C 0x1A 0x12 0x26)

$y0=192; Rect $S 0 $y0 16 16 $FloorColor
Rect $S 0 ($y0+7) 16 2 (C 0x22 0x14 0x3A)
Rect $S 2 ($y0+7) 12 1 (C 0x30 0x18 0x50)

$y0=208; Rect $S 0 $y0 16 16 $FloorColor
Bord $S 0 $y0 16 16 $Red 1
Bord $S 2 ($y0+2) 12 12 (C 0x50 0x08 0x18) 1
for($py=4;$py-le 9;$py++){ Px $S 7 ($y0+$py) $Red; Px $S 8 ($y0+$py) $Red }
Px $S 7 ($y0+11) $Red; Px $S 8 ($y0+11) $Red

Save $S "$outDir\Tilesheet\DoomScrolling_Tiles.png"; $S.Dispose()
Write-Host "Done."
