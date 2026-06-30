Add-Type -AssemblyName System.Drawing

# ═══════════════════════════════════════════════════════════════════
# PALETTE — exact values from game design document
# ═══════════════════════════════════════════════════════════════════
function C([int]$r,[int]$g,[int]$b,[int]$a=255){ [System.Drawing.Color]::FromArgb($a,$r,$g,$b) }

# Official palette
$Pink    = C 0xFF 0x4D 0x90   # #FF4D90 — Notification Pink
$Purple  = C 0x81 0x5F 0xFF   # #815FFF — Feed Purple  (was wrong: #8115FF)
$Violet  = C 0x78 0x6C 0xF6   # #786CF6 — Scroll Violet
$Black   = C 0x12 0x0F 0x1E   # #120F1E — Screen Black
$White   = C 0xF7 0xD8 0xFF   # #F7D8FF — Ghost White
$Blue    = C 0x00 0xF5 0xFF   # #00F5FF — Hyper Blue
$Red     = C 0xFF 0x3A 0x5E   # #FF3A5E — Alert Red
$Clear   = [System.Drawing.Color]::Transparent

# Derived
$PinkDim    = C 0xC0 0x20 0x55   # darker pink for glow gradient row 1
$PinkDeep   = C 0x60 0x08 0x28   # very dark pink row 2
$WallFill   = C 0x1C 0x0F 0x30   # dark purple wall interior
$FloorColor = C 0x12 0x0F 0x1E   # same as Black (clean dark corridors)
$FloorDot   = C 0x1E 0x14 0x2E   # slightly lighter for feed tile stripe
$VioletDim  = C 0x3A 0x28 0x70   # dim violet for UI borders
$PinkGhost  = C 0xFF 0x80 0xB0 120  # semi-transparent glow

# City background colors
$SkyDeep    = C 0x08 0x04 0x10
$SkyMid     = C 0x12 0x06 0x22
$SkyHori    = C 0x1E 0x08 0x30
$BuildDk    = C 0x0C 0x06 0x1A
$BuildMid   = C 0x16 0x0A 0x28
$WinP       = C 0xFF 0x4D 0x90   # pink window
$WinV       = C 0x81 0x5F 0xFF   # purple window
$WinB       = C 0x00 0xF5 0xFF   # blue window
$WinW       = C 0xF7 0xD8 0xFF 160  # white dim window

$outDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\_Sprites"

function MkD([string]$p){ if(-not(Test-Path $p)){New-Item -ItemType Directory -Force -Path $p|Out-Null} }
MkD "$outDir\Tilesheet"; MkD "$outDir\Character"; MkD "$outDir\Enemies"
MkD "$outDir\Collectibles"; MkD "$outDir\Hazards"; MkD "$outDir\UI"; MkD "$outDir\Background"

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

# ════════════════════════════════════════════════════════════════════
# WALL TILE — 16-bit neon PINK tube style
# Each tile: dark interior + 3px bright pink border on corridor-facing sides
# → walls look like glowing pink tubes (matching design doc)
# ════════════════════════════════════════════════════════════════════
function WallTile([System.Drawing.Bitmap]$b,[int]$x0,[int]$y0,[bool]$gT,[bool]$gB,[bool]$gL,[bool]$gR){
    # Base: dark wall fill
    Rect $b $x0 $y0 16 16 $WallFill

    # All edges start as dim violet (wall-to-wall join color)
    for($i=0;$i-lt 16;$i++){
        Px $b ($x0+$i) $y0      $VioletDim
        Px $b ($x0+$i) ($y0+15) $VioletDim
        Px $b $x0 ($y0+$i)      $VioletDim
        Px $b ($x0+15) ($y0+$i) $VioletDim
    }

    # Corridor-facing sides get 3-px PINK neon gradient (bright → dim)
    if($gT){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) $y0       $Pink; Px $b ($x0+$i) ($y0+1) $PinkDim; Px $b ($x0+$i) ($y0+2) $PinkDeep } }
    if($gB){ for($i=0;$i-lt 16;$i++){ Px $b ($x0+$i) ($y0+15)  $Pink; Px $b ($x0+$i) ($y0+14) $PinkDim; Px $b ($x0+$i) ($y0+13) $PinkDeep } }
    if($gL){ for($j=0;$j-lt 16;$j++){ Px $b $x0 ($y0+$j)       $Pink; Px $b ($x0+1) ($y0+$j) $PinkDim; Px $b ($x0+2) ($y0+$j) $PinkDeep } }
    if($gR){ for($j=0;$j-lt 16;$j++){ Px $b ($x0+15) ($y0+$j)  $Pink; Px $b ($x0+14) ($y0+$j) $PinkDim; Px $b ($x0+13) ($y0+$j) $PinkDeep } }
}

