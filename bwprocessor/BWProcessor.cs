// /home/schani/Work/mono/bwprocessor/bwprocessor/BWProcessor.cs created with MonoDevelop
// User: schani at 10:46 PM 6/29/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gegl;

public abstract class BWProcessor
{
	float red, green, blue;
	protected Gegl.Curve contrastCurve; 
	float tintHue, tintAmount;

	protected BWProcessor ()
	{
		red = 0.4f;
		green = 0.3f;
		blue = 0.3f;

		contrastCurve = new Gegl.Curve (0.0f, 1.0f);
		contrastCurve.AddPoint (0.0f, 0.0f);
		contrastCurve.AddPoint (1.0f, 1.0f);
		
		tintHue = 23.0f;
		tintAmount = 0.1f;
	}

	public abstract void Save (string filename);
	public abstract void Export (string filename);

	protected virtual void ParametersChanged () { }
	
	public virtual float Red {
		get { return red; }
		set { red = value; ParametersChanged (); }
	}
	
	public virtual float Green {
		get { return green; }
		set { green = value; ParametersChanged (); }
	}

	public virtual float Blue {
		get { return blue; }
		set { blue = value; ParametersChanged (); }
	}

	public virtual Gegl.Curve ContrastCurve {
		get { return contrastCurve; }
	}
	
	public virtual float TintHue {
		get { return tintHue; }
		set { tintHue = value; ParametersChanged (); }
	}

	public virtual float TintAmount {
		get { return tintAmount; }
		set { tintAmount = value; ParametersChanged (); }
	}
	
	public abstract int Width {
		get;
	}
	
	public abstract int Height {
		get;
	}

	public virtual void ContrastCurveUpdated ()
	{
		ParametersChanged ();
	}

	public abstract byte[] Render (int x, int y, int width, int height,
	                               int resultWidth, int resultHeight);
	
	public abstract ushort[] QueryPixel (int x, int y,
	                                     out ushort mixed, ushort[] layered);
}
