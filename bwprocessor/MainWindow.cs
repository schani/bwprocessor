// /home/schani/Work/mono/bwprocessor/bwprocessor/MainWindow.cs created with MonoDevelop
// User: schani at 3:53 PM 6/25/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
using System;
using Gtk;
using Gegl;
using GLib;

public partial class MainWindow: Gtk.Window
{
	BWProcessor processor;
	Gegl.View geglView;
	CurveWidget curveWidget;
	int markerX, markerY;

	private void TakeValuesFromProcessor ()
	{
		curveWidget.Curve = processor.ContrastCurve;

		double red = processor.Red * 100.0;
		double green = processor.Green * 100.0;
		double blue = processor.Blue * 100.0;

		redScale.Value = red;
		greenScale.Value = green;
		blueScale.Value = blue;

		double hue = processor.TintHue;
		double amount = processor.TintAmount * 100.0;

		tintHueScale.Value = hue;
		tintAmountScale.Value = amount;
	}
	
	private void NewProcessorWithImage (string filename)
	{
		processor = new CBWProcessor (filename);
		
		TakeValuesFromProcessor ();

		curveWidget.AddPoint (0.25, 0.15);
		curveWidget.AddPoint (0.75, 0.85);
		
		curveWidget.MarkerActive = false;
	}
	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		
		//Gegl.Global.Init();

		curveWidget = new CurveWidget (new Curve (0.0f, 1.0f));
		curveWidget.CurveChanged += new CurveChangedEventHandler(CurveChanged);

		controlsVBox.Remove(curveDummy);
		controlsVBox.PackEnd(curveWidget, true, true, 0);

		NewProcessorWithImage ("/home/schani/Desktop/petra.jpg");

		geglView = new Gegl.View(processor);
		geglView.PixelQueried += new PixelQueriedEventHandler(PixelQueried);

		paned.Remove(geglViewDummy);
		paned.Pack1(geglView, true, false);
		paned.ShowAll();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void MixerValueChanged (object sender, System.EventArgs e)
	{
		if (processor == null || geglView == null)
			return;

		double red = redScale.Value / 100.0;
		double green = greenScale.Value / 100.0;
		double blue = blueScale.Value / 100.0;

		processor.Red = (float)red;
		processor.Green = (float)green;
		processor.Blue = (float)blue;

		Console.WriteLine("mixer changed " + red + " " + green + " " + blue);

		geglView.NodeUpdated();
		UpdateMarker();
	}

	protected virtual void TintChanged (object sender, System.EventArgs e)
	{
		if (processor == null || geglView == null)
			return;

		processor.TintHue = (float)tintHueScale.Value;
		processor.TintAmount = (float)(tintAmountScale.Value / 100.0);

		geglView.NodeUpdated();
		UpdateMarker();
	}
	
	protected virtual void CurveChanged (object sender)
	{
		if (processor == null || geglView == null)
			return;
		
		processor.ContrastCurveUpdated();
		geglView.NodeUpdated();
		UpdateMarker();
	}

	protected void GetPixelValue (int x, int y, out double r, out double g, out double b, out double v)
	{
		byte [] buf = processor.Render(x, y, 1, 1, 1, 1);
		r = (double)buf[0] / 255.0;
		g = (double)buf[1] / 255.0;
		b = (double)buf[2] / 255.0;
		v = r * 0.299 + g * 0.587 + b * 0.114;
	}

	protected virtual void UpdateMarker ()
	{
		if (!curveWidget.MarkerActive)
			return;

		ushort mixed;
		ushort[] layered = new ushort [1];
		double r, g, b, v;

		ushort[] pixel = processor.QueryPixel (markerX, markerY, out mixed, layered);

		r = pixel [0] / 65535.0;
		g = pixel [1] / 65535.0;
		b = pixel [2] / 65535.0;

		v = r * 0.299 + g * 0.587 + b * 0.114; 
			
		statusLabel.Text = "" + v.ToString("0.000") + "   (r:" + r.ToString("0.000") + "  g:" + g.ToString("0.000") + "  b:" + b.ToString("0.000") + ")";

		v = mixed / 65535.0;

		Console.WriteLine("lum: " + v);
		curveWidget.SetMarker(v);
	}
	
	protected virtual void PixelQueried (object sender, int x, int y)
	{
		int width = processor.Width;
		int height = processor.Height;
		
		Console.WriteLine("pixel queried " + x + " " + y);
		Console.WriteLine("bb " + width + " " + height);

		if (x >= 0 && x < width && y >= 0 && y < height) {
			markerX = x;
			markerY = y;
			
			curveWidget.MarkerActive = true;
			UpdateMarker ();
		}
	}
	
	protected virtual void OpenFile (object sender, System.EventArgs e)
	{
		FileChooserDialog fc =
			new FileChooserDialog("Open file",
			                      this,
			                      FileChooserAction.Open,
			                      "Cancel", ResponseType.Cancel,
			                      "Open", ResponseType.Accept);
		
		if (fc.Run() == (int)ResponseType.Accept) {
			processor = CBWProcessor.Load (fc.Filename);

			TakeValuesFromProcessor ();

			geglView.Processor = processor;
		}

		fc.Destroy();
	}

	protected virtual void SaveFileAs (object sender, System.EventArgs e)
	{
		FileChooserDialog fc =
			new FileChooserDialog("Save file",
			                      this,
			                      FileChooserAction.Save,
			                      "Cancel", ResponseType.Cancel,
			                      "Save", ResponseType.Accept);
		
		if (fc.Run() == (int)ResponseType.Accept)
			processor.Save (fc.Filename);
		
		fc.Destroy();
	}

	protected virtual void ExportFile (object sender, System.EventArgs e)
	{
		FileChooserDialog fc =
			new FileChooserDialog("Export file",
			                      this,
			                      FileChooserAction.Save,
			                      "Cancel", ResponseType.Cancel,
			                      "Export", ResponseType.Accept);
		
		if (fc.Run() == (int)ResponseType.Accept)
			processor.Export (fc.Filename);
		
		fc.Destroy();
	}

	protected virtual void ImportFile (object sender, System.EventArgs e)
	{
		FileChooserDialog fc =
			new FileChooserDialog("Open file",
			                      this,
			                      FileChooserAction.Open,
			                      "Cancel", ResponseType.Cancel,
			                      "Open", ResponseType.Accept);
		
		if (fc.Run() == (int)ResponseType.Accept) {
			NewProcessorWithImage (fc.Filename);
			geglView.Processor = processor;
		}
		
		fc.Destroy();
	}

	protected virtual void DoZoomIn (object sender, System.EventArgs e)
	{
		geglView.ZoomTo((float)(geglView.Scale * 2.0));
	}
	
	protected virtual void DoZoomOut (object sender, System.EventArgs e)
	{
		geglView.ZoomTo((float)(geglView.Scale / 2.0));
	}
}