# ════════════════════════════════════════════════════════════════════
# 1. TILESHEET  (16×224 — 14 tiles)
# ════════════════════════════════════════════════════════════════════
Write-Host "[1] Tilesheet (16-bit pink tube walls)"
$S = NewBmp 16 224

WallTile $S 0   0  $true  $true  $false $false   # WALL_H:     top+bot = corridor
WallTile $S 0  16  $false $false $true  $true    # WALL_V:     left+right = corridor
WallTile $S 0  32  $true  $false $true  $false   # CORNER_TL:  top+left = corridor
WallTile $S 0  48  $true  $false $false $true    # CORNER_TR:  top+right = corridor
WallTile $S 0  64  $false $true  $true  $false   # CORNER_BL:  bot+left = corridor
WallTile $S 0  80  $false $true  $false $true    # CORNER_BR:  bot+right = corridor
WallTile $S 0  96  $true  $false $false $false   # T_TOP:      top = corridor
WallTile $S 0 112  $false $true  $false $false   # T_BOT:      bot = corridor
WallTile $S 0 128  $false $false $true  $false   # T_LEFT:     left = corridor
WallTile $S 0 144  $false $false $false $true    # T_RIGHT:    right = corridor
WallTile $S 0 160  $false $false $false $false   # CROSS:      no corridor sides

# FLOOR_PLAIN (11) — pure #120F1E, very clean 16-bit style
$y0=176; Rect $S 0 $y0 16 16 $FloorColor
# Tiny 1px corner dots (subtle grid marker, very dim)
Px $S 0 $y0 (C 0x1A 0x12 0x26); Px $S 15 $y0 (C 0x1A 0x12 0x26)
Px $S 0 ($y0+15) (C 0x1A 0x12 0x26); Px $S 15 ($y0+15) (C 0x1A 0x12 0x26)

# FLOOR_FEED (12) — horizontal bright stripe → "scrolling content" feel
$y0=192; Rect $S 0 $y0 16 16 $FloorColor
Rect $S 0 ($y0+7) 16 2 (C 0x22 0x14 0x3A)   # subtle mid stripe
Rect $S 2 ($y0+7) 12 1 (C 0x30 0x18 0x50)   # slightly lighter center

# MALUS (13) — red warning with ! mark
$y0=208; Rect $S 0 $y0 16 16 $FloorColor
Bord $S 0 $y0 16 16 $Red 1
Bord $S 2 ($y0+2) 12 12 (C 0x50 0x08 0x18) 1
for($py=4;$py-le 9;$py++){ Px $S 7 ($y0+$py) $Red; Px $S 8 ($y0+$py) $Red }
Px $S 7 ($y0+11) $Red; Px $S 8 ($y0+11) $Red

Save $S "$outDir\Tilesheet\DoomScrolling_Tiles.png"; $S.Dispose()

