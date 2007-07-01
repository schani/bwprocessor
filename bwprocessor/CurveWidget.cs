using System;
using System.Collections;
using Gtk;
using Gdk;
using GLib;
using Gnome;
using System.Diagnostics;

public delegate void CurveChangedEventHandler (object source);

public class CurveWidget : Gnome.Canvas
{
	class MyBPath : Gnome.CanvasBpath
	{
		public MyBPath (CanvasGroup cg) : base(cg) {}

	 	public void SetPathDef (CanvasPathDef path_def)
		{
			GLib.Value val = new GLib.Value(path_def, "GnomeCanvasPathDef");
			SetProperty("bpath", val);
		}
	}

	class CurveMarker : IComparable
	{
		private double x, y;
		protected CanvasRect rect;
		protected CurveWidget curveWidget;
		private bool active;

		public CurveMarker (double _x, double _y, CurveWidget _curveWidget, string color)
		{
			x = _x;
			y = _y;
			curveWidget = _curveWidget;
			rect = new CanvasRect(curveWidget.Root());
			rect.FillColor = color;
			rect.OutlineColor = color;
			rect.WidthUnits = 0.0;
			SetRectBounds();
			active = true;
		}

		void SetRectBounds ()
		{
			rect.X1 = x - 0.01;
			rect.Y1 = (1 - y) - 0.01;
			rect.X2 = x + 0.01;
			rect.Y2 = (1 - y) + 0.01;
		}
		
		public double X {
			get { return x; }
			set {
				x = value;
				SetRectBounds();
			}
		}

		public double Y {
			get { return y; }
			set {
				y = value;
				SetRectBounds();
			}
		}
		
		public bool Active {
			get { return active; }
			set {
				active = value;
				if (active)
					rect.Show();
				else
					rect.Hide();
			}
		}
		
		public int CompareTo (object o)
		{
			CurveMarker p = (CurveMarker)o;
			
			if (x < p.x)
				return -1;
			if (x > p.x)
				return 1;
			return 0;
		}
	}

	class CurvePoint : CurveMarker
	{
		double dragY;
		double rememberX, rememberY;

		public CurvePoint (double _x, double _y, CurveWidget _curveWidget) : base(_x, _y, _curveWidget, "black")
		{
			rect.CanvasEvent += new Gnome.CanvasEventHandler (RectEvent);
		}

		void RectEvent (object obj, Gnome.CanvasEventArgs args)
		{
			EventButton ev = new EventButton(args.Event.Handle);
			
			switch (ev.Type)
			{
			case EventType.ButtonPress :
				if (ev.Button == 1) {
				rememberX = ev.X;
				rememberY = ev.Y;
				dragY = Y;
				args.RetVal = true;
				return;
			}
				break;
			case EventType.MotionNotify :
				Gdk.ModifierType state = (Gdk.ModifierType)ev.State;
				if ((state & Gdk.ModifierType.Button1Mask) != 0) {
				//Console.WriteLine("old: " + rememberX + " " + rememberY + " new: " + ev.X + " " + ev.Y);
				X += ev.X - rememberX;
				dragY -= ev.Y - rememberY;
				Y = Math.Min(Math.Max(dragY, 0.0), 1.0);
				rememberX = ev.X;
				rememberY = ev.Y;
				if (Active && (X < 0.0 || X > 1.0)) {
					Console.WriteLine("removing");
					curveWidget.CurvePointRemoved(this);
					Active = false;
				} else if (!Active && (X >= 0.0 && X <= 1.0)) {
					curveWidget.CurvePointAdded(this);
					Console.WriteLine("adding");
					Active = true;
				} else if (Active) {
					Console.WriteLine("updating");
					curveWidget.CurvePointChanged(this);
				}
				args.RetVal = true;
				return;
			}
				break;
			case EventType.ButtonRelease :
				if (!Active) {
				//rect.Dispose();
			}
				break;
			}
			
			args.RetVal = false;
			return;
		}
	}

	private Curve curve;
	private MyBPath bpath;
	private ArrayList points;
	public event CurveChangedEventHandler CurveChanged;
	private CurveMarker marker;

	public bool MarkerActive {
		get { return marker != null && marker.Active; }
		set { marker.Active = value; }
	}
	
	void CurvePointAdded (CurvePoint p)
	{
		curve.AddPoint((float)p.X, (float)p.Y);
		points.Add(p);
		ProcessCurveChange();
	}

	public void AddPoint (double x, double y)
	{
		CurvePointAdded(new CurvePoint(x, y, this));
	}

