Add-Type -AssemblyName System.Drawing

# ═══════════════════════════════════════════════════════════════════
# CHIBI CHARACTER OVERHAUL — restyles Player + LikeCreature enemy to
# the "Wizard Chase" structure (big round head, small blocky body,
# thick black outline per body part, flat 2-3 tone shading) while
# keeping DOOM SCROLLING's own neon palette.
# ═══════════════════════════════════════════════════════════════════
function C([int]$r,[int]$g,[int]$b,[int]$a=255){ [System.Drawing.Color]::FromArgb($a,$r,$g,$b) }

$Pink    = C 0xFF 0x4D 0x90   # Notification Pink
$Purple  = C 0x81 0x5F 0xFF   # Feed Purple
$Violet  = C 0x78 0x6C 0xF6   # Scroll Violet
$Black   = C 0x12 0x0F 0x1E   # Screen Black (outline color)
$White   = C 0xF7 0xD8 0xFF   # Ghost White
$Blue    = C 0x00 0xF5 0xFF   # Hyper Blue
$Red     = C 0xFF 0x3A 0x5E   # Alert Red
$Clear   = [System.Drawing.Color]::Transparent

$PurpleDk = C 0x4A 0x30 0x90   # unused legacy shade (kept for reference)
$PinkDeep = C 0x60 0x08 0x28   # unused legacy shade (kept for reference)

# ── likeness colors, matched to the reference character sheet ────────
# Reference is much more monochrome than earlier passes: near-black hoodie,
# pink as the ONE accent color (rim outline + heart + shoes), not a purple
# family of hoodie/trim/shadow hues.
$Skin       = C 0xE3 0x9C 0x80   # warm tan skin
$HairDk     = C 0x2A 0x14 0x2A   # near-black plum curly hair
$HairHi     = C 0x5A 0x2E 0x54   # subtle curl highlight
$HoodieMain = C 0x20 0x16 0x28   # near-black hoodie (was plum/purple)
$HoodieTrim = C 0x34 0x2A 0x3C   # subtle charcoal shoulder shading — pink stays only the rim outline
$HoodieShad = C 0x14 0x0E 0x1A   # hoodie shadow side, near-black
$ShoePink   = $Pink               # signature bright pink sneakers

$outDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\_Sprites"

