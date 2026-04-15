using ImageMagick;

// Generates app.ico from AppIcon.svg; optional wizard PNGs for Inno Setup.
// Run from repo root:
//   dotnet run --project tools/IconGen -- <svgPath> <icoOutPath> [wizardLarge.png] [wizardSmall.png]

var argsList = args.ToList();
if (argsList.Count < 2)
{
    Console.Error.WriteLine("Usage: IconGen <input.svg> <output.ico> [wizard-large.png] [wizard-small.png]");
    Environment.Exit(1);
}

var svgPath = Path.GetFullPath(argsList[0]);
var icoPath = Path.GetFullPath(argsList[1]);

if (!File.Exists(svgPath))
{
    Console.Error.WriteLine($"Input not found: {svgPath}");
    Environment.Exit(2);
}

Directory.CreateDirectory(Path.GetDirectoryName(icoPath)!);

var settings = new MagickReadSettings
{
    Format = MagickFormat.Svg,
    BackgroundColor = MagickColors.Transparent,
    // Higher raster size for sharper downscaling into 16–48px taskbar/tray frames.
    Width = 1024,
    Height = 1024
};

using (var image = new MagickImage(svgPath, settings))
{
    image.Format = MagickFormat.Ico;
    // Windows 10/11: taskbar + tray use 16–32px at 100% DPI, 20/24/32 at higher DPI; include all common sizes.
    image.Settings.SetDefine(MagickFormat.Ico, "auto-resize", "256,128,96,64,48,40,32,24,20,16");
    image.Write(icoPath);
}

Console.WriteLine($"Wrote {icoPath}");

void WriteWizardPng(string outPath, int width, int height)
{
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    using var w = new MagickImage(svgPath, settings);
    w.Format = MagickFormat.Png;
    w.Resize(new MagickGeometry((uint)width, (uint)height) { IgnoreAspectRatio = false });
    using var canvas = new MagickImage("xc:none", new MagickReadSettings
    {
        Width = (uint)width,
        Height = (uint)height,
        BackgroundColor = MagickColors.Transparent
    });
    canvas.Format = MagickFormat.Png;
    canvas.Composite(w, Gravity.Center, CompositeOperator.Over);
    canvas.Write(outPath);
    Console.WriteLine($"Wrote {outPath}");
}

if (argsList.Count >= 3)
{
    WriteWizardPng(Path.GetFullPath(argsList[2]), 164, 314);
}

if (argsList.Count >= 4)
{
    WriteWizardPng(Path.GetFullPath(argsList[3]), 55, 58);
}
