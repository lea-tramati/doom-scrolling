Add-Type -AssemblyName System.Drawing

# ── Palette ──────────────────────────────────────────────────────────────
function C([int]$r,[int]$g,[int]$b,[int]$a=255) { [System.Drawing.Color]::FromArgb($a,$r,$g,$b) }

$Pink      = C 0xFF 0x4D 0x90
$Purple    = C 0x81 0x15 0xFF
$Violet    = C 0x78 0x6C 0xF6
$BgBlk     = C 0x12 0x0F 0x1E
$DeepBg    = C 0x0D 0x0A 0x1C
$Ghost     = C 0xF7 0xD8 0xFF
$Blue      = C 0x00 0xF5 0xFF
$Red       = C 0xFF 0x3A 0x5E
$Clear     = [System.Drawing.Color]::Transparent
$Navy      = C 0x1A 0x10 0x40
$dimV      = C 0x2A 0x26 0x5A

# Wall-specific
$WallFill  = C 0x16 0x0D 0x2E   # dark purple wall interior
$WallEdge  = C 0x3A 0x2A 0x6A   # muted violet — wall-to-wall edge (seamless join)
$NeonBrt   = C 0x00 0xF5 0xFF   # bright cyan — corridor-facing row 0
$NeonMid   = C 0x00 0x88 0xAA   # mid cyan  — corridor-facing row 1
$NeonDim   = C 0x00 0x3A 0x50   # dim cyan  — corridor-facing row 2

# Collectible neon
$DotCore   = C 0xFF 0x4D 0x90
$DotGlow   = C 0xFF 0x90 0xB8
$DotOuter  = C 0x88 0x10 0x3A 100   # semi-transparent pink glow

# Hazard
$AdRed     = C 0xFF 0x10 0x30
$gray      = C 0x44 0x44 0x50

# BG city
$Sky0      = C 0x08 0x05 0x14   # top sky (near black)
$Sky1      = C 0x12 0x08 0x28   # mid sky
$Sky2      = C 0x1A 0x0D 0x35   # horizon
$BuildDk   = C 0x0E 0x07 0x1F   # building dark
$BuildMid  = C 0x18 0x0E 0x2E   # building mid
$WinCyan   = C 0x00 0xD5 0xDD   # window neon cyan
$WinPink   = C 0xFF 0x50 0x88   # window neon pink
$WinViol   = C 0x90 0x60 0xFF   # window neon violet
$WinYel    = C 0xFF 0xEC 0x40   # window yellow
$StarC     = C 0xE8 0xCC 0xFF 200

$outDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\_Sprites"

function MkD([string]$p){ if(-not(Test-Path $p)){New-Item -ItemType Directory -Force -Path $p|Out-Null} }
MkD "$outDir\Tilesheet"; MkD "$outDir\Character"; MkD "$outDir\Enemies"
MkD "$outDir\Collectibles"; MkD "$outDir\Hazards"; MkD "$outDir\UI"; MkD "$outDir\Background"