# ════════════════════════════════════════════════════════════════════
# 2. PLAYER SPRITE — preserve original walk cycle with phone overlay
# ════════════════════════════════════════════════════════════════════
Write-Host "[2] Player sprite"
$srcPath = "C:\Users\Utilisateur\Downloads\User-walk-v2.png"
if(Test-Path $srcPath){
    $src = [System.Drawing.Bitmap]::new($srcPath)
    $dst = NewBmp $src.Width $src.Height
    $g2  = [System.Drawing.Graphics]::FromImage($dst)
    $g2.DrawImage($src,0,0,$src.Width,$src.Height)
    $g2.Dispose(); $src.Dispose()

    $fw=256; $fh=256
    $pW=22; $pH=36
    $pxOff=@(147,151,155,151,147); $pyBase=122

    for($row=0;$row-lt 5;$row++){
        for($col=0;$col-lt 5;$col++){
            $ox=$col*$fw+$pxOff[$col]; $oy=$row*$fh+$pyBase
            # Pink glow halo (matches new palette)
            for($gy=$oy-2;$gy-lt$oy+$pH+2;$gy++){
                for($gx=$ox-2;$gx-lt$ox+$pW+2;$gx++){
                    $inP=($gx-ge$ox -and $gx-lt$ox+$pW -and $gy-ge$oy -and $gy-lt$oy+$pH)
                    if(-not $inP -and $gx-ge 0 -and $gx-lt$dst.Width -and $gy-ge 0 -and $gy-lt$dst.Height){
                        $ex=$dst.GetPixel($gx,$gy)
                        if($ex.A-gt 20){
                            $nr=[Math]::Min(255,[int]($ex.R*0.4+0x60))
                            $ng=[Math]::Min(255,[int]($ex.G*0.2+0x10))
                            $nb=[Math]::Min(255,[int]($ex.B*0.4+0x30))
                            $dst.SetPixel($gx,$gy,[System.Drawing.Color]::FromArgb($ex.A,$nr,$ng,$nb))
                        }
                    }
                }
            }
            # Phone body — #F7D8FF screen
            Rect $dst $ox $oy $pW $pH $White
            Rect $dst ($ox+2) ($oy+2) ($pW-4) ($pH-6) (C 0x20 0x10 0x38)
            Rect $dst ($ox+5) ($oy+2) 10 2 (C 0x14 0x0A 0x28)
            Px $dst ($ox+$pW-4) ($oy+3) (C 0x14 0x0A 0x28)
            # Screen shows pink/purple content
            Rect $dst ($ox+3) ($oy+5) ($pW-6) ($pH-10) (C 0x1A 0x0A 0x2E)
            Rect $dst ($ox+4) ($oy+7) ($pW-8) 3 $Pink   # like icon row
            Rect $dst ($ox+8) ($oy+14) 5 3 $White       # home button
        }
    }
    Save $dst "$outDir\Character\Player_Spritesheet.png"; $dst.Dispose()
} else { Write-Warning "User-walk-v2.png not found" }

# ════════════════════════════════════════════════════════════════════
# 3. LIKE CREATURE  (32×80 — 10 sprites: 2 frames × 5 tiers)
#    Style: chunky 16-bit pixel hearts with eyes
# ════════════════════════════════════════════════════════════════════
Write-Host "[3] Like Creature"
$LB = NewBmp 32 80

# 5×5 heart bitmap (matches design doc chunky style)
$hMap=@(
    "0011011000",
    "0111111100",
    "1111111110",
    "1111111110",
    "0111111100",
    "0011111000",
    "0001110000",
    "0000100000"
)
$hW=10; $hH=8

# Per-tier: body color, eye style (0=normal, 1=pixel eyes)
$tiers=@(
    @{c=$Pink;    eyes=0; glitch=$false},   # T1 normal
    @{c=$Pink;    eyes=0; glitch=$false},   # T1 frame2
    @{c=(C 0xFF 0x20 0x70); eyes=0; glitch=$false},  # T2
    @{c=(C 0xFF 0x20 0x70); eyes=0; glitch=$false},
    @{c=$Red;     eyes=1; glitch=$true},    # T3 (losing control)
    @{c=$Red;     eyes=1; glitch=$true},
    @{c=$Purple;  eyes=1; glitch=$false},   # Clone
    @{c=$Purple;  eyes=1; glitch=$false},
    @{c=(C 0x40 0x08 0x80); eyes=1; glitch=$true},  # Max
    @{c=(C 0x40 0x08 0x80); eyes=1; glitch=$true}
)

$rng=[System.Random]::new(13)