function NewBmp([int]$w,[int]$h){ [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Save([System.Drawing.Bitmap]$b,[string]$p){ $b.Save($p,[System.Drawing.Imaging.ImageFormat]::Png); Write-Host "  >> $([IO.Path]::GetFileName($p))" }
function Px([System.Drawing.Bitmap]$b,[int]$x,[int]$y,$c){ if($x-ge 0-and$x-lt$b.Width-and$y-ge 0-and$y-lt$b.Height){$b.SetPixel($x,$y,$c)} }
function Rect([System.Drawing.Bitmap]$b,[int]$x,[int]$y,[int]$w,[int]$h,$c){ for($j=$y;$j-lt$y+$h;$j++){for($i=$x;$i-lt$x+$w;$i++){Px $b $i $j $c}} }

# ═══════════════════════════════════════════════════════════════════
# Logical-pixel character canvas: build a 2D key-map at low resolution,
# then blit each logical cell as an SxS block into the real bitmap.
# Body parts are stamped largest-first so each part's own black halo
# (stamped 1 logical px larger, right before its fill) reads as a
# separate thick outline — the "segmented blocky silhouette" look.
# ═══════════════════════════════════════════════════════════════════
function NewMap([int]$w,[int]$h){
    $m = [System.Array]::CreateInstance([object],$h,$w)
    return ,$m
}
function StampEllipse($map,[int]$cx,[int]$cy,[double]$rx,[double]$ry,$key){
    $h=$map.GetLength(0); $w=$map.GetLength(1)
    $y0=[Math]::Floor($cy-$ry); $y1=[Math]::Ceiling($cy+$ry)
    $x0=[Math]::Floor($cx-$rx); $x1=[Math]::Ceiling($cx+$rx)
    for($y=$y0;$y-le$y1;$y++){
        for($x=$x0;$x-le$x1;$x++){
            if($x-ge 0-and$x-lt$w-and$y-ge 0-and$y-lt$h){
                $nx=($x-$cx)/$rx; $ny=($y-$cy)/$ry
                if(($nx*$nx+$ny*$ny) -le 1.0){ $map[$y,$x]=$key }
            }
        }
    }
}
function StampRect($map,[int]$x0,[int]$y0,[int]$w0,[int]$h0,$key){
    $h=$map.GetLength(0); $w=$map.GetLength(1)
    for($y=$y0;$y-lt$y0+$h0;$y++){
        for($x=$x0;$x-lt$x0+$w0;$x++){
            if($x-ge 0-and$x-lt$w-and$y-ge 0-and$y-lt$h){ $map[$y,$x]=$key }
        }
    }
}
# Rounded rect via distance field (corners clipped to a quarter-circle of
# radius $r) — this is what replaces hard rectangular blocks with soft,
# organic-looking silhouettes for the body/limbs.
function StampRoundedRect($map,[int]$x0,[int]$y0,[int]$w0,[int]$h0,[double]$r,$key){
    $h=$map.GetLength(0); $w=$map.GetLength(1)
    $cxMin=$x0+$r; $cxMax=$x0+$w0-1-$r
    $cyMin=$y0+$r; $cyMax=$y0+$h0-1-$r
    for($y=$y0;$y-lt$y0+$h0;$y++){
        for($x=$x0;$x-lt$x0+$w0;$x++){
            if($x-lt 0-or$x-ge$w-or$y-lt 0-or$y-ge$h){ continue }
            $clx=[Math]::Min([Math]::Max($x,$cxMin),$cxMax)
            $cly=[Math]::Min([Math]::Max($y,$cyMin),$cyMax)
            $dx=$x-$clx; $dy=$y-$cly
            if(($dx*$dx+$dy*$dy) -le ($r*$r)){ $map[$y,$x]=$key }
        }
    }
}
function BlitMap($bmp,$map,[int]$ox,[int]$oy,[int]$scale,$palette){
    $h=$map.GetLength(0); $w=$map.GetLength(1)
    for($y=0;$y-lt$h;$y++){
        for($x=0;$x-lt$w;$x++){
            $key=$map[$y,$x]
            if($key -ne $null){
                Rect $bmp ($ox+$x*$scale) ($oy+$y*$scale) $scale $scale $palette[$key]
            }
        }
    }
}

# ═══════════════════════════════════════════════════════════════════
# Chibi builder — logical grid 24 wide x 27 tall.
#   legPhase: 0 = neutral, 1 = left-leg-forward, 2 = right-leg-forward
#   facing:   "down" | "up" | "side"   (side = facing left; mirror for right)
#   bob:      whole-body vertical offset in logical px (walk bounce)
# ═══════════════════════════════════════════════════════════════════
$HairShadow = C 0x22 0x0E 0x22
$ScreenDark = C 0x14 0x0A 0x22
$palette = @{
    K = $Black; h = $HairDk; N = $HairHi; hd = $HairShadow; g = $Pink
    f = $Skin; e = $Black
    r = $HoodieMain; d = $HoodieShad; v = $HoodieTrim
    p = $White; s = $Pink; o = $Skin; b = $ShoePink; q = $ScreenDark
}

function AddCurlBumps($map,[int]$cx,[int]$topY,[bool]$allAround=$false){
    $bumps = if($allAround){
        # fluffy tufts all around the top and sides, matching the reference's
        # messy all-around curly hair instead of just 3 bumps on top.
        @(
            @{dx=-7;dy=3}, @{dx=-6;dy=0}, @{dx=-3;dy=-2}, @{dx=0;dy=-3},
            @{dx=3;dy=-2}, @{dx=6;dy=0}, @{dx=7;dy=3}
        )
    } else {
        @(@{dx=-6;dy=0}, @{dx=0;dy=-2}, @{dx=6;dy=0})
    }
    foreach($bmp in $bumps){
        $bx=$cx+$bmp.dx; $by=$topY+$bmp.dy
        StampEllipse $map $bx $by 2.5 2.2 g
        StampEllipse $map $bx $by 2.2 1.9 K
        StampEllipse $map $bx $by 1.5 1.2 h
    }
}

function BuildChibiMap([string]$facing,[int]$lOff,[int]$rOff,[int]$bob,[int]$armSwing,[bool]$eyesClosed){
    # Taller, less exaggerated proportions matching the reference sheet
    # (~3.5-4 heads tall, visible hanging arms, phone as its own centered
    # chest panel rather than something held in the hands).
    $W=24; $Hh=30
    $map = NewMap $W $Hh
    $cx = 11
    $headCy = 6 + $bob

    # ── legs (drawn first, behind body) — longer capsules, not stubby ──
    $legY = 24
    StampRoundedRect $map 7  ($legY-1+$lOff) 4 7 1.4 K
    StampRoundedRect $map 13 ($legY-1+$rOff) 4 7 1.4 K
    StampRoundedRect $map 8  ($legY+$lOff)   2 6 1.0 b
    StampRoundedRect $map 14 ($legY+$rOff)   2 6 1.0 b

    # ── body / hoodie — taller torso ──────────────────────────────────
    $bodyY = 12 + $bob
    StampRoundedRect $map 3 ($bodyY-1) 18 13 3.4 g        # thin pink glow rim
    StampRoundedRect $map 4 $bodyY 16 12 3.0 K
    StampRoundedRect $map 5 ($bodyY+1) 14 11 2.6 r
    StampRoundedRect $map 5 ($bodyY+1) 14 2 1.0 v          # subtle shoulder shading
    StampRoundedRect $map 15 ($bodyY+3) 4 8 1.6 d          # shadow side (flat 2-tone)

    # Arms hang naturally at the sides (not holding the phone — it floats on
    # the chest as its own glowing panel, matching the reference). A few px
    # of counter-swing driven by $armSwing.
    $aY = $bodyY + $armSwing

    if($facing -ne "up"){
        # single continuous shape per arm (outline spans the whole arm so the
        # hand reads as connected, not a separate floating blob)
        StampRoundedRect $map ($cx-11) ($aY+2) 4 11 1.8 K
        StampRoundedRect $map ($cx-10) ($aY+3) 2 7  1.3 r
        StampRoundedRect $map ($cx-10) ($aY+9) 2 4  1.0 o
        StampRoundedRect $map ($cx+7)  ($aY+2) 4 11 1.8 K
        StampRoundedRect $map ($cx+8)  ($aY+3) 2 7  1.3 r
        StampRoundedRect $map ($cx+8)  ($aY+9) 2 4  1.0 o
    }

    if($facing -eq "down"){
        # Big centered phone panel on the chest, rounded-rect like a real phone.
        # Generous margins between outline/body/screen so the rounded corners
        # still leave a clean visible border instead of vanishing at the corners.
        StampRoundedRect $map ($cx-6) ($bodyY+2) 13 13 2.2 K
        StampRoundedRect $map ($cx-5) ($bodyY+3) 11 11 1.8 p
        StampRoundedRect $map ($cx-3) ($bodyY+5) 7 7  1.0 q
        # big pink heart glowing on the screen — proper symmetric heart bitmap
        $phoneHeart = @(
            ".XX.XX.",
            "XXXXXXX",
            "XXXXXXX",
            ".XXXXX.",
            "..XXX..",
            "...X..."
        )
        for($hy=0;$hy-lt $phoneHeart.Length;$hy++){
            for($hx=0;$hx-lt $phoneHeart[$hy].Length;$hx++){
                if($phoneHeart[$hy][$hx] -eq 'X'){
                    Px_ $map ($cx-3+$hx) ($bodyY+6+$hy) s
                }
            }
        }
    } elseif($facing -eq "side"){
        # profile: phone edge peeks out from the front of the hoodie
        StampRoundedRect $map 1 ($bodyY+3) 6 8 1.6 K
        StampRoundedRect $map 2 ($bodyY+4) 4 6 1.2 p
        StampRoundedRect $map 3 ($bodyY+5) 2 4 0.8 q
        Px_ $map 3 ($bodyY+6) s
        Px_ $map 3 ($bodyY+7) s
    }
    # (back view: nothing extra — phone isn't visible from behind)

    # ── head ────────────────────────────────────────────────────
    StampEllipse $map $cx $headCy 6.8 6.3 K
    StampEllipse $map $cx $headCy 5.9 5.4 f

    if($facing -eq "up"){
        # full hood/hair covering the whole head, no face
        StampEllipse $map $cx ($headCy) 6.9 6.4 g   # thin pink glow rim
        StampEllipse $map $cx ($headCy) 6.3 5.8 K
        StampEllipse $map $cx ($headCy) 5.4 4.9 h
        AddCurlBumps $map $cx ($headCy-4) $true
        StampRect $map ($cx-2) ($headCy-1) 4 2 hd
    } else {
        # fluffy curly hair, all-around tufts, face exposed below the hairline
        StampEllipse $map $cx ($headCy-3) 6.6 4.0 g   # thin pink glow rim
        StampEllipse $map $cx ($headCy-3) 6.0 3.4 K
        StampEllipse $map $cx ($headCy-3) 5.1 2.7 h
        AddCurlBumps $map $cx ($headCy-5) $true

        $eyeY = $headCy + 2
        $browY = $eyeY - 2
        $eyeXs = if($facing -eq "side"){ @(($cx-4),($cx-1)) } else { @(($cx-3),($cx+2)) }
        foreach($ex in $eyeXs){
            Px_ $map $ex $browY K   # small eyebrow mark
            if($eyesClosed){ Px_ $map $ex $eyeY e; Px_ $map ($ex+1) $eyeY e }
            else{
                StampRect $map $ex $eyeY 1 2 e   # bigger, taller eye
            }
        }
        Px_ $map $cx ($eyeY+3) K   # small open-mouth dot
    }
    return ,$map
}
function Px_($map,[int]$x,[int]$y,$key){
    $h=$map.GetLength(0); $w=$map.GetLength(1)
    if($x-ge 0-and$x-lt$w-and$y-ge 0-and$y-lt$h){ $map[$y,$x]=$key }
}

function MirrorH([System.Drawing.Bitmap]$src){
    $d = NewBmp $src.Width $src.Height
    $g = [System.Drawing.Graphics]::FromImage($d)
    $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0,0,$src.Width,$src.Height)), $src.Width,0,(-$src.Width),$src.Height,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    return $d
}