function NewBmp([int]$w,[int]$h){ return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Save([System.Drawing.Bitmap]$b,[string]$p){ $b.Save($p,[System.Drawing.Imaging.ImageFormat]::Png); Write-Host "  >> $([IO.Path]::GetFileName($p))" }
function Px([System.Drawing.Bitmap]$b,[int]$x,[int]$y,$c){ if($x-ge 0-and $x-lt $b.Width-and $y-ge 0-and $y-lt $b.Height){$b.SetPixel($x,$y,$c)} }
function Rect([System.Drawing.Bitmap]$b,[int]$x,[int]$y,[int]$w,[int]$h,$c){ for($j=$y;$j-lt$y+$h;$j++){for($i=$x;$i-lt$x+$w;$i++){Px $b $i $j $c}} }
function Bord([System.Drawing.Bitmap]$b,[int]$x,[int]$y,[int]$w,[int]$h,$c,[int]$t=1){ for($n=0;$n-lt$t;$n++){ for($i=$x+$n;$i-lt$x+$w-$n;$i++){Px $b $i ($y+$n) $c;Px $b $i ($y+$h-1-$n) $c}; for($j=$y+$n;$j-lt$y+$h-$n;$j++){Px $b ($x+$n) $j $c;Px $b ($x+$w-1-$n) $j $c} } }

# Draw one wall tile at offset (x0,y0) in bitmap $b.
# gT/gB/gL/gR = side faces a corridor (gets neon glow)
function WallTile([System.Drawing.Bitmap]$b,[int]$x0,[int]$y0,[bool]$gT,[bool]$gB,[bool]$gL,[bool]$gR){
    # 1. dark interior fill
    Rect $b $x0 $y0 16 16 $WallFill
    # 2. all four edges = muted wall-connect color
    for($i=0;$i-lt 16;$i++){
        Px $b ($x0+$i) $y0         $WallEdge
        Px $b ($x0+$i) ($y0+15)    $WallEdge
        Px $b $x0 ($y0+$i)         $WallEdge
        Px $b ($x0+15) ($y0+$i)    $WallEdge
    }
    # 3. corridor-facing edges → neon glow (3-pixel gradient)
    if($gT){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) $y0       $NeonBrt; Px $b ($x0+$i) ($y0+1) $NeonMid; Px $b ($x0+$i) ($y0+2) $NeonDim } }
    if($gB){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) ($y0+15)  $NeonBrt; Px $b ($x0+$i) ($y0+14) $NeonMid; Px $b ($x0+$i) ($y0+13) $NeonDim } }
    if($gL){ for($j=0;$j-lt 16;$j++){ Px $b $x0 ($y0+$j)       $NeonBrt; Px $b ($x0+1) ($y0+$j) $NeonMid; Px $b ($x0+2) ($y0+$j) $NeonDim } }
    if($gR){ for($j=0;$j-lt 16;$j++){ Px $b ($x0+15) ($y0+$j)  $NeonBrt; Px $b ($x0+14) ($y0+$j) $NeonMid; Px $b ($x0+13) ($y0+$j) $NeonDim } }
}

# ════════════════════════════════════════════════════════════════════
# 1. TILESHEET  (16 × 224 — 14 tiles stacked, neon corridor glow)
# Tile index → gTop, gBot, gLeft, gRight (which sides face corridors)
# ════════════════════════════════════════════════════════════════════
Write-Host "[1] Tilesheet"
$S = NewBmp 16 224

# WALL_H(0):   left+right=wall, top+bot=corridor
WallTile $S 0   0  $true  $true  $false $false
# WALL_V(1):   top+bot=wall,   left+right=corridor
WallTile $S 0  16  $false $false $true  $true
# CORNER_TL(2): down+right=wall, top+left=corridor
WallTile $S 0  32  $true  $false $true  $false
# CORNER_TR(3): down+left=wall,  top+right=corridor
WallTile $S 0  48  $true  $false $false $true
# CORNER_BL(4): up+right=wall,   bot+left=corridor
WallTile $S 0  64  $false $true  $true  $false
# CORNER_BR(5): up+left=wall,    bot+right=corridor
WallTile $S 0  80  $false $true  $false $true
# T_TOP(6):    left+right+down=wall, top=corridor
WallTile $S 0  96  $true  $false $false $false
# T_BOT(7):    left+right+up=wall,   bot=corridor
WallTile $S 0 112  $false $true  $false $false
# T_LEFT(8):   up+down+right=wall,   left=corridor
WallTile $S 0 128  $false $false $true  $false
# T_RIGHT(9):  up+down+left=wall,    right=corridor
WallTile $S 0 144  $false $false $false $true
# CROSS(10):   all sides=wall (no corridor glow at all)
WallTile $S 0 160  $false $false $false $false

# FLOOR_PLAIN (11) — subtle dark grid
$y0=176; Rect $S 0 $y0 16 16 $DeepBg
for($i=0;$i-lt 16;$i+=4){ for($j=0;$j-lt 16;$j+=4){ Px $S $i ($y0+$j) $dimV } }

