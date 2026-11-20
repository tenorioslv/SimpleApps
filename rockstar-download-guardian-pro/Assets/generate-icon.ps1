Add-Type -AssemblyName System.Drawing
$frames = @()
foreach ($size in @(16,24,32,48,64,256)) {
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform($size/256.0,$size/256.0)
    $bg = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#101827'))
    $gold = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F3BE54'))
    $shape = [Drawing.Drawing2D.GraphicsPath]::new()
    $shape.AddArc(8,8,56,56,180,90); $shape.AddArc(192,8,56,56,270,90)
    $shape.AddArc(192,192,56,56,0,90); $shape.AddArc(8,192,56,56,90,90); $shape.CloseFigure()
    $graphics.FillPath($bg,$shape)
    $points = [Drawing.PointF[]]@([Drawing.PointF]::new(128,40),[Drawing.PointF]::new(202,70),[Drawing.PointF]::new(194,140),[Drawing.PointF]::new(170,181),[Drawing.PointF]::new(128,213),[Drawing.PointF]::new(86,181),[Drawing.PointF]::new(62,140),[Drawing.PointF]::new(54,70))
    $graphics.FillPolygon($gold,$points)
    $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#101827'),17)
    $pen.StartCap=[Drawing.Drawing2D.LineCap]::Round; $pen.EndCap=[Drawing.Drawing2D.LineCap]::Round; $pen.LineJoin=[Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawLines($pen,[Drawing.PointF[]]@([Drawing.PointF]::new(94,125),[Drawing.PointF]::new(118,149),[Drawing.PointF]::new(164,101)))
    $memory=[IO.MemoryStream]::new(); $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $frames += @{ Size=$size; Bytes=$memory.ToArray() }
    if ($size -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'guardian.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $memory.Dispose(); $pen.Dispose(); $shape.Dispose(); $gold.Dispose(); $bg.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$stream=[IO.File]::Create((Join-Path $PSScriptRoot 'guardian.ico')); $writer=[IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset=6+16*$frames.Count
    foreach ($frame in $frames) {
        $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose(); $stream.Dispose() }