	protected void UpdateMarker ()
	{
		if (marker != null)
			marker.Y = curve.CalcValue((float)marker.X);
	}

	public void SetMarker (double x)
	{
		marker.X = (float)x;
		UpdateMarker();
		marker.Active = true;
	}
	
	/*
	protected override void OnSizeAllocated (Gdk.Rectangle allocation)
	{
		Console.WriteLine("size allocated " + allocation.Width + " " + allocation.Height);
		PixelsPerUnit = 300; //Math.Min(allocation.Width, allocation.Height);
	}
	 * */

	private void MakeLine (double x1, double y1, double x2, double y2)
	{
		CanvasLine line = new CanvasLine(Root());
		line.Points = new CanvasPoints(new double[]{x1, y1, x2, y2});
		line.WidthPixels = 0;
		line.FillColor = "grey";
	}

	public CurveWidget (Curve _curve) : base()
	{
		PixelsPerUnit = 300;
		
		CanvasRect rect = new CanvasRect(Root());
		rect.X1 = 0;
		rect.Y1 = 0;
		rect.X2 = 1;
		rect.Y2 = 1;
		rect.FillColor = "white";
		rect.OutlineColor = null;
		rect.WidthUnits = 0.0;

		CenterScrollRegion = true;
		SetScrollRegion(0.0, 0.0, 1.0, 1.0);

		curve = _curve;

		bpath = new MyBPath(Root());
		bpath.WidthPixels = 0;
		bpath.OutlineColor = "blue";

		MakeLine(0,0.25, 1,0.25);
		MakeLine(0,0.5, 1,0.5);
		MakeLine(0,0.75, 1,0.75);

		MakeLine(0.25,0, 0.25,1);
		MakeLine(0.5,0, 0.5,1);
		MakeLine(0.75,0, 0.75,1);

		points = new ArrayList();

		for (uint i = 0; i < curve.NumPoints(); ++i) {
			float x, y;
			curve.GetPoint (i, out x, out y);
			points.Add (new CurvePoint(x, y, this));
		}

		bpath.SetPathDef(MakePathDef());

		marker = new CurveMarker(0, 0, this, "dark grey");
		marker.Active = false;

		ButtonPressEvent += new ButtonPressEventHandler(OnButtonPress);
	}

	protected void CalculateArray (float[] xs, float[] ys)
	{
		curve.CalcValues(0, 1, xs, ys);
	}

	protected CanvasPathDef MakePathDef ()
	{
		int numPoints = 100;
		float[] xs = new float[numPoints];
		float[] ys = new float[numPoints];
		CanvasPathDef pd = new CanvasPathDef();

		curve.CalcValues(0, 1, xs, ys);

		pd.MoveTo(xs[0], 1 - ys[0]);
		for (int i = 1; i < numPoints; ++i)
			pd.LineTo(xs[i], 1 - ys[i]);

		return pd;
	}

	private void OnButtonPress (object o, ButtonPressEventArgs args)
	{
		EventButton ev = new EventButton(args.Event.Handle);
		
		if (ev.Button == 1)
		{
			double worldX, worldY;

			WindowToWorld(ev.X, ev.Y, out worldX, out worldY);
			if (worldX >= 0 && worldX <= 1) {
				AddPoint(worldX, 1 - worldY);

				ProcessCurveChange();

				args.RetVal = true;
			}
		}
	}

	int IndexOfPoint (CurvePoint p)
	{
		for (int i = 0; i < points.Count; ++i)
			if (points[i] == p)
				return i;
		return -1;
	}

	protected void ProcessCurveChange ()
	{
		bpath.SetPathDef(MakePathDef());
		UpdateMarker();
		if (CurveChanged != null)
			CurveChanged (this);
	}

	void CurvePointChanged (CurvePoint p)
	{
		int index = IndexOfPoint(p);

		curve.SetPoint((uint)index, (float)p.X, (float)p.Y);
		ProcessCurveChange();
	}
	
	void CurvePointRemoved (CurvePoint p)
	{
		int index = IndexOfPoint(p);
		
		curve.RemovePointIndex((uint)index);
		points.RemoveAt(index);
		ProcessCurveChange();
	}
	
	public Curve Curve {
		get { return curve; }
		set {
			curve = value;
			foreach (CurvePoint p in points) {
				p.Active = false;
			}
			points = new ArrayList();
			uint num = curve.NumPoints();
			for (uint i = 0; i < num; ++i) {
				float x, y;
				curve.GetPoint(i, out x, out y);
				points.Add(new CurvePoint(x, y, this));
				ProcessCurveChange();
			}
		}
	}
}