# FLOOR_FEED (12) — horizontal scanlines suggesting content stream
$y0=192; Rect $S 0 $y0 16 16 $DeepBg
for($py=2;$py-lt 16;$py+=3){ for($px=0;$px-lt 16;$px++){ Px $S $px ($y0+$py) (C 0x22 0x18 0x40) } }
Px $S 3 ($y0+2) $dimV; Px $S 7 ($y0+5) $dimV; Px $S 11 ($y0+8) $dimV; Px $S 5 ($y0+11) $dimV

# MALUS (13) — red warning tile with exclamation
$y0=208; Rect $S 0 $y0 16 16 $DeepBg
Bord $S 0 $y0 16 16 $Red 1
Bord $S 2 ($y0+2) 12 12 (C 0x60 0x08 0x18) 1
for($py=4;$py-le 9;$py++){ Px $S 8 ($y0+$py) $Red }
Px $S 8 ($y0+11) $Red

Save $S "$outDir\Tilesheet\DoomScrolling_Tiles.png"; $S.Dispose()

# ════════════════════════════════════════════════════════════════════
# 2. PLAYER SPRITE — phone overlay on every frame (unchanged logic)
# ════════════════════════════════════════════════════════════════════
Write-Host "[2] Player sprite"
$srcPath = "C:\Users\Utilisateur\Downloads\User-walk-v2.png"
if(Test-Path $srcPath){
    $src  = [System.Drawing.Bitmap]::new($srcPath)
    $fw   = [int]($src.Width/5)
    $fh   = [int]($src.Height/5)
    $dst  = NewBmp $src.Width $src.Height
    $g2   = [System.Drawing.Graphics]::FromImage($dst)
    $g2.DrawImage($src,0,0,$src.Width,$src.Height)
    $g2.Dispose(); $src.Dispose()

    $pW=22; $pH=36
    $pxOff=@(147,151,155,151,147)
    $pyBase=122

    for($row=0;$row-lt 5;$row++){
        for($col=0;$col-lt 5;$col++){
            $ox=$col*$fw+$pxOff[$col]; $oy=$row*$fh+$pyBase
            # Cyan glow halo
            for($gy=$oy-2;$gy-lt$oy+$pH+2;$gy++){
                for($gx=$ox-2;$gx-lt$ox+$pW+2;$gx++){
                    $inP=($gx-ge $ox-and $gx-lt $ox+$pW-and $gy-ge $oy-and $gy-lt $oy+$pH)
                    if(-not $inP){
                        if($gx-ge 0-and $gx-lt $dst.Width-and $gy-ge 0-and $gy-lt $dst.Height){
                            $ex=$dst.GetPixel($gx,$gy)
                            if($ex.A-gt 20){
                                $nr=[int]($ex.R*0.4)
                                $ng=[Math]::Min(255,[int]($ex.G*0.4+110))
                                $nb=[Math]::Min(255,[int]($ex.B*0.4+130))
                                $dst.SetPixel($gx,$gy,[System.Drawing.Color]::FromArgb($ex.A,$nr,$ng,$nb))
                            }
                        }
                    }
                }
            }
            # Phone body
            Rect $dst $ox $oy $pW $pH $Ghost
            Rect $dst ($ox+2) ($oy+2) ($pW-4) ($pH-6) $Blue
            Rect $dst ($ox+5) ($oy+2) 10 2 $Navy
            Px $dst ($ox+$pW-4) ($oy+3) $Navy
            Rect $dst ($ox+8) ($oy+$pH-4) 5 3 $Ghost
        }
    }
    Save $dst "$outDir\Character\Player_Spritesheet.png"; $dst.Dispose()
} else {
    Write-Warning "User-walk-v2.png not found"
}

# ════════════════════════════════════════════════════════════════════
# 3. LIKE CREATURE  (32×80 — 2 frames × 5 states, larger + neon)
# ════════════════════════════════════════════════════════════════════
Write-Host "[3] Like Creature"
$LikeBmp = NewBmp 32 80