for($si=0;$si-lt 10;$si++){
    $sCol=$si%2; $sRow=[int]($si/2)
    $xO=$sCol*16; $yO=$sRow*16
    $tier=$tiers[$si]
    $bc=$tier.c

    Rect $LB $xO $yO 16 16 $Clear

    # Draw heart (10×8 bitmap, scaled 1:1, centered at x+3, y+2)
    for($hy=0;$hy-lt$hH;$hy++){
        for($hx=0;$hx-lt$hW;$hx++){
            if($hMap[$hy][$hx] -eq '1'){
                $px=$xO+3+$hx; $py=$yO+2+$hy
                Px $LB $px $py $bc
                # 1px brighter highlight at top-left of heart
                if($hy-le 2-and$hx-le 4){ Px $LB $px $py (C ([Math]::Min(255,$bc.R+60)) ([Math]::Min(255,$bc.G+40)) ([Math]::Min(255,$bc.B+40))) }
            }
        }
    }

    # Eyes — white squares
    if($tier.eyes -eq 0){
        Rect $LB ($xO+5) ($yO+5) 2 2 $White
        Rect $LB ($xO+9) ($yO+5) 2 2 $White
    } else {
        # Pixel cross eyes (× shape)
        Px $LB ($xO+5) ($yO+5) $White; Px $LB ($xO+6) ($yO+6) $White
        Px $LB ($xO+6) ($yO+5) $White; Px $LB ($xO+5) ($yO+6) $White
        Px $LB ($xO+9) ($yO+5) $White; Px $LB ($xO+10) ($yO+6) $White
        Px $LB ($xO+10) ($yO+5) $White; Px $LB ($xO+9) ($yO+6) $White
    }

    # Legs (alternating per frame)
    $legOff=$si%2
    Px $LB ($xO+6)  ($yO+11+$legOff) $bc
    Px $LB ($xO+9)  ($yO+11+(1-$legOff)) $bc
    Px $LB ($xO+6)  ($yO+12+$legOff) $bc
    Px $LB ($xO+9)  ($yO+12+(1-$legOff)) $bc

    if($tier.glitch){
        for($gi=0;$gi-lt 3;$gi++){
            Px $LB ($xO+$rng.Next(0,16)) ($yO+$rng.Next(0,16)) $Blue
        }
    }
}
Save $LB "$outDir\Enemies\LikeCreature_Spritesheet.png"; $LB.Dispose()

# ════════════════════════════════════════════════════════════════════
# 4. COLLECTIBLES — 16-bit pixel art, pink palette
# ════════════════════════════════════════════════════════════════════
Write-Host "[4] Collectibles"

# NotifDot — tiny 4×4 pixel dot centered in 8×8 (classic Pac-Man style)
$D=NewBmp 8 8; Rect $D 0 0 8 8 $Clear
Rect $D 3 3 2 2 $Pink     # 2×2 pixel core
Px $D 2 3 $PinkDim; Px $D 5 3 $PinkDim
Px $D 3 2 $PinkDim; Px $D 3 5 $PinkDim   # cross glow
Save $D "$outDir\Collectibles\NotifDot.png"; $D.Dispose()

# Like icon — pink heart (design doc style)
function DrawHeart16([System.Drawing.Bitmap]$bmp,[int]$ox,[int]$oy,[int]$sz=12){
    $hBits=@("01100110","11111111","11111111","01111110","00111100","00011000","00000000")
    for($hy=0;$hy-lt 7;$hy++){
        for($hx=0;$hx-lt 8;$hx++){
            if($hBits[$hy][$hx]-eq'1'){
                Px $bmp ($ox+$hx) ($oy+$hy) $Pink
                if($hy-le 1-and$hx-ge 2-and$hx-le 4){ Px $bmp ($ox+$hx) ($oy+$hy) $White }
            }
        }
    }
}

# Snapchat (notification bell style - 16×16)
$SN=NewBmp 16 16; Rect $SN 0 0 16 16 $Black
Bord $SN 0 0 16 16 $Pink 1
DrawHeart16 $SN 4 5
Save $SN "$outDir\Collectibles\Snapchat_Icon.png"; $SN.Dispose()

# Instagram (message bubble - 16×16)
$IG=NewBmp 16 16; Rect $IG 0 0 16 16 $Black
Bord $IG 0 0 16 16 $Purple 1
Bord $IG 3 3 10 8 $Purple 1
Rect $IG 5 6 6 2 $Purple
Px $IG 5 12 $Purple; Px $IG 4 13 $Purple
Save $IG "$outDir\Collectibles\Instagram_Icon.png"; $IG.Dispose()

# TikTok (play triangle - 16×16)
$TT=NewBmp 16 16; Rect $TT 0 0 16 16 $Black
Bord $TT 0 0 16 16 $Violet 1
for($ti=0;$ti-lt 7;$ti++){ Rect $TT (5+$ti) (4+$ti) (7-$ti) 1 $Violet; Rect $TT (5+$ti) (11-$ti) (7-$ti) 1 $Violet }
Rect $TT 5 7 1 2 $White
Save $TT "$outDir\Collectibles\TikTok_Icon.png"; $TT.Dispose()