# ═══════════════════════════════════════════════════════════════════
# PLAYER SPRITESHEET — 8 cols (walk frames) x 6 rows
#   Row0 Down, Row1 Left, Row2 Right, Row3 Up, Row4 Death, Row5 Idle (blink)
# ═══════════════════════════════════════════════════════════════════
Write-Host "[1] Chibi player"
$fw=256; $fh=256
$SCALE=8
$COLS=8; $ROWS=6
$dst = NewBmp ($fw*$COLS) ($fh*$ROWS)

# 8-frame walk cycle: leg offsets peak at frames 2/6 (opposite legs), body
# bobs up during both lift phases, arms counter-swing against the legs.
$lCycle   = @(0,  1,  2,  1,  0, -1, -1,  0)
$rCycle   = @(0, -1, -1,  0,  0,  1,  2,  1)
$bobCycle = @(0, -1, -1,  0,  0, -1, -1,  0)
$armCycle = @(0,  1,  2,  1,  0, -1, -2, -1)

function DrawGroundShadow([System.Drawing.Bitmap]$bmp,[int]$cx,[int]$feetY,[int]$w,[int]$h){
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $brush = New-Object System.Drawing.SolidBrush (C 0x00 0x00 0x00 90)
    $g.FillEllipse($brush, ($cx-$w/2), ($feetY-$h/2), $w, $h)
    $brush.Dispose(); $g.Dispose()
}