$heartMap = @(
    "0001100110000000",
    "0011111111000000",
    "0111111111100000",
    "0111111111100000",
    "0111111111100000",
    "0011111111000000",
    "0001111110000000",
    "0000111100000000",
    "0000011000000000",
    "0000000000000000"
)

$tier2C = C 0xFF 0x28 0x60
$tier3C = C 0xFF 0x00 0x40
$cloneC = C 0x40 0x05 0x80

$bodyColorArr = @($Pink,$Pink,$tier2C,$tier2C,$tier3C,$tier3C,$Purple,$Purple,$cloneC,$cloneC)
$eyeColorArr  = @($Ghost,$Ghost,$Ghost,$Ghost,$Ghost,$Ghost,$BgBlk,$BgBlk,$Pink,$Pink)
$glitchArr    = @($false,$false,$false,$false,$true,$true,$false,$false,$false,$false)

for($si=0;$si-lt 10;$si++){
    $sRow=[int]($si/2); $sCol=$si%2
    $xOff2=$sCol*16; $yOff2=$sRow*16
    $bodyCol2=$bodyColorArr[$si]; $eyeCol2=$eyeColorArr[$si]

    Rect $LikeBmp $xOff2 $yOff2 16 16 $Clear

    # Draw heart outline glow (1px lighter ring)
    for($hr=0;$hr-lt 10;$hr++){
        for($hc=0;$hc-lt 16;$hc++){
            if($heartMap[$hr][$hc] -eq '1'){
                Px $LikeBmp ($xOff2+$hc) ($yOff2+$hr+2) $bodyCol2
                # 1px glow on neighbors if they're empty
                foreach($dx in -1,0,1){ foreach($dy in -1,0,1){
                    if($hc+$dx-ge 0-and $hc+$dx-lt 16-and $hr+$dy-ge 0-and $hr+$dy-lt 10){
                        $clampHr=[Math]::Max(0,[Math]::Min(9,$hr+$dy)); $clampHc=[Math]::Max(0,[Math]::Min(15,$hc+$dx))
                        if($clampHr-lt $heartMap.Length -and $heartMap[$clampHr][$clampHc] -ne '1'){
                            $gx2=$xOff2+$hc+$dx; $gy2=$yOff2+$hr+$dy+2
                            if($gx2-ge $xOff2-and $gx2-lt $xOff2+16-and $gy2-ge $yOff2-and $gy2-lt $yOff2+16){
                                $cur=$LikeBmp.GetPixel($gx2,$gy2)
                                if($cur.A -lt 50){
                                    $glowA=[int]80
                                    $LikeBmp.SetPixel($gx2,$gy2,[System.Drawing.Color]::FromArgb($glowA,$bodyCol2.R,$bodyCol2.G,$bodyCol2.B))
                                }
                            }
                        }
                    }
                }}
            }
        }
    }

    Rect $LikeBmp ($xOff2+4) ($yOff2+6) 2 2 $eyeCol2
    Rect $LikeBmp ($xOff2+9) ($yOff2+6) 2 2 $eyeCol2
    $legOff2=$sCol
    Px $LikeBmp ($xOff2+5)  ($yOff2+13+$legOff2)     $bodyCol2
    Px $LikeBmp ($xOff2+10) ($yOff2+13+(1-$legOff2)) $bodyCol2
    if($glitchArr[$si]){
        $rngG=[System.Random]::new(42)
        for($gi=0;$gi-lt 5;$gi++){ Px $LikeBmp ($xOff2+$rngG.Next(0,16)) ($yOff2+$rngG.Next(0,16)) $Pink }
    }
}

Save $LikeBmp "$outDir\Enemies\LikeCreature_Spritesheet.png"; $LikeBmp.Dispose()

# ════════════════════════════════════════════════════════════════════
# 4. COLLECTIBLES  (neon-bordered, polished)
# ════════════════════════════════════════════════════════════════════
Write-Host "[4] Collectibles"

