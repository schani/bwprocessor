// /home/schani/Work/mono/bwprocessor/bwprocessor/Main.cs created with MonoDevelop
// User: schani at 3:53 PM 6/25/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
// project created on 6/25/2007 at 3:53 PM
using System;
using Gtk;

namespace bwprocessor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}