function ComposeFrame($facing,[int]$lOff,[int]$rOff,[int]$bob,[int]$armSwing,[bool]$eyesClosed){
    $frame = NewBmp $fw $fh
    $map = BuildChibiMap $facing $lOff $rOff $bob $armSwing $eyesClosed
    $ox = [int](($fw - 24*$SCALE)/2)
    $oy = [int](($fh - 30*$SCALE)/2)
    # Ground shadow, anchored at the feet regardless of the walk-cycle bob,
    # drawn before the character so it reads as sitting under the sprite.
    DrawGroundShadow $frame ($ox+12*$SCALE) ($oy+29*$SCALE) (13*$SCALE) (3*$SCALE)
    BlitMap $frame $map $ox $oy $SCALE $palette
    return $frame
}

for($col=0;$col-lt $COLS;$col++){
    $down = ComposeFrame "down" $lCycle[$col] $rCycle[$col] $bobCycle[$col] $armCycle[$col] $false
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($down,($col*$fw),(0*$fh)); $g.Dispose(); $down.Dispose()

    $left = ComposeFrame "side" $lCycle[$col] $rCycle[$col] $bobCycle[$col] $armCycle[$col] $false
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($left,($col*$fw),(1*$fh)); $g.Dispose()
    $right = MirrorH $left
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($right,($col*$fw),(2*$fh)); $g.Dispose()
    $left.Dispose(); $right.Dispose()

    $up = ComposeFrame "up" $lCycle[$col] $rCycle[$col] $bobCycle[$col] $armCycle[$col] $false
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($up,($col*$fw),(3*$fh)); $g.Dispose(); $up.Dispose()
}