# Twitter (bell notification - 16×16)
$TW=NewBmp 16 16; Rect $TW 0 0 16 16 $Black
Bord $TW 0 0 16 16 $Blue 1
Rect $TW 7 2 2 1 $Blue
for($bi=0;$bi-lt 6;$bi++){ Rect $TW (5-[int]($bi/2)) (3+$bi) (6+$bi) 1 $Blue }
Rect $TW 4 9 8 2 $Blue
Rect $TW 6 11 4 2 $Blue
Save $TW "$outDir\Collectibles\Twitter_Icon.png"; $TW.Dispose()

# BonusPoints — "99+" badge (design doc style)
$BP=NewBmp 16 16; Rect $BP 0 0 16 16 $Black
Bord $BP 0 0 16 16 $Pink 1
Rect $BP 2 2 12 12 (C 0x20 0x08 0x18)
# Heart icon small
Px $BP 5 5 $Pink; Px $BP 8 5 $Pink; Px $BP 4 6 $Pink; Px $BP 5 6 $Pink; Px $BP 8 6 $Pink; Px $BP 9 6 $Pink
for($bx=4;$bx-le 9;$bx++){ Px $BP $bx 7 $Pink }
for($bx=5;$bx-le 8;$bx++){ Px $BP $bx 8 $Pink }
Px $BP 6 9 $Pink; Px $BP 7 9 $Pink
# "99+" text dots
Rect $BP 3 11 3 2 $White; Rect $BP 7 11 3 2 $White; Px $BP 11 11 $White; Px $BP 11 12 $White
Save $BP "$outDir\Collectibles\BonusPoints.png"; $BP.Dispose()

# FullPhone — phone collectible (matches design doc)
$FP=NewBmp 16 16; Rect $FP 0 0 16 16 $Black
Bord $FP 2 0 12 16 $White 1          # phone body
Rect $FP 3 1 10 12 (C 0x15 0x08 0x2A)  # screen bg
Rect $FP 3 1 10 3  $Purple             # top bar
Rect $FP 5 5 6 1 $Pink; Rect $FP 5 7 6 1 $Pink; Rect $FP 5 9 4 1 $Pink  # content lines
Rect $FP 7 13 2 2 $White               # home button
Save $FP "$outDir\Collectibles\FullPhone.png"; $FP.Dispose()

# ════════════════════════════════════════════════════════════════════
# 5. HAZARDS — 16-bit red/violet style
# ════════════════════════════════════════════════════════════════════
Write-Host "[5] Hazards"

# PopupAd — red window (design doc: "AD  SKIP IN 3")
$PA=NewBmp 16 32; Rect $PA 0 0 16 32 $Black
Bord $PA 0 0 16 32 $Red 1
Rect $PA 1 1 14 7 (C 0x50 0x08 0x14)   # title bar
Rect $PA 1 1 14 2 $Red                  # top accent
Px $PA 13 3 $White; Px $PA 14 3 $White  # X button
for($al=10;$al-lt 28;$al+=4){ Rect $PA 2 $al 12 2 (C 0x25 0x10 0x18) }
Rect $PA 3 26 10 4 (C 0xFF 0x70 0x00)   # CTA orange button
Save $PA "$outDir\Hazards\PopupAd_Sprite.png"; $PA.Dispose()

# AutoPlay — purple play button
$AP=NewBmp 16 16; Rect $AP 0 0 16 16 $Black
Bord $AP 0 0 16 16 $Purple 1
Rect $AP 2 2 12 12 (C 0x20 0x10 0x3A)
for($i=0;$i-lt 6;$i++){ Rect $AP (4+$i) (4+$i) (6-$i) 1 $Purple; Rect $AP (4+$i) (11-$i) (6-$i) 1 $Purple }
Rect $AP 4 4 1 8 $White
Save $AP "$outDir\Hazards\AutoPlay_Sprite.png"; $AP.Dispose()

