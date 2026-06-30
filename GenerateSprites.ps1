Add-Type -AssemblyName System.Drawing

$Pink   = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x4D, 0x90)
$Purple = [System.Drawing.Color]::FromArgb(255, 0x81, 0x15, 0xFF)
$Violet = [System.Drawing.Color]::FromArgb(255, 0x78, 0x6C, 0xF6)
$BgBlk  = [System.Drawing.Color]::FromArgb(255, 0x12, 0x0F, 0x1E)
$Ghost  = [System.Drawing.Color]::FromArgb(255, 0xF7, 0xD8, 0xFF)
$Blue   = [System.Drawing.Color]::FromArgb(255, 0x00, 0xF5, 0xFF)
$Red    = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x3A, 0x5E)
$Clear  = [System.Drawing.Color]::Transparent
$Navy   = [System.Drawing.Color]::FromArgb(255, 0x1A, 0x10, 0x40)
$dimV   = [System.Drawing.Color]::FromArgb(255, 0x2A, 0x26, 0x5A)
$tier2C = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x28, 0x60)
$tier3C = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x00, 0x40)
$cloneC = [System.Drawing.Color]::FromArgb(255, 0x40, 0x05, 0x80)
$orange = [System.Drawing.Color]::FromArgb(255, 0xFF, 0x8C, 0x42)
$gray   = [System.Drawing.Color]::FromArgb(255, 0x44, 0x44, 0x50)
$panBg  = [System.Drawing.Color]::FromArgb(255, 0x1C, 0x1C, 0x1E)
$yellow = [System.Drawing.Color]::FromArgb(255, 0xFF, 0xFC, 0x00)

$outDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\_Sprites"

function MkDir2([string]$p) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null } }
MkDir2 "$outDir\Tilesheet"; MkDir2 "$outDir\Character"; MkDir2 "$outDir\Enemies"
MkDir2 "$outDir\Collectibles"; MkDir2 "$outDir\Hazards"; MkDir2 "$outDir\UI"

function NewBmp([int]$w,[int]$h) { return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function SaveBmp { param($bmp,[string]$p) $bmp.Save($p,[System.Drawing.Imaging.ImageFormat]::Png); Write-Host "  >> $([IO.Path]::GetFileName($p))" }
function Px { param($b,[int]$x,[int]$y,$c) if ($x -ge 0 -and $x -lt $b.Width -and $y -ge 0 -and $y -lt $b.Height) { $b.SetPixel($x,$y,$c) } }
function Rect { param($b,[int]$x,[int]$y,[int]$w,[int]$h,$c) for ($j=$y;$j -lt $y+$h;$j++) { for ($i=$x;$i -lt $x+$w;$i++) { Px $b $i $j $c } } }
function Bord { param($b,[int]$x,[int]$y,[int]$w,[int]$h,$c,[int]$t=1) for ($n=0;$n -lt $t;$n++) { for ($i=$x+$n;$i -lt $x+$w-$n;$i++) { Px $b $i ($y+$n) $c; Px $b $i ($y+$h-1-$n) $c }; for ($j=$y+$n;$j -lt $y+$h-$n;$j++) { Px $b ($x+$n) $j $c; Px $b ($x+$w-1-$n) $j $c } } }

# ════════════════════════════════════════════════════
# 1. TILESHEET  (16 × 224 — 14 tiles stacked)
# ════════════════════════════════════════════════════
Write-Host "[1] Tilesheet"
$S = NewBmp 16 224

for ($t=0; $t -lt 11; $t++) {
    $y0 = $t * 16
    Rect $S 0 $y0 16 16 $Purple
    Bord $S 0 $y0 16 16 $Violet 1
}
# WALL_H(0): open top+bottom
$t=0; $y0=$t*16; for ($px=1;$px-lt 15;$px++) { Px $S $px $y0 $BgBlk; Px $S $px ($y0+15) $BgBlk }
# WALL_V(1): open left+right
$t=1; $y0=$t*16; for ($py=1;$py -lt 15;$py++) { Px $S 0 ($y0+$py) $BgBlk; Px $S 15 ($y0+$py) $BgBlk }
# CORNER_TL(2): open top+left
$t=2; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px $y0 $BgBlk}; for ($py=1;$py -lt 15;$py++){Px $S 0 ($y0+$py) $BgBlk}
# CORNER_TR(3): open top+right
$t=3; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px $y0 $BgBlk}; for ($py=1;$py -lt 15;$py++){Px $S 15 ($y0+$py) $BgBlk}
# CORNER_BL(4): open bottom+left
$t=4; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px ($y0+15) $BgBlk}; for ($py=1;$py -lt 15;$py++){Px $S 0 ($y0+$py) $BgBlk}
# CORNER_BR(5): open bottom+right
$t=5; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px ($y0+15) $BgBlk}; for ($py=1;$py -lt 15;$py++){Px $S 15 ($y0+$py) $BgBlk}
# T_TOP(6): open top
$t=6; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px $y0 $BgBlk}
# T_BOT(7): open bottom
$t=7; $y0=$t*16; for ($px=1;$px -lt 15;$px++){Px $S $px ($y0+15) $BgBlk}
# T_LEFT(8): open left
$t=8; $y0=$t*16; for ($py=1;$py -lt 15;$py++){Px $S 0 ($y0+$py) $BgBlk}
# T_RIGHT(9): open right
$t=9; $y0=$t*16; for ($py=1;$py -lt 15;$py++){Px $S 15 ($y0+$py) $BgBlk}
# CROSS(10): all solid — already drawn

