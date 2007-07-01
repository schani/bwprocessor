// /home/schani/Work/mono/bwprocessor/bwprocessor/Curve.cs created with MonoDevelop
// User: schani at 4:42 PM 7/1/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;

public class Curve
{
	class CurvePoint : IComparable
	{
		public float x;
		public float y;
		public float y2;
	
		public CurvePoint (float _x, float _y)
		{
			x = _x;
			y = _y;
			y2 = 0.0f;
		}
		
		public int CompareTo (object o)
		{
			CurvePoint p = (CurvePoint)o;
			
			if (x < p.x)
				return -1;
			if (x > p.x)
				return 1;
			return 0;
		}
	}

	float yMin;
	float yMax;
	ArrayList points;
	CurvePoint[] ps; //sorted
	bool needRecalc;

	public Curve (float _yMin, float _yMax)
	{
		yMin = _yMin;
		yMax = _yMax;
		points = new ArrayList ();
	}
	
	public uint AddPoint (float x, float y)
	{
		points.Add (new CurvePoint (x, y));
		needRecalc = true;
		return (uint)(points.Count - 1);
	}
	
	public void RemovePointIndex (uint index)
	{
		points.RemoveAt ((int)index);
		needRecalc = true;
	}
	
	public uint NumPoints ()
	{
		return (uint)points.Count;
	}
	
	public void GetPoint (uint index, out float x, out float y)
	{
		CurvePoint p = (CurvePoint)points [(int)index];
		
		x = p.x;
		y = p.y;
	}
	
	public void SetPoint (uint index, float x, float y)
	{
		CurvePoint p = new CurvePoint (x, y);
		
		points [(int)index] = p;
		
		needRecalc = true;
	}
	
	private void Recalculate ()
	{
		if (!needRecalc)
			return;
		
		int len = points.Count;

		ArrayList sorted = (ArrayList)points.Clone ();
		sorted.Sort ();
		ps = new CurvePoint [len];
		for (int i = 0; i < len; ++i)
			ps [i] = (CurvePoint)sorted [i];

		needRecalc = false;
		
		if (len < 2)
			return;
		
		float[] b = new float [len - 1];

		// lower natural boundary condition
		ps [0].y2 = b [0] = 0.0f;
		
		for (int i = 1; i < len - 1; ++i) {
			float sig = (ps [i].x - ps [i-1].x) / (ps [i+1].x - ps [i-1].x);
			float p = sig * ps [i-1].y2 + 2;
			
			ps [i].y2 = (sig - 1) / p;
			b [i] = (ps [i+1].y - ps [i].y) / (ps [i+1].x - ps [i].x)
				- (ps [i].y - ps [i-1].y) / (ps [i].x - ps [i-1].x);
			b [i] = (6 * b [i] / (ps [i+1].x - ps [i-1].x) - sig * b [i-1]) / p;
		}
		
		// upper natural boundary condition
		ps [len-1].y2 = 0.0f;
		for (int k = len - 2; k >= 0; --k)
			ps [k].y2 = ps [k].y2 * ps [k+1].y2 + b [k];
	}
	
	private uint FindInterval (float u)
	{
		int i = 0;
		int j = ps.Length - 1;
		
		while (j - i > 1) {
			int k = (i + j) / 2;
			if (ps [k].x > u)
				j = k;
			else
				i = k;
		}
		
		return (uint)i;
	}

	private float YClamp (float y)
	{
		if (y < yMin)
			return yMin;
		if (y > yMax)
			return yMax;
		return y;
	}
	
	private float Apply (float u, uint i)
	{
		float h = ps [i+1].x - ps [i].x;
		float a = (ps [i+1].x - u) / h;
		float b = (u - ps [i].x) / h;
		float y = a * ps [i].y + b * ps [i+1].y
			+ ((a*a*a - a) * ps [i].y2
			   + (b*b*b - b) * ps [i+1].y2) * (h*h)/6;
		
		return YClamp (y);
	}
	
	public float CalcValue (float x)
	{
		Recalculate ();
		
		if (points.Count >= 2)
			return Apply (x, FindInterval (x));
		else if (points.Count == 1)
			return YClamp (ps [0].y);
		else
			return yMin;
	}
	
	public void CalcValues (float xMin, float xMax, float[] xs, float[] ys)
	{
		int len = points.Count;
		int numSamples = xs.Length;
		
		Recalculate ();
		
		int j = 0;
		for (int i = 0; i < numSamples; ++i) {
			float u = xMin + (xMax - xMin) * (float)i / (float)(numSamples - 1);
			
			xs [i] = u;
			
			if (len >= 2) {
				while (j < len - 2 && ps [j+1].x < u)
					++j;
				
				ys [i] = Apply (u, (uint)j);
			} else if (len == 1)
				ys [i] = YClamp (ps [0].y);
			else
				ys [i] = yMin;
		}
	}
}