# Death row: reuse Down neutral pose, progressively rotate + tint red + glitch
$rng=[System.Random]::new(7)
for($col=0;$col-lt $COLS;$col++){
    $frame = ComposeFrame "down" 0 0 0 0 $false
    $angle = $col * 12.0
    $rot = NewBmp $fw $fh
    $g=[System.Drawing.Graphics]::FromImage($rot)
    $g.TranslateTransform($fw/2.0,$fh/2.0)
    $g.RotateTransform($angle)
    $g.TranslateTransform(-$fw/2.0,-$fh/2.0)
    $g.DrawImage($frame,0,0)
    $g.Dispose(); $frame.Dispose()

    # red-tint + fade toward black as death progresses
    $mix = $col / ($COLS-1.0)
    for($y=0;$y-lt$fh;$y++){
        for($x=0;$x-lt$fw;$x++){
            $px=$rot.GetPixel($x,$y)
            if($px.A -gt 0){
                $nr=[int]($px.R*(1-$mix)+$Red.R*$mix)
                $ng=[int]($px.G*(1-$mix)+$Red.G*$mix*0.3)
                $nb=[int]($px.B*(1-$mix)+$Red.B*$mix*0.3)
                $rot.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($px.A,$nr,$ng,$nb))
            }
        }
    }
    if($col -ge 3){
        for($gi=0;$gi-lt ($col*3);$gi++){
            Px $rot ($rng.Next(60,196)) ($rng.Next(60,196)) $Blue
        }
    }
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($rot,($col*$fw),(4*$fh)); $g.Dispose(); $rot.Dispose()
}