# FLOOR_PLAIN (11)
$y0=11*16; Rect $S 0 $y0 16 16 $BgBlk
foreach ($cx in @(1,13)) { foreach ($cy in @(1,13)) { Px $S $cx ($y0+$cy) $Violet; Px $S ($cx+1) ($y0+$cy) $Violet; Px $S $cx ($y0+$cy+1) $Violet; Px $S ($cx+1) ($y0+$cy+1) $Violet } }

# FLOOR_FEED (12)
$y0=12*16; Rect $S 0 $y0 16 16 $BgBlk
for ($px=0;$px -lt 16;$px++) { Px $S $px ($y0+4) $dimV; Px $S $px ($y0+8) $dimV; Px $S $px ($y0+12) $dimV }

# MALUS (13)
$y0=13*16; Rect $S 0 $y0 16 16 $BgBlk; Bord $S 0 $y0 16 16 $Red 1
Px $S 8 ($y0+3) $Red
Px $S 7 ($y0+4) $Red; Px $S 9 ($y0+4) $Red
Px $S 6 ($y0+5) $Red; Px $S 10 ($y0+5) $Red
Px $S 5 ($y0+6) $Red; Px $S 11 ($y0+6) $Red
for ($px=5;$px -le 11;$px++) { Px $S $px ($y0+7) $Red }
Px $S 8 ($y0+5) $BgBlk; Px $S 8 ($y0+6) $BgBlk; Px $S 8 ($y0+9) $Red

SaveBmp $S "$outDir\Tilesheet\DoomScrolling_Tiles.png"; $S.Dispose()