# TrendingTrap — flame in pink
$TR=NewBmp 16 16; Rect $TR 0 0 16 16 $Black
Bord $TR 0 0 16 16 $Pink 1
$fRows=@(2,3,4,5,5,4,3,2,1)
for($fi=0;$fi-lt 9;$fi++){
    $fw=$fRows[$fi]; $fx=8-[int]($fw/2)
    Rect $TR $fx (12-$fi) $fw 1 $Pink
}
Px $TR 7 4 $White; Px $TR 8 4 $White; Px $TR 8 3 $White
Save $TR "$outDir\Hazards\TrendingTrap_Sprite.png"; $TR.Dispose()

# ════════════════════════════════════════════════════════════════════
# 6. CYBERPUNK BACKGROUND (304×336) — deep violet, PINK neon signs
# ════════════════════════════════════════════════════════════════════
Write-Host "[6] Background"
$BG=NewBmp 304 336

# Sky gradient — very dark purple top to slightly lighter bottom
for($by=0;$by-lt 336;$by++){
    $t=$by/335.0
    $r=[int](0x08*(1-$t)+0x16*$t)
    $g=[int](0x03*(1-$t)+0x06*$t)
    $bv=[int](0x10*(1-$t)+0x22*$t)
    for($bx=0;$bx-lt 304;$bx++){ $BG.SetPixel($bx,$by,[System.Drawing.Color]::FromArgb(255,$r,$g,$bv)) }
}

# Stars — small white/pink pixels
$rng2=[System.Random]::new(7)
for($s=0;$s-lt 100;$s++){
    $sx=$rng2.Next(0,304); $sy=$rng2.Next(0,160)
    $sa=[int](80+$rng2.Next(0,120))
    $BG.SetPixel($sx,$sy,[System.Drawing.Color]::FromArgb($sa,0xF7,0xD8,0xFF))
    if($rng2.Next(0,3)-eq 0){ $BG.SetPixel([Math]::Min(303,$sx+1),$sy,[System.Drawing.Color]::FromArgb($sa/3,0xF7,0xD8,0xFF)) }
}

# Buildings
$bldgs=@(
    @{x=0;   w=48; h=190; c=$BuildDk},
    @{x=44;  w=36; h=150; c=$BuildMid},
    @{x=76;  w=58; h=230; c=$BuildDk},
    @{x=129; w=44; h=175; c=$BuildMid},
    @{x=168; w=54; h=210; c=$BuildDk},
    @{x=216; w=40; h=145; c=$BuildMid},
    @{x=250; w=54; h=195; c=$BuildDk}
)
foreach($bld in $bldgs){
    Rect $BG $bld.x (336-$bld.h) $bld.w $bld.h $bld.c
    # Antenna
    $mx=$bld.x+[int]($bld.w/2)
    for($ay=(336-$bld.h-6);$ay-lt(336-$bld.h);$ay++){ Px $BG $mx $ay (C 0x22 0x10 0x38) }
    Px $BG $mx (336-$bld.h-6) $Red   # red blink

    # Windows — PINK and VIOLET (not cyan — matches palette)
    $winC=@($WinP,$WinV,$WinB,$WinW)
    for($wy=0;$wy-lt 14;$wy++){
        for($wx=0;$wx-lt [int]($bld.w/8);$wx++){
            if($rng2.Next(0,3)-gt 0){
                $wpx=$bld.x+2+$wx*8+$rng2.Next(0,3)
                $wpy=336-$bld.h+4+$wy*12+$rng2.Next(0,4)
                if($wpx+4-lt 304 -and $wpy+3-lt 336){
                    $wc=$winC[$rng2.Next(0,$winC.Length)]
                    if($rng2.Next(0,5)-gt 1){ Rect $BG $wpx $wpy 4 3 $wc }
                    else { Rect $BG $wpx $wpy 4 3 (C 0x16 0x0A 0x22) }
                }
            }
        }
    }
}

# Neon signs — PINK dominant (like design doc)
$signs=@(
    @{x=8;   y=135; w=40; h=14; c=$Pink;   label="LIKE"},
    @{x=80;  y=95;  w=36; h=12; c=$Purple; label="FEED"},
    @{x=136; y=128; w=32; h=10; c=$Pink;   label="99+"},
    @{x=220; y=115; w=38; h=12; c=$Violet; label="SCROLL"}
)
foreach($sg in $signs){
    Bord $BG $sg.x $sg.y $sg.w $sg.h $sg.c 1
    Rect $BG ($sg.x+1) ($sg.y+1) ($sg.w-2) ($sg.h-2) (C $sg.c.R $sg.c.G $sg.c.B 30)
    for($gi=0;$gi-lt 5;$gi++){
        Px $BG ($sg.x+$rng2.Next(1,$sg.w-1)) ($sg.y+$rng2.Next(1,$sg.h-1)) $sg.c
    }
}

