// /home/schani/Work/mono/bwprocessor/bwprocessor/GeglBWProcessor.cs created with MonoDevelop
// User: schani at 11:08 PM 6/29/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gegl;
using GLib;

public class GeglBWProcessor : BWProcessor
{
	Node gegl;
	Node load;
	Node mixer;
	Node contrast;
	Node tint;

	public GeglBWProcessor (string filename) : base()
	{
		gegl = new Gegl.Node ();
		load = gegl.CreateChild ("load");
		mixer = gegl.CreateChild ("mono-mixer");
		contrast = gegl.CreateChild ("contrast-curve");
		tint = gegl.CreateChild ("tint");
		
		load.SetProperty ("path", filename);

		mixer.SetProperty ("red", Red);
		mixer.SetProperty ("green", Green);
		mixer.SetProperty ("blue", Blue);
		
		contrast.SetProperty ("curve", ContrastCurve);
		contrast.SetProperty ("sampling_points", 2048);

		tint.SetProperty ("hue", TintHue);
		tint.SetProperty ("saturation", TintAmount);

		load.Link(mixer);
		mixer.Link(contrast);
		contrast.Link(tint);
	}

	protected GeglBWProcessor (Gegl.Node _gegl) : base()
	{
		gegl = _gegl;

		SList children = gegl.Children;
			
		load = mixer = contrast = tint = null;
		foreach (Node c in children) {
			string op = c.Operation;
			if (op == "load")
				load = c;
			else if (op == "mono-mixer")
				mixer = c;
			else if (op == "contrast-curve")
				contrast = c;
			else if (op == "tint")
				tint = c;
		}

		if (load == null || mixer == null || contrast == null || tint == null)
			throw new ApplicationException("not all nodes found");
		
		Value val = new Value(Gegl.Curve.GType);
		contrastCurve = (GLib.Object)contrast.GetProperty("curve", ref val) as Gegl.Curve;
	}
	
	public static GeglBWProcessor Load (string filename)
	{
		return new GeglBWProcessor (Gegl.Global.ParseXml (System.IO.File.ReadAllText (filename), null));
	}

	public override void Save (string filename)
	{
		string str = Gegl.Global.ToXml(tint, null);
		System.IO.File.WriteAllText(filename, str);
	}

	public override void Export (string filename)
	{
		Node save = gegl.CreateChild("png-save");
		save.SetProperty("path", filename);
		tint.Link(save);
		save.Process();
		save.Disconnect("input");
	}

	public override float Red {
		set { base.Red = value; mixer.SetProperty ("red", value); }
	}

	public override float Green {
		set { base.Green = value; mixer.SetProperty ("green", value); }
	}

	public override float Blue {
		set { base.Blue = value; mixer.SetProperty ("blue", value); }
	}

	public override float TintHue {
		set { base.TintHue = value; tint.SetProperty ("hue", value); }
	}

	public override float TintAmount {
		set { base.TintAmount = value; tint.SetProperty ("saturation", value); }
	}

	public override int Width {
		get {
			Gegl.Rectangle r = tint.BoundingBox;
			return r.Width;
		}
	}

	public override int Height {
		get {
			Gegl.Rectangle r = tint.BoundingBox;
			return r.Height;
		}
	}
	
	public override void ContrastCurveUpdated ()
	{
		contrast.SetProperty ("curve", ContrastCurve);
	}

	public override byte[] Render (int x, int y, int width, int height,
	                               int resultWidth, int resultHeight)
	{
		Gegl.Rectangle roi = new Gegl.Rectangle();
		roi.Set (x, y, (uint)resultWidth, (uint)resultHeight);
		double scale = (double)resultWidth / (double)width;
		tint.Process();
		return tint.Render (roi, scale, "R'G'B' u8", Gegl.BlitFlags.Cache | Gegl.BlitFlags.Dirty);
	}
	
	public override ushort[] QueryPixel (int x, int y,
	                                     out ushort mixed, ushort[] layered)
	{
		mixed = 0;
		return null;
	}
}