# ════════════════════════════════════════════════════
# 2. PLAYER SPRITE — phone overlay on every frame
# ════════════════════════════════════════════════════
Write-Host "[2] Player sprite"
$srcPath = "C:\Users\Utilisateur\Downloads\User-walk-v2.png"
if (Test-Path $srcPath) {
    $src  = [System.Drawing.Bitmap]::new($srcPath)
    $fw   = [int]($src.Width  / 5)
    $fh   = [int]($src.Height / 5)
    $dst  = NewBmp $src.Width $src.Height
    $g2   = [System.Drawing.Graphics]::FromImage($dst)
    $g2.DrawImage($src, 0, 0, $src.Width, $src.Height)
    $g2.Dispose(); $src.Dispose()

    $pW   = 22; $pH = 36
    $pxOff = @(147,151,155,151,147)
    $pyBase = 122

    for ($row=0; $row -lt 5; $row++) {
        for ($col=0; $col -lt 5; $col++) {
            $ox = $col * $fw + $pxOff[$col]
            $oy = $row * $fh + $pyBase

            # Blue glow halo (2px ring)
            for ($gy = $oy-2; $gy -lt $oy+$pH+2; $gy++) {
                for ($gx = $ox-2; $gx -lt $ox+$pW+2; $gx++) {
                    $inP = ($gx -ge $ox -and $gx -lt $ox+$pW -and $gy -ge $oy -and $gy -lt $oy+$pH)
                    if (-not $inP) {
                        if ($gx -ge 0 -and $gx -lt $dst.Width -and $gy -ge 0 -and $gy -lt $dst.Height) {
                            $ex = $dst.GetPixel($gx,$gy)
                            if ($ex.A -gt 20) {
                                $nr = [int]($ex.R * 0.5)
                                $ng = [Math]::Min(255,[int]($ex.G*0.5 + 122))
                                $nb = [Math]::Min(255,[int]($ex.B*0.5 + 127))
                                $dst.SetPixel($gx,$gy,[System.Drawing.Color]::FromArgb($ex.A,$nr,$ng,$nb))
                            }
                        }
                    }
                }
            }
            # Phone body (Ghost White border)
            Rect $dst $ox $oy $pW $pH $Ghost
            # Screen (Hyper Blue, inset 2px, leaving 4px bottom for home btn)
            Rect $dst ($ox+2) ($oy+2) ($pW-4) ($pH-6) $Blue
            # Speaker slit at top
            Rect $dst ($ox+5) ($oy+2) 10 2 $Navy
            # Camera dot
            Px $dst ($ox+$pW-4) ($oy+3) $Navy
            # Home button
            Rect $dst ($ox+8) ($oy+$pH-4) 5 3 $Ghost
        }
    }
    SaveBmp $dst "$outDir\Character\Player_Spritesheet.png"
    $dst.Dispose()
} else {
    Write-Warning "User-walk-v2.png not found at $srcPath"
}

# ════════════════════════════════════════════════════
# 3. LIKE CREATURE  (32 × 80 — 2 frames × 5 states)
# ════════════════════════════════════════════════════
Write-Host "[3] Like Creature"
$LikeBmp = NewBmp 32 80   # NOTE: $EC collides with $eC (PS is case-insensitive)

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

$bodyColorArr = @($Pink,$Pink,$tier2C,$tier2C,$tier3C,$tier3C,$Purple,$Purple,$cloneC,$cloneC)
$eyeColorArr  = @($Ghost,$Ghost,$Ghost,$Ghost,$Ghost,$Ghost,$BgBlk,$BgBlk,$Pink,$Pink)
$glitchArr    = @($false,$false,$false,$false,$true,$true,$false,$false,$false,$false)

for ($si=0; $si -lt 10; $si++) {
    $sRow    = [int]($si / 2)
    $sCol    = $si % 2
    $xOff2   = $sCol * 16
    $yOff2   = $sRow * 16
    $bodyCol2= $bodyColorArr[$si]
    $eyeCol2 = $eyeColorArr[$si]   # $eyeCol2, not $eC, to avoid clobbering $LikeBmp

    Rect $LikeBmp $xOff2 $yOff2 16 16 $Clear
    for ($hr=0; $hr -lt 10; $hr++) {
        for ($hc=0; $hc -lt 16; $hc++) {
            if ($heartMap[$hr][$hc] -eq '1') { Px $LikeBmp ($xOff2+$hc) ($yOff2+$hr+2) $bodyCol2 }
        }
    }
    Rect $LikeBmp ($xOff2+4) ($yOff2+6) 2 2 $eyeCol2
    Rect $LikeBmp ($xOff2+9) ($yOff2+6) 2 2 $eyeCol2
    $legOff2 = $sCol
    Px $LikeBmp ($xOff2+5)  ($yOff2+13+$legOff2)     $bodyCol2
    Px $LikeBmp ($xOff2+10) ($yOff2+13+(1-$legOff2)) $bodyCol2
    if ($glitchArr[$si]) {
        $rngG = [System.Random]::new(42)
        for ($gi=0;$gi -lt 5;$gi++) { Px $LikeBmp ($xOff2+$rngG.Next(0,16)) ($yOff2+$rngG.Next(0,16)) $Pink }
    }
}