# Idle row: 2 live frames (open-eyes, blink) + 6 unused columns padded with
# the open-eyes pose so the sheet stays a uniform COLS x ROWS grid.
$idleOpen   = ComposeFrame "down" 0 0 0 0 $false
$idleClosed = ComposeFrame "down" 0 0 0 0 $true
for($col=0;$col-lt $COLS;$col++){
    $src = if($col -eq 1){ $idleClosed } else { $idleOpen }
    $g=[System.Drawing.Graphics]::FromImage($dst); $g.DrawImage($src,($col*$fw),(5*$fh)); $g.Dispose()
}
$idleOpen.Dispose(); $idleClosed.Dispose()

Save $dst "$outDir\Character\Player_Spritesheet.png"; $dst.Dispose()

# ═══════════════════════════════════════════════════════════════════
# LIKE CREATURE — add a thick black outline halo to the existing
# heart-blob silhouette so it reads as a segmented chibi enemy,
# matching the reference's dungeon-monster outline weight.
# ═══════════════════════════════════════════════════════════════════
Write-Host "[2] Chibi like-creature (outlined)"
$LB = NewBmp 32 80
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

$tiers=@(
    @{c=$Pink;    eyes=0; glitch=$false},
    @{c=$Pink;    eyes=0; glitch=$false},
    @{c=(C 0xFF 0x20 0x70); eyes=0; glitch=$false},
    @{c=(C 0xFF 0x20 0x70); eyes=0; glitch=$false},
    @{c=$Red;     eyes=1; glitch=$true},
    @{c=$Red;     eyes=1; glitch=$true},
    @{c=$Purple;  eyes=1; glitch=$false},
    @{c=$Purple;  eyes=1; glitch=$false},
    @{c=(C 0x40 0x08 0x80); eyes=1; glitch=$true},
    @{c=(C 0x40 0x08 0x80); eyes=1; glitch=$true}
)

$rng2=[System.Random]::new(13)

for($si=0;$si-lt 10;$si++){
    $sCol=$si%2; $sRow=[int]($si/2)
    $xO=$sCol*16; $yO=$sRow*16
    $tier=$tiers[$si]
    $bc=$tier.c

    Rect $LB $xO $yO 16 16 $Clear

    # Small ground shadow under the heart so it reads as sitting on the floor.
    $sg = [System.Drawing.Graphics]::FromImage($LB)
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $sBrush = New-Object System.Drawing.SolidBrush (C 0x00 0x00 0x00 90)
    $sg.FillEllipse($sBrush, ($xO+3), ($yO+12), 10, 3)
    $sBrush.Dispose(); $sg.Dispose()

    # 1px black outline halo: any transparent cell adjacent to a filled
    # heart cell becomes black first, then the heart is drawn on top.
    for($hy=0;$hy-lt$hH;$hy++){
        for($hx=0;$hx-lt$hW;$hx++){
            if($hMap[$hy][$hx] -eq '1'){
                for($dy=-1;$dy-le 1;$dy++){
                    for($dx=-1;$dx-le 1;$dx++){
                        $ny=$hy+$dy; $nx=$hx+$dx
                        $outside = ($ny -lt 0 -or $ny -ge $hH -or $nx -lt 0 -or $nx -ge $hW -or $hMap[$ny][$nx] -eq '0')
                        if($outside){
                            Px $LB ($xO+3+$hx+$dx) ($yO+2+$hy+$dy) $Black
                        }
                    }
                }
            }
        }
    }
    for($hy=0;$hy-lt$hH;$hy++){
        for($hx=0;$hx-lt$hW;$hx++){
            if($hMap[$hy][$hx] -eq '1'){
                $px=$xO+3+$hx; $py=$yO+2+$hy
                Px $LB $px $py $bc
                if($hy-le 2-and$hx-le 4){ Px $LB $px $py (C ([Math]::Min(255,$bc.R+60)) ([Math]::Min(255,$bc.G+40)) ([Math]::Min(255,$bc.B+40))) }
            }
        }
    }

    if($tier.eyes -eq 0){
        Rect $LB ($xO+5) ($yO+5) 2 2 $White
        Rect $LB ($xO+9) ($yO+5) 2 2 $White
        Px $LB ($xO+5) ($yO+5) $Black
        Px $LB ($xO+9) ($yO+5) $Black
    } else {
        Px $LB ($xO+5) ($yO+5) $White; Px $LB ($xO+6) ($yO+6) $White
        Px $LB ($xO+6) ($yO+5) $White; Px $LB ($xO+5) ($yO+6) $White
        Px $LB ($xO+9) ($yO+5) $White; Px $LB ($xO+10) ($yO+6) $White
        Px $LB ($xO+10) ($yO+5) $White; Px $LB ($xO+9) ($yO+6) $White
    }

    $legOff=$si%2
    Px $LB ($xO+6)  ($yO+12+$legOff) $Black
    Px $LB ($xO+9)  ($yO+12+(1-$legOff)) $Black
    Px $LB ($xO+6)  ($yO+11+$legOff) $bc
    Px $LB ($xO+9)  ($yO+11+(1-$legOff)) $bc
    Px $LB ($xO+6)  ($yO+12+$legOff) $bc
    Px $LB ($xO+9)  ($yO+12+(1-$legOff)) $bc

    if($tier.glitch){
        for($gi=0;$gi-lt 3;$gi++){
            Px $LB ($xO+$rng2.Next(0,16)) ($yO+$rng2.Next(0,16)) $Blue
        }
    }
}
Save $LB "$outDir\Enemies\LikeCreature_Spritesheet.png"; $LB.Dispose()