# NotifDot (8×8) — glowing pink sphere
$D=NewBmp 8 8; Rect $D 0 0 8 8 $Clear
Rect $D 1 1 6 6 (C 0x80 0x15 0x38 80)   # outer glow ring
Rect $D 2 2 4 4 $Pink
Px $D 3 2 $DotGlow; Px $D 4 2 $DotGlow
Px $D 2 3 $DotGlow; Px $D 3 3 $Ghost
Save $D "$outDir\Collectibles\NotifDot.png"; $D.Dispose()

# Snapchat icon — neon yellow ghost on dark bg
$SN=NewBmp 16 16; Rect $SN 0 0 16 16 (C 0x1A 0x1A 0x00)
Bord $SN 0 0 16 16 (C 0xFF 0xFC 0x00) 1
$yellow=C 0xFF 0xFC 0x00
Rect $SN 4 3 8 7 $yellow; Px $SN 4 3 $Clear; Px $SN 11 3 $Clear
Px $SN 3 7 $yellow; Px $SN 12 7 $yellow
Rect $SN 4 9 8 3 $yellow
Px $SN 5 12 $yellow; Px $SN 6 12 $yellow; Px $SN 9 12 $yellow; Px $SN 10 12 $yellow
Save $SN "$outDir\Collectibles\Snapchat_Icon.png"; $SN.Dispose()

# Instagram — purple→pink gradient with neon border
$IG=NewBmp 16 16
for($y=0;$y-lt 16;$y++){
    for($x=0;$x-lt 16;$x++){
        $t2=($x+$y)/30.0
        $r2=[Math]::Min(255,[int](0x81*(1-$t2)+0xFF*$t2))
        $g2=[Math]::Min(255,[int](0x15*(1-$t2)+0x4D*$t2))
        $b2=[Math]::Min(255,[int](0xFF*(1-$t2)+0x90*$t2))
        $IG.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$r2,$g2,$b2))
    }
}
Bord $IG 0 0 16 16 $Ghost 1
Bord $IG 3 4 10 8 $Ghost 1; Rect $IG 7 6 3 3 $Ghost
Px $IG 12 4 $Ghost; Px $IG 12 5 $Ghost
Save $IG "$outDir\Collectibles\Instagram_Icon.png"; $IG.Dispose()

# TikTok — black bg, white note shape, red/blue accents
$TT=NewBmp 16 16; Rect $TT 0 0 16 16 (C 0x05 0x05 0x05)
Bord $TT 0 0 16 16 $Ghost 1
Rect $TT 5 4 4 6 $Ghost; Rect $TT 8 2 2 4 $Ghost; Rect $TT 8 2 4 2 $Ghost
for($y=6;$y-lt 10;$y++){ Px $TT 4 $y $Blue; Px $TT 9 $y $Red }
Px $TT 6 10 $Ghost; Px $TT 7 10 $Ghost
Save $TT "$outDir\Collectibles\TikTok_Icon.png"; $TT.Dispose()

# Twitter/X — cyan X on dark bg
$TW=NewBmp 16 16; Rect $TW 0 0 16 16 (C 0x05 0x10 0x1A)
Bord $TW 0 0 16 16 $Blue 1
for($i=0;$i-lt 10;$i++){ Px $TW (3+$i) (3+$i) $Blue; Px $TW (3+$i) (12-$i) $Blue }
Px $TW 3 3 $Ghost; Px $TW 12 3 $Ghost; Px $TW 3 12 $Ghost; Px $TW 12 12 $Ghost
Save $TW "$outDir\Collectibles\Twitter_Icon.png"; $TW.Dispose()

# BonusPoints — sparkling stars in neon cyan
$BP=NewBmp 16 16; Rect $BP 0 0 16 16 $DeepBg
Bord $BP 0 0 16 16 $Blue 1
$pcs=@(4,8,12)
foreach($pc in $pcs){
    Px $BP $pc     $pc     $Ghost
    Px $BP ($pc-1) $pc     $Blue
    Px $BP ($pc+1) $pc     $Blue
    Px $BP $pc     ($pc-1) $Blue
    Px $BP $pc     ($pc+1) $Blue
}
Save $BP "$outDir\Collectibles\BonusPoints.png"; $BP.Dispose()