SaveBmp $LikeBmp "$outDir\Enemies\LikeCreature_Spritesheet.png"; $LikeBmp.Dispose()

# ════════════════════════════════════════════════════
# 4. COLLECTIBLES
# ════════════════════════════════════════════════════
Write-Host "[4] Collectibles"

# NotifDot (8×8)
$D = NewBmp 8 8; Rect $D 0 0 8 8 $Clear; Rect $D 1 1 6 6 $Pink
Px $D 3 3 $Ghost; Px $D 4 3 $Ghost; Px $D 3 4 $Ghost; Px $D 4 4 $Ghost
SaveBmp $D "$outDir\Collectibles\NotifDot.png"; $D.Dispose()

# Snapchat
$SN = NewBmp 16 16; Rect $SN 0 0 16 16 $yellow
Rect $SN 4 3 8 7 $Ghost; Px $SN 4 3 $yellow; Px $SN 11 3 $yellow
Px $SN 3 7 $Ghost; Px $SN 12 7 $Ghost
Rect $SN 4 9 8 3 $Ghost
Px $SN 5 12 $Ghost; Px $SN 6 12 $Ghost; Px $SN 9 12 $Ghost; Px $SN 10 12 $Ghost
SaveBmp $SN "$outDir\Collectibles\Snapchat_Icon.png"; $SN.Dispose()

# Instagram
$IG = NewBmp 16 16
for ($y=0;$y -lt 16;$y++) {
    for ($x=0;$x -lt 16;$x++) {
        $t2 = ($x+$y)/30.0
        $r2 = [Math]::Min(255,[int]($Purple.R*(1-$t2)+$Pink.R*$t2*0.5+$orange.R*$t2*0.5))
        $g2 = [Math]::Min(255,[int]($Purple.G*(1-$t2)+$Pink.G*$t2*0.5+$orange.G*$t2*0.5))
        $b2 = [Math]::Min(255,[int]($Purple.B*(1-$t2)+$Pink.B*$t2*0.5+$orange.B*$t2*0.5))
        if (($x+$y)%2 -eq 0) { $IG.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$r2,$g2,$b2)) }
        else                  { $IG.SetPixel($x,$y,$Purple) }
    }
}
Bord $IG 4 5 8 6 $Ghost 1; Rect $IG 7 7 3 3 $Ghost; Px $IG 11 5 $Ghost
SaveBmp $IG "$outDir\Collectibles\Instagram_Icon.png"; $IG.Dispose()

# TikTok
$TT = NewBmp 16 16; Rect $TT 0 0 16 16 $BgBlk
Rect $TT 5 5 4 5 $Ghost; Rect $TT 8 2 1 4 $Ghost; Rect $TT 8 2 3 1 $Ghost
for ($y=5;$y -lt 10;$y++) { Px $TT 4 $y $Blue; Px $TT 9 $y $Red }
SaveBmp $TT "$outDir\Collectibles\TikTok_Icon.png"; $TT.Dispose()

# Twitter/X
$TW = NewBmp 16 16; Rect $TW 0 0 16 16 $BgBlk
for ($i=0;$i -lt 10;$i++) { Px $TW (3+$i) (3+$i) $Ghost; Px $TW (3+$i) (12-$i) $Ghost }
SaveBmp $TW "$outDir\Collectibles\Twitter_Icon.png"; $TW.Dispose()