# ═══════════════════════════════════════════════════════════════════
# PERSONALITY ACCESSORIES — small silhouette markers so Chaser/Ambusher/
# Flanker/Skittish read as visually distinct beyond just their color tint.
# Solid white so LikeEnemy's runtime tint recolors them to match.
# Saved individually under Resources so LikeEnemy can Resources.Load them
# by name at runtime with no prefab/scene wiring needed.
# ═══════════════════════════════════════════════════════════════════
Write-Host "[3] Personality accessories"
$resDir = "C:\Users\Utilisateur\OneDrive\Bureau\Doom Scrolling\Assets\Resources\PersonalityAccessories"
if(-not(Test-Path $resDir)){ New-Item -ItemType Directory -Force -Path $resDir | Out-Null }

# Chaser — aggressive upward spike/horn (narrow tip at top, wide base at bottom)
$icoSpike = NewBmp 16 16
for($row=0;$row-lt 6;$row++){
    $width=$row+1; $startX=8-[int]($width/2)
    for($i=0;$i-lt$width;$i++){ Px $icoSpike ($startX+$i) (4+$row) $White }
}
Save $icoSpike "$resDir\Spike.png"; $icoSpike.Dispose()

# Ambusher — swept-back wings
$icoWings = NewBmp 16 16
for($row=0;$row-lt 5;$row++){
    Px $icoWings (5-$row) (5+$row) $White
    Px $icoWings (6-$row) (5+$row) $White
    Px $icoWings (10+$row) (5+$row) $White
    Px $icoWings (9+$row) (5+$row) $White
}
Save $icoWings "$resDir\Wings.png"; $icoWings.Dispose()

# Flanker — curled tail
$icoTail = NewBmp 16 16
for($i=0;$i-lt 7;$i++){ Px $icoTail (5+$i) (5+[int]($i/2)) $White }
Px $icoTail 11 8 $White; Px $icoTail 11 7 $White
Save $icoTail "$resDir\Tail.png"; $icoTail.Dispose()

# Skittish — timid little antenna/ears
$icoEars = NewBmp 16 16
for($row=0;$row-lt 5;$row++){
    Px $icoEars 5 (9-$row) $White
    Px $icoEars 10 (9-$row) $White
}
Rect $icoEars 4 3 2 2 $White
Rect $icoEars 9 3 2 2 $White
Save $icoEars "$resDir\Ears.png"; $icoEars.Dispose()

Write-Host "Done."