# Full Phone — neon cyan phone with glowing screen
$FP=NewBmp 16 16; Rect $FP 0 0 16 16 $DeepBg
Bord $FP 0 0 16 16 (C 0x00 0x80 0x88) 1   # outer dim glow
Bord $FP 3 1 10 14 $Ghost 1                # phone body
Rect $FP 4 2 8 11 $Blue                    # screen
Rect $FP 4 2 8 2 (C 0x00 0xCC 0xDD) 1     # brighter top of screen
Rect $FP 7 5 2 4 $Pink; Px $FP 6 5 $Pink; Px $FP 9 5 $Pink   # call icon
Px $FP 7 14 $Ghost; Px $FP 8 14 $Ghost    # home button
Save $FP "$outDir\Collectibles\FullPhone.png"; $FP.Dispose()

# ════════════════════════════════════════════════════════════════════
# 5. HAZARDS  (neon-bordered, more legible)
# ════════════════════════════════════════════════════════════════════
Write-Host "[5] Hazards"

# PopupAd — red warning dialog with X button
$PA=NewBmp 16 32; Rect $PA 0 0 16 32 (C 0x1A 0x0A 0x12)
Bord $PA 0 0 16 32 $Red 1
Rect $PA 1 1 14 7 $AdRed             # header bar
Px $PA 13 2 $Ghost; Px $PA 14 2 $Ghost    # X button
Px $PA 13 3 $Ghost; Px $PA 14 3 $Ghost
for($ly=11;$ly-lt 28;$ly+=4){ Rect $PA 2 $ly 12 2 (C 0x30 0x18 0x22) }
Rect $PA 3 27 10 3 (C 0xFF 0x60 0x20)    # CTA button orange
Save $PA "$outDir\Hazards\PopupAd_Sprite.png"; $PA.Dispose()

# AutoPlay — play arrow in violet neon
$AP=NewBmp 16 16; Rect $AP 0 0 16 16 $DeepBg
Bord $AP 0 0 16 16 $Violet 1
for($i=0;$i-lt 7;$i++){
    $w2=7-$i; $xi=4+$i
    Rect $AP $xi (4+$i) $w2 1 $Violet
    Rect $AP $xi (11-$i) $w2 1 $Violet
}
Rect $AP 4 4 1 8 $Ghost
Save $AP "$outDir\Hazards\AutoPlay_Sprite.png"; $AP.Dispose()

# TrendingTrap — flame/chart shape in hot pink
$TR=NewBmp 16 16; Rect $TR 0 0 16 16 $DeepBg
Bord $TR 0 0 16 16 $Pink 1
$fhArr=@(2,4,6,6,5,4,4,3,2,1)
for($i=0;$i-lt 10;$i++){
    $fw2=$fhArr[$i]; Rect $TR (8-[int]($fw2/2)) (12-$i) $fw2 1 $Pink
}
Px $TR 7 4 $Blue; Px $TR 8 4 $Blue; Px $TR 8 3 $Blue; Px $TR 9 5 $Blue
Save $TR "$outDir\Hazards\TrendingTrap_Sprite.png"; $TR.Dispose()

# ════════════════════════════════════════════════════════════════════
# 6. CYBERPUNK CITY BACKGROUND  (304×336 — full maze size)
#    Deep purple night sky, building silhouettes, neon signs, stars
# ════════════════════════════════════════════════════════════════════
Write-Host "[6] Cyberpunk background"
$BG=NewBmp 304 336

# Sky gradient (top=darkest, bottom=slightly lighter)
for($y=0;$y-lt 336;$y++){
    $t=$y/335.0
    $r=[int](0x08*(1-$t)+0x16*$t)
    $g=[int](0x04*(1-$t)+0x0A*$t)
    $b=[int](0x14*(1-$t)+0x28*$t)
    for($x=0;$x-lt 304;$x++){
        $BG.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$r,$g,$b))
    }
}