# Horizon glow — pink/purple gradient
for($hx=0;$hx-lt 304;$hx++){
    $glo=[int](18*[Math]::Sin([Math]::PI*$hx/304.0))
    $hy2=336-220
    for($gj=0;$gj-lt 3;$gj++){
        $ga=[int](($glo/18.0)*(1-$gj/3.0)*35)
        if($ga-gt 0){ Px $BG $hx ($hy2+$gj) (C 0x80 0x15 0x60 $ga) }
    }
}

Save $BG "$outDir\Background\CyberpunkCity.png"; $BG.Dispose()

# ════════════════════════════════════════════════════════════════════
# 7. MAZE FRAME — neon PINK/VIOLET border (9-slice, 32×32)
# ════════════════════════════════════════════════════════════════════
Write-Host "[7] Maze frame"
$FR=NewBmp 32 32; Rect $FR 0 0 32 32 $Clear
Bord $FR 0 0 32 32 (C 0xFF 0x4D 0x90 100) 1   # outer glow
Bord $FR 1 1 30 30 $Pink 1                      # main bright pink
Bord $FR 2 2 28 28 $PinkDim 1
Bord $FR 3 3 26 26 $Purple 1                    # inner violet
Bord $FR 4 4 24 24 (C 0x40 0x18 0x70) 1
# Corner ornaments
foreach($cx in @(0,24)){ foreach($cy in @(0,24)){
    Px $FR ($cx+4) ($cy+4) $Pink
    Px $FR ($cx+5) ($cy+4) $PinkDim
    Px $FR ($cx+4) ($cy+5) $PinkDim
}}
Save $FR "$outDir\UI\MazeFrame.png"; $FR.Dispose()

# ════════════════════════════════════════════════════════════════════
# 8. HUD ELEMENTS
# ════════════════════════════════════════════════════════════════════
Write-Host "[8] HUD"

# Heart for lives — 8×8 pink chunky (matches design doc ♥ 99+)
$HT=NewBmp 8 8; Rect $HT 0 0 8 8 $Clear
$heartB=@("01101100","11111110","11111110","01111100","00111000","00010000","00000000","00000000")
for($hy=0;$hy-lt 8;$hy++){
    for($hx=0;$hx-lt 8;$hx++){
        if($heartB[$hy][$hx]-eq'1'){
            Px $HT $hx $hy $Pink
            if($hy-le 1-and$hx-ge 1-and$hx-le 2){ Px $HT $hx $hy $White }
        }
    }
}
Save $HT "$outDir\UI\HeartIcon.png"; $HT.Dispose()

# Notification panel — dark glass with pink neon border
$NP=NewBmp 200 64; Rect $NP 0 0 200 64 (C 0x10 0x06 0x20 230)
Bord $NP 0 0 200 64 $Pink 1
Bord $NP 1 1 198 62 $PinkDim 1
Rect $NP 4 4 24 24 (C 0x18 0x08 0x28)
Bord $NP 4 4 24 24 $Purple 1
# Heart icon on notification
Px $NP 10 10 $Pink; Px $NP 14 10 $Pink
for($nx=9;$nx-le 15;$nx++){ Px $NP $nx 11 $Pink }
for($nx=10;$nx-le 14;$nx++){ Px $NP $nx 12 $Pink }
Px $NP 11 13 $Pink; Px $NP 12 13 $Pink
# Text lines
Rect $NP 34 12 130 4 $White
Rect $NP 34 22 90 3 $Violet
Rect $NP 34 30 60 3 (C 0x50 0x30 0x70)
Save $NP "$outDir\UI\NotificationPanel.png"; $NP.Dispose()

$HU=NewBmp 4 4; Rect $HU 0 0 4 4 $White
Save $HU "$outDir\UI\HUD_Elements.png"; $HU.Dispose()

Write-Host "`n=== DONE - 16-bit pink neon palette applied ===" -ForegroundColor Magenta
