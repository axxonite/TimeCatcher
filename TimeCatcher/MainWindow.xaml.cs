namespace TimeCatcher
{
	#region Usings

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Windows;
	using System.Windows.Threading;

	#endregion

	public sealed partial class MainWindow : Window
	{
		readonly Dictionary<string, int> _activities = new Dictionary<string, int>();
		readonly DispatcherTimer _timer = new DispatcherTimer();
		DateTime _timeOfLastSave;

		public MainWindow()
		{
			InitializeComponent();
			_timer.Interval = TimeSpan.FromSeconds(1);
			_timer.Tick += UpdateCurrentActivity;
			_timer.Start();
		}

		static object GetForegroundProcessNameAndWindowTitle()
		{
			var foregroundWindow = Win32.GetForegroundWindow();
			var stringBuilder = new StringBuilder(256);
			if (Win32.GetWindowText(foregroundWindow, stringBuilder, 256) == 0)
				return new { ProcessName = "<Unknown>", WindowTitle = "Unknown" };
			uint processID;
			Win32.GetWindowThreadProcessId(foregroundWindow, out processID);
			return new { ProcessName = Path.GetFileName(Process.GetProcessById((int)processID).MainModule.FileName), WindowTitle = stringBuilder.ToString() };
		}

		void SaveLog()
		{
			var sortedActivities = _activities.Select(a => new { ID = a.Key, Seconds = a.Value }).OrderByDescending(a => a.Seconds);
			File.WriteAllText(@"C:\Logs\Log.txt", sortedActivities.Aggregate("", (current, item) => current + $"{item.ID}, {item.Seconds} seconds\r\n"));
			_timeOfLastSave = DateTime.Now;
		}

		static string TimeSpanToString(TimeSpan timespan)
		{
			if (timespan < TimeSpan.FromMinutes(1))
				return $"{timespan.Seconds}s";
			if (timespan < TimeSpan.FromHours(1))
				return $"{timespan.Minutes}m";
			return $"{timespan.Hours}h{timespan.Minutes}m";
		}

		void UpdateCurrentActivity(object sender, EventArgs e)
		{
			dynamic foreground = GetForegroundProcessNameAndWindowTitle();
			var activityID = $"{foreground.ProcessName}: {foreground.WindowTitle}";
			var secondsSpent = _activities.ContainsKey(activityID) ? _activities[activityID] : 0;
			secondsSpent++;
			_activities[activityID] = secondsSpent;
			CurrentActivity.Text = $"{activityID} ({TimeSpanToString(TimeSpan.FromSeconds(secondsSpent))})";
			if (DateTime.Now - _timeOfLastSave > TimeSpan.FromMinutes(1))
				SaveLog();
		}

		void WindowClosed(object sender, EventArgs e) => SaveLog();

		void WindowDeactivated(object sender, EventArgs e) => ((Window)sender).Topmost = true;
	}

	static class Win32
	{
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("user32.dll")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	}
}