# Stars (scattered in top 200 rows)
$rng=[System.Random]::new(7)
for($s=0;$s-lt 120;$s++){
    $sx=$rng.Next(0,304); $sy=$rng.Next(0,180)
    $sa=[int](100+$rng.Next(0,155))
    $sz=$rng.Next(0,3)
    $BG.SetPixel($sx,$sy,[System.Drawing.Color]::FromArgb($sa,0xE0,0xCC,0xFF))
    if($sz-gt 0){ Px $BG ($sx+1) $sy (C 0xE0 0xCC 0xFF 60); Px $BG $sx ($sy+1) (C 0xE0 0xCC 0xFF 60) }
}

# Building silhouettes — 7 buildings
$buildings = @(
    @{x=0;   w=48; h=200; clr=$BuildDk},
    @{x=44;  w=36; h=160; clr=$BuildMid},
    @{x=75;  w=60; h=240; clr=$BuildDk},
    @{x=130; w=44; h=180; clr=$BuildMid},
    @{x=168; w=52; h=220; clr=$BuildDk},
    @{x=215; w=40; h=150; clr=$BuildMid},
    @{x=250; w=54; h=200; clr=$BuildDk}
)
foreach($bld in $buildings){
    Rect $BG $bld.x (336-$bld.h) $bld.w $bld.h $bld.clr
    # Antenna
    $midX=$bld.x+[int]($bld.w/2)
    for($ay=(336-$bld.h-8);$ay-lt(336-$bld.h);$ay++){ Px $BG $midX $ay (C 0x28 0x18 0x40) }
    Px $BG $midX (336-$bld.h-8) $Red   # red blink
}

# Windows on buildings (tiny neon rectangles)
$winColors=@($WinCyan,$WinPink,$WinViol,$WinYel,(C 0xFF 0xFF 0xFF 180))
foreach($bld in $buildings){
    for($wy=0;$wy-lt 15;$wy++){
        for($wx=0;$wx-lt [int]($bld.w/8);$wx++){
            if($rng.Next(0,3)-gt 0){
                $wpx=$bld.x+2+$wx*8+$rng.Next(0,3)
                $wpy=336-$bld.h+4+$wy*12+$rng.Next(0,4)
                if($wpy-gt 0-and $wpy+3-lt 336-and $wpx+4-lt 304){
                    $wc=$winColors[$rng.Next(0,$winColors.Length)]
                    if($rng.Next(0,5)-gt 0){   # lit window
                        Rect $BG $wpx $wpy 4 3 $wc
                    } else {                    # dark window
                        Rect $BG $wpx $wpy 4 3 (C 0x18 0x10 0x28)
                    }
                }
            }
        }
    }
}

# Neon signs (large colored rectangles with text pixels)
$signs=@(
    @{x=10; y=140; w=40; h=14; c=$Blue; t="BTC"},
    @{x=78; y=100; w=36; h=12; c=$Purple; t="AE"},
    @{x=138;y=130; w=32; h=10; c=$Pink; t="NET"},
    @{x=220;y=118; w=38; h=12; c=$Violet; t="SYS"}
)
foreach($sg in $signs){
    # sign frame
    Bord $BG $sg.x $sg.y $sg.w $sg.h $sg.c 1
    Rect $BG ($sg.x+1) ($sg.y+1) ($sg.w-2) ($sg.h-2) (C $sg.c.R $sg.c.G $sg.c.B 40)
    # glow dots scattered near sign
    for($gi=0;$gi-lt 6;$gi++){
        Px $BG ($sg.x+$rng.Next(0,$sg.w)) ($sg.y+$rng.Next(0,$sg.h)) $sg.c
    }
}

