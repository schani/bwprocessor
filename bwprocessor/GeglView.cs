using Gtk;
using Gdk;
using System;

namespace Gegl
{
	public delegate void PixelQueriedEventHandler (object source, int x, int y);

    // C# implementation of GeglView used in the test application, leaves
    // handling of mouse events to other classes.
    class View : Gtk.DrawingArea
    {
		private BWProcessor processor;
        private int x, y;
        private double scale;
		private int dragStartX, dragStartY;
		private double dragStartMouseX, dragStartMouseY;

		public event PixelQueriedEventHandler PixelQueried;

		private void NormalizeXY ()
		{
			if ((int)(x * Scale) + Allocation.Width > (int)(processor.Width * Scale))
				x = (int)(((int)(processor.Width * Scale) - Allocation.Width) / Scale);
			if (x < 0)
				x = 0;

			if ((int)(y * Scale) + Allocation.Height > (int)(processor.Height * Scale))
				y = (int)(((int)(processor.Height * Scale) - Allocation.Height) / Scale);
			if (y < 0)
				y = 0;
		}

        /* Properties */
        public int X {
            get { return x; }
            set {
				x = value;
				NormalizeXY ();
				QueueDraw();
			}
        }

        public int Y {
            get { return y; }
            set {
				y = value;
				NormalizeXY ();
				QueueDraw();
			}
        }

        public double Scale {
            get { return scale; }
            set {
                scale = value;
                if (scale == 0.0)
                    scale = 1.0;

                QueueDraw();
            }
        }

		public void NodeUpdated ()
		{
			Repaint(Allocation.Width, Allocation.Height);
		}
		
        public BWProcessor Processor {
            get { return processor; }
            set {
                processor = value;
                Repaint(Allocation.Width, Allocation.Height);
            }
        }
      
        /* Constructor */
        public View(BWProcessor _processor) : base ()
        {
			processor = _processor;

            X = Y = 0;
            Scale = 1.0;

            ExposeEvent += HandleViewExposed;
			ConfigureEvent += HandleResize;
			
			Events |= (EventMask.Button1MotionMask | EventMask.ButtonPressMask);
        }

        public void Repaint (int width, int height)
        {
            Gegl.Rectangle processor_rect = new Gegl.Rectangle();
            processor_rect.Set(x, y, (uint) (width / scale), (uint) (height/scale));
			HandleNodeComputed (null, processor_rect);
        }

        public void ZoomTo (float new_zoom)
        {
            x = x + (int) (Allocation.Width/2 / scale);
            y = y + (int) (Allocation.Height/2 / scale);
            scale = new_zoom;
            X = x - (int) (Allocation.Width/2 / scale);
            Y = y - (int) (Allocation.Height/2 / scale);
			QueueDraw ();
        }

        /* Event Handlers */

		public void HandleResize (object sender, ConfigureEventArgs args)
		{
			Gdk.EventConfigure ev = args.Event;
			Console.WriteLine ("resized: " + ev.X + " " + ev.Y + " " + ev.Width + " " + ev.Height);
			NormalizeXY ();
			Repaint (ev.Width, ev.Height);
		}

        private void HandleViewExposed(object sender, ExposeEventArgs args)
        {
			//Console.WriteLine("exposed");
            if (processor != null) {
				/*
                foreach (Gdk.Rectangle rect in args.Event.Region.GetRectangles()) {
					byte [] buf = processor.Render ((int)(X + rect.X/Scale), (int)(Y + rect.Y/Scale),
					                                (int)(rect.Width/Scale), (int)(rect.Height/Scale),
					                                rect.Width, rect.Height);
                    
                    this.GdkWindow.DrawRgbImage(
                        Style.WhiteGC,
                        rect.X, rect.Y,
                        rect.Width, rect.Height,
                        Gdk.RgbDither.None, buf, rect.Width*3
                    );
                }
				 * */
				
				int rectX = X;
				int rectY = Y;
				int resultWidth = Math.Min (Allocation.Width, (int)((processor.Width - X) * Scale));
				int resultHeight = Math.Min (Allocation.Height, (int)((processor.Height - Y) * Scale));
				int rectWidth = (int)(resultWidth / Scale);
				int rectHeight = (int)(resultHeight / Scale);
				byte[] buf = processor.Render (rectX, rectY, rectWidth, rectHeight,
				                               resultWidth, resultHeight);
				//Console.WriteLine ("pixel " + buf[6] + " " + buf[7] + " " + buf[8]);
				this.GdkWindow.DrawRgbImage (Style.WhiteGC, 0, 0,
				                             resultWidth, resultHeight,
				                             Gdk.RgbDither.None, buf, resultWidth * 3);
            }

            args.RetVal = false;  // returning false here, allows cairo to hook in and draw more
        }

        private void HandleNodeComputed(object o, Gegl.Rectangle rectangle)
        {
            rectangle.X = (int) (Scale * (rectangle.X - X));
            rectangle.Y = (int) (Scale * (rectangle.Y - Y));
            rectangle.Width = (int) Math.Ceiling(rectangle.Width * Scale);
            rectangle.Height = (int) Math.Ceiling(rectangle.Height * Scale);

            QueueDrawArea(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
		
		private void MouseToImageCoords (double mouseX, double mouseY, out int pixelX, out int pixelY)
		{
			pixelX = (int)(mouseX / Scale + X);
			pixelY = (int)(mouseY / Scale + Y);
		}
		
		protected override bool OnButtonPressEvent (EventButton ev)
		{
			Gdk.ModifierType state = (Gdk.ModifierType)ev.State;
			if (ev.Button == 1) {
				dragStartX = X;
				dragStartY = Y;
				dragStartMouseX = ev.X;
				dragStartMouseY = ev.Y;
			}
			if ((state & Gdk.ModifierType.ShiftMask) == 0) {
				if (PixelQueried != null) {
					int pixelX, pixelY;
					MouseToImageCoords (ev.X, ev.Y, out pixelX, out pixelY);
					PixelQueried (this, pixelX, pixelY);
				}
			}
			return true;
		}

		protected override bool OnMotionNotifyEvent (EventMotion ev)
		{
			Gdk.ModifierType state = (Gdk.ModifierType)ev.State;
			if ((state & Gdk.ModifierType.Button1Mask) != 0) {
			    if ((state & Gdk.ModifierType.ShiftMask) != 0) {
					X = dragStartX + (int)((dragStartMouseX - ev.X) / Scale);
					Y = dragStartY + (int)((dragStartMouseY - ev.Y) / Scale);
				} else {
					if (PixelQueried != null) {
						int pixelX, pixelY;
						MouseToImageCoords (ev.X, ev.Y, out pixelX, out pixelY);
						PixelQueried (this, pixelX, pixelY);
					}
				}
				return true;	
			}
			return false;
		}
    }
}
