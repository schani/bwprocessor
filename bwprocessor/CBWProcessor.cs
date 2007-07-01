// /home/schani/Work/mono/bwprocessor/bwprocessor/CBWProcessor.cs created with MonoDevelop
// User: schani at 8:31 PM 6/30/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Xml;

public class CBWProcessor : BWProcessor
{
	struct ContrastLayer
	{
		public ushort[] curve;
		public ushort[] mask;
	}

	string imageFilename;
	ushort[] imageData;
	int width, height;
	byte[] cacheMask;
	ushort[] cache;
	bool invalidate;
	ContrastLayer[] layers;

	private void LoadImage (string filename)
	{
		imageFilename = filename;

		Bitmap bmp;

		try {
			bmp = new Bitmap (filename);
		} catch (Exception) {
			bmp = new Bitmap (256, 256);
		}

		width = bmp.Width;
		height = bmp.Height;

		Rectangle rect = new Rectangle (0, 0, width, height);
		BitmapData data = bmp.LockBits (rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

		int stride = data.Stride;

		IntPtr ptr = data.Scan0;
		int numBytes = stride * height;

		Console.WriteLine("w:" + width + " h:" + height + " str:" + stride);
				
		byte[] values = new byte [numBytes];

		System.Runtime.InteropServices.Marshal.Copy(ptr, values, 0, numBytes);

		imageData = new ushort[width * height * 3];

		for (int iy = 0; iy < height; ++iy) {
			for (int ix = 0; ix < width; ++ix) {
				imageData[(iy * width + ix) * 3 + 2] = (ushort)(values[iy * stride + ix * 3 + 0] << 8); 
				imageData[(iy * width + ix) * 3 + 1] = (ushort)(values[iy * stride + ix * 3 + 1] << 8); 
				imageData[(iy * width + ix) * 3 + 0] = (ushort)(values[iy * stride + ix * 3 + 2] << 8); 
			}
		}
		
		bmp.UnlockBits (data);

		Console.WriteLine ("image " + imageData [0] + " " + imageData [1] + " " + imageData [2]);

		cacheMask = new byte[width * height];
		cache = new ushort[width * height * 3];
		
		invalidate = true;
	}
	
	public CBWProcessor (string filename) : base ()
	{
		LoadImage (filename);
		
		layers = new ContrastLayer[1];
		layers [0].curve = new ushort [2048];
		layers [0].mask = null;
	}

	public static CBWProcessor Load (string filename)
	{
		XmlTextReader reader = new XmlTextReader (filename);
		XmlDocument doc = new XmlDocument ();

		doc.Load (reader);
		reader.Close ();

		XmlNode root = doc.DocumentElement;

		if (root.Name != "bw")
			throw new ApplicationException ("Wrong root node");

		XmlNode node = root.FirstChild;
		if (node.Name != "load")
			throw new ApplicationException ("Could not find load node");
		CBWProcessor processor = new CBWProcessor (node.InnerText);
		
		node = node.NextSibling;
		if (node.Name != "mixer")
			throw new ApplicationException ("Could not find mixer node");
		
		processor.Red = (float)Double.Parse (node.Attributes.GetNamedItem ("red").Value);
		processor.Green = (float)Double.Parse (node.Attributes.GetNamedItem ("green").Value);
		processor.Blue = (float)Double.Parse (node.Attributes.GetNamedItem ("blue").Value);
		
		node = node.NextSibling;
		if (node.Name != "contrast")
			throw new ApplicationException ("Could not find contrast node");
		
		Curve curve = processor.ContrastCurve;
		while (curve.NumPoints () > 0)
			curve.RemovePointIndex (0);

		XmlNode curveNode = node.FirstChild;
		if (curveNode.Name != "curve")
			throw new ApplicationException ("Could not find curve node");
		curveNode = curveNode.FirstChild;
		while (curveNode != null) {
			if (curveNode.Name != "point")
				throw new ApplicationException ("Could not find point node");
			
			curve.AddPoint ((float)Double.Parse (curveNode.Attributes.GetNamedItem ("x").Value),
			                (float)Double.Parse (curveNode.Attributes.GetNamedItem ("y").Value));
			                
			curveNode = curveNode.NextSibling;
		}

		node = node.NextSibling;
		if (node.Name != "tint")
			throw new ApplicationException ("Could not find tint node");

		processor.TintHue = (float)Double.Parse (node.Attributes.GetNamedItem ("hue").Value);
		processor.TintAmount = (float)Double.Parse (node.Attributes.GetNamedItem ("amount").Value);
		
		return processor;
	}
	
	public override void Save (string filename)
	{
		XmlTextWriter tw = new XmlTextWriter (filename, null);
		
		tw.Formatting = Formatting.Indented;
		
		tw.WriteStartDocument ();
		tw.WriteStartElement ("bw");
		
		tw.WriteElementString ("load", imageFilename);
		
		tw.WriteStartElement ("mixer");
		tw.WriteAttributeString ("red", Red.ToString ());
		tw.WriteAttributeString ("green", Red.ToString ());
		tw.WriteAttributeString ("blue", Red.ToString ());
		tw.WriteEndElement ();
	
		tw.WriteStartElement ("contrast");
		tw.WriteStartElement ("curve");
		for (uint i = 0; i < contrastCurve.NumPoints (); ++i) {
			float x, y;
			contrastCurve.GetPoint (i, out x, out y);
			tw.WriteStartElement ("point");
			tw.WriteAttributeString ("x", x.ToString ());
			tw.WriteAttributeString ("y", y.ToString ());
			tw.WriteEndElement ();
		}
		tw.WriteEndElement ();
		tw.WriteEndElement ();

		tw.WriteStartElement ("tint");
		tw.WriteAttributeString ("hue", TintHue.ToString ());
		tw.WriteAttributeString ("amount", TintAmount.ToString ());
		tw.WriteEndElement ();
		
		tw.WriteEndElement ();
		tw.Flush ();
		tw.Close ();
	}
	
	public override void Export (string filename)
	{
		Bitmap bmp = new Bitmap (width, height);
		
		Rectangle rect = new Rectangle (0, 0, width, height);
		BitmapData data = bmp.LockBits (rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
		int stride = data.Stride;
		IntPtr ptr = data.Scan0;
		
		int numBytes = stride * height;
		byte[] rendered = Render (0, 0, width, height, width, height);
		byte[] values = new byte [numBytes];

		for (int iy = 0; iy < height; ++iy) {
			for (int ix = 0; ix < width; ++ix) {
				values[iy * stride + ix * 3 + 2] = rendered[(iy * width + ix) * 3 + 0]; 
				values[iy * stride + ix * 3 + 1] = rendered[(iy * width + ix) * 3 + 1]; 
				values[iy * stride + ix * 3 + 0] = rendered[(iy * width + ix) * 3 + 2]; 
			}
		}
		
		System.Runtime.InteropServices.Marshal.Copy (values, 0, ptr, numBytes);

		bmp.UnlockBits (data);

		bmp.Save (filename, ImageFormat.Png);
	}

	protected override void ParametersChanged ()
	{
		invalidate = true;
	}
	
	public override int Width {
		get { return width; }
	}

	public override int Height {
		get { return height; }
	}
	
	[DllImport("libbwproc.dll")]
	static extern unsafe void bw_process_layers_8
		(int width, int height, byte *outData, ushort *inData,
		 int numCols, int *cols, int numRows, int *rows,
		 byte *cacheMask, ushort *cache,
		 float red, float green, float blue,
		 int numLayers, IntPtr *layers,
		 float tintHue, float tintAmount);

	private byte[] ProcessIter (int[] cols, int[] rows, IntPtr[] layerArray, int layerIndex)
	{
		unsafe {
			if (layerIndex >= layers.Length) {
				byte[] outData = new byte [cols.Length * rows.Length * 3];
					
				fixed (ushort *_inData = imageData, _cache = cache) {
					fixed (byte *_outData = outData, _cacheMask = cacheMask) {
						fixed (int *_cols = cols, _rows = rows) {
							fixed (IntPtr *_layerArray = layerArray) {
								bw_process_layers_8 (width, height, _outData, _inData,
								                     cols.Length, _cols, rows.Length, _rows,
								                     _cacheMask, _cache,
								                     Red, Green, Blue,
								                     layers.Length, _layerArray,
								                     TintHue, TintAmount);
							}
						}
					}
				}
				
				return outData;
			} else {
				if (layers [layerIndex].mask == null) {
					fixed (ushort *curve = layers [layerIndex].curve) {
						layerArray [layerIndex * 2 + 0] = (IntPtr)curve;
						layerArray [layerIndex * 2 + 1] = IntPtr.Zero;
						return ProcessIter (cols, rows, layerArray, layerIndex + 1);
					}
				} else {
					fixed (ushort *curve = layers [layerIndex].curve,
					       mask = layers [layerIndex].mask) {
						layerArray [layerIndex * 2 + 0] = (IntPtr)curve;
						layerArray [layerIndex * 2 + 1] = (IntPtr)mask;
						return ProcessIter (cols, rows, layerArray, layerIndex + 1);
					}
				}
			}
		}
	}
		
	private byte[] Process (int[] cols, int[] rows)
	{
		IntPtr[] layerArray = new IntPtr [layers.Length * 2];
		return ProcessIter (cols, rows, layerArray, 0);
	}
	
	public override byte[] Render (int rectX, int rectY, int rectWidth, int rectHeight,
	                               int resultWidth, int resultHeight)
	{
		if (invalidate) {
			Console.WriteLine ("invalidating");
			
			cacheMask = new byte [width * height];
			
			float[] xs = new float [2048];
			float[] ys = new float [2048];
			for (int i = 0; i < 2048; ++i)
				xs [i] = i / 2047f;
			ContrastCurve.CalcValues (0.0f, 1.0f, xs, ys);
			for (int i = 0; i < 2048; ++i)
				layers [0].curve [i] = (ushort)(ys [i] * 65535f);
			Console.WriteLine ("Curve " + layers [0].curve [0] + " " + layers [0].curve [2047]);
			
			invalidate = false;
		}

		int[] cols = new int [resultWidth];
		int[] rows = new int [resultHeight];
		
		for (int i = 0; i < resultWidth; ++i)
			cols [i] = rectX + i * rectWidth / resultWidth;

		for (int i = 0; i < resultHeight; ++i)
			rows [i] = rectY + i * rectHeight / resultHeight;
		
		return Process (cols, rows);
	}

	[DllImport("libbwproc.dll")]
	static extern unsafe void query_pixel_layers
		(int width, int height, ushort *outPixel, ushort *inData,
		 int x, int y,
		 float red, float green, float blue,
		 int numLayers, IntPtr *layers,
		 float tintHue, float tintAmount,
		 ushort *mixed, ushort *layered);

	private ushort[] QueryIter (IntPtr[] layerArray, int layerIndex,
	                            int x, int y,
	                            out ushort mixed, ushort[] layered)
	{
		unsafe {
			if (layerIndex >= layers.Length) {
				ushort[] outPixel = new ushort [3];
					
				fixed (ushort *_inData = imageData,
				       _outPixel = outPixel, _layered = layered,
				       _mixed = &mixed) {
					fixed (IntPtr *_layerArray = layerArray) {
						query_pixel_layers (width, height, _outPixel, _inData,
						                    x, y,
						                    Red, Green, Blue,
						                    layers.Length, _layerArray,
						                    TintHue, TintAmount,
						                    _mixed, _layered);
					}
				}
				
				return outPixel;
			} else {
				if (layers [layerIndex].mask == null) {
					fixed (ushort *curve = layers [layerIndex].curve) {
						layerArray [layerIndex * 2 + 0] = (IntPtr)curve;
						layerArray [layerIndex * 2 + 1] = IntPtr.Zero;
						return QueryIter (layerArray, layerIndex + 1, x, y, out mixed, layered);
					}
				} else {
					fixed (ushort *curve = layers [layerIndex].curve,
					       mask = layers [layerIndex].mask) {
						layerArray [layerIndex * 2 + 0] = (IntPtr)curve;
						layerArray [layerIndex * 2 + 1] = (IntPtr)mask;
						return QueryIter (layerArray, layerIndex + 1, x, y, out mixed, layered);
					}
				}
			}
		}
	}
		
	private ushort[] Query (int x, int y, out ushort mixed, ushort[] layered)
	{
		IntPtr[] layerArray = new IntPtr [layers.Length * 2];
		return QueryIter (layerArray, 0, x, y, out mixed, layered);
	}

	public override ushort[] QueryPixel (int x, int y,
	                                     out ushort mixed, ushort[] layered)
	{
		return Query (x, y, out mixed, layered);
	}
}