# Distant glow on horizon (atmospheric)
for($x=0;$x-lt 304;$x++){
    $glo=[int](20*[Math]::Sin([Math]::PI*$x/304.0))
    $hy=336-220
    for($gy=0;$gy-lt 4;$gy++){
        $ga=[int](($glo/20.0)*(1-$gy/4.0)*40)
        if($ga-gt 0){ Px $BG $x ($hy+$gy) (C 0x60 0x20 0x88 $ga) }
    }
}

Save $BG "$outDir\Background\CyberpunkCity.png"; $BG.Dispose()

# ════════════════════════════════════════════════════════════════════
# 7. MAZE FRAME  (9-slice border sprite, 32×32 with 8px border)
#    Neon cyan outer glow → violet inner
# ════════════════════════════════════════════════════════════════════
Write-Host "[7] Maze frame"
$FR=NewBmp 32 32; Rect $FR 0 0 32 32 $Clear

# Outer glow: 1px dim cyan
Bord $FR 0 0 32 32 (C 0x00 0x88 0x99 120) 1
# Main border: 2px bright cyan
Bord $FR 1 1 30 30 $NeonBrt 1
Bord $FR 2 2 28 28 $NeonMid 1
# Inner violet
Bord $FR 3 3 26 26 $Violet 1
Bord $FR 4 4 24 24 (C 0x40 0x30 0x80) 1
# Corner decorations — small pixel ornaments
foreach($cx in @(0,25)){ foreach($cy in @(0,25)){
    Px $FR ($cx+3) ($cy+3) $Ghost
    Px $FR ($cx+4) ($cy+3) $Ghost
    Px $FR ($cx+3) ($cy+4) $Ghost
}}

Save $FR "$outDir\UI\MazeFrame.png"; $FR.Dispose()

# ════════════════════════════════════════════════════════════════════
# 8. HUD ELEMENTS  — heart (life), pixel bar, notification panel
# ════════════════════════════════════════════════════════════════════
Write-Host "[8] HUD elements"

# Heart icon for lives (8×8)
$HT=NewBmp 8 8; Rect $HT 0 0 8 8 $Clear
$heartRows8=@("011011","111111","111111","011110","001100","000000")
for($hy=0;$hy-lt 6;$hy++){
    for($hx=0;$hx-lt 6;$hx++){
        if($hy-lt $heartRows8.Length-and $hx-lt $heartRows8[$hy].Length-and $heartRows8[$hy][$hx]-eq '1'){
            Px $HT ($hx+1) ($hy+1) $Pink
            # glow
            if($hy-eq 0-or $hy-eq 5-or $hx-eq 0-or $hx-eq 5){
                Px $HT $hx ($hy+1) (C 0xFF 0x4D 0x90 80)
            }
        }
    }
}
Px $HT 2 2 (C 0xFF 0xAA 0xCC)  # highlight
Save $HT "$outDir\UI\HeartIcon.png"; $HT.Dispose()

# Notification panel (200×64) — dark glass with neon border
$NP=NewBmp 200 64; Rect $NP 0 0 200 64 (C 0x0D 0x08 0x1E 230)
Bord $NP 0 0 200 64 $Blue 1
Bord $NP 1 1 198 62 (C 0x00 0x60 0x80) 1
# Icon area
Rect $NP 4 4 24 24 (C 0x18 0x0D 0x35)
Bord $NP 4 4 24 24 $Violet 1
# Text lines
Rect $NP 34 10 130 5 (C 0xF7 0xD8 0xFF 200)
Rect $NP 34 22 90 4 (C 0x78 0x6C 0xF6 160)
# Ping dot
Rect $NP 4 32 24 8 (C 0x12 0x0F 0x1E)
Px $NP 15 36 $Pink; Px $NP 16 36 $Pink; Px $NP 15 37 $Pink; Px $NP 16 37 $Pink
Save $NP "$outDir\UI\NotificationPanel.png"; $NP.Dispose()

# HUD_Elements placeholder (4×4)
$HU=NewBmp 4 4; Rect $HU 0 0 4 4 $Ghost
Save $HU "$outDir\UI\HUD_Elements.png"; $HU.Dispose()

Write-Host "`n=== DONE ===" -ForegroundColor Cyan