# BonusPoints
$BP = NewBmp 16 16; Rect $BP 0 0 16 16 $BgBlk
$pcs = @(3,7,11)
foreach ($pc in $pcs) {
    Px $BP $pc      $pc      $Blue
    Px $BP ($pc+1)  $pc      $Blue
    Px $BP ($pc-1)  $pc      $Blue
    Px $BP $pc      ($pc-1)  $Blue
    Px $BP $pc      ($pc+1)  $Blue
    Px $BP ($pc+1)  ($pc+1)  $Blue
}
SaveBmp $BP "$outDir\Collectibles\BonusPoints.png"; $BP.Dispose()

# Full Phone
$FP = NewBmp 16 16; Rect $FP 0 0 16 16 $BgBlk
Bord $FP 3 1 10 14 $Ghost 1; Rect $FP 4 2 8 11 $Blue
Rect $FP 7 5 2 4 $Pink; Px $FP 6 5 $Pink; Px $FP 9 5 $Pink
Px $FP 7 14 $Ghost; Px $FP 8 14 $Ghost
SaveBmp $FP "$outDir\Collectibles\FullPhone.png"; $FP.Dispose()

# ════════════════════════════════════════════════════
# 5. HAZARDS
# ════════════════════════════════════════════════════
Write-Host "[5] Hazards"

# PopupAd (16×32)
$PA = NewBmp 16 32; Rect $PA 0 0 16 32 $gray; Bord $PA 0 0 16 32 $Ghost 1
Rect $PA 1 1 14 6 $Red; Px $PA 13 2 $Ghost; Px $PA 12 3 $Ghost; Px $PA 13 3 $Ghost
for ($ly=10;$ly -lt 26;$ly+=4) { Rect $PA 2 $ly 12 2 $BgBlk }
SaveBmp $PA "$outDir\Hazards\PopupAd_Sprite.png"; $PA.Dispose()

# AutoPlay (16×16)
$AP = NewBmp 16 16; Rect $AP 0 0 16 16 $BgBlk; Bord $AP 0 0 16 16 $Violet 1
for ($i=0;$i -lt 8;$i++) { Rect $AP (4+$i) (4+$i) (8-$i) 1 $Violet; Rect $AP (4+$i) (11-$i) (8-$i) 1 $Violet }
SaveBmp $AP "$outDir\Hazards\AutoPlay_Sprite.png"; $AP.Dispose()

# TrendingTrap (16×16)
$TR = NewBmp 16 16; Rect $TR 0 0 16 16 $BgBlk
$fhArr = @(2,4,6,6,5,4,4,3,2,1)
for ($i=0;$i -lt 10;$i++) { $fw2=$fhArr[$i]; Rect $TR (8-[int]($fw2/2)) (13-$i) $fw2 1 $Pink }
Px $TR 7 5 $Blue; Px $TR 8 5 $Blue; Px $TR 8 4 $Blue
SaveBmp $TR "$outDir\Hazards\TrendingTrap_Sprite.png"; $TR.Dispose()

# ════════════════════════════════════════════════════
# 6. UI
# ════════════════════════════════════════════════════
Write-Host "[6] UI"
$NP = NewBmp 200 64; Rect $NP 0 0 200 64 $panBg; Bord $NP 0 0 200 64 $Purple 2
Rect $NP 4 4 24 24 $BgBlk; Bord $NP 4 4 24 24 $Violet 1
Rect $NP 34 8 130 6 $Ghost; Rect $NP 34 20 90 5 $Violet
SaveBmp $NP "$outDir\UI\NotificationPanel.png"; $NP.Dispose()

$HU = NewBmp 4 4; Rect $HU 0 0 4 4 $Ghost
SaveBmp $HU "$outDir\UI\HUD_Elements.png"; $HU.Dispose()

Write-Host "`n=== DONE ===" -ForegroundColor Green
