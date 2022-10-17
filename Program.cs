using System;
using System.IO;
using System.Diagnostics;

namespace copytest
{
	enum ExitCode
	{
		Success = 0,
		Exception = 1,
		FilesPresent = 2
	}



	class Program
	{
		const string _7Z_EXE = "C:/Program Files/7-Zip/7z.exe";
		const string _7Z_EXTENSION = ".7z";
		const string _7Z_ARGS = " -m0=LZMA2 -mx=9 -mmt=2";

		// Files to read data in from
		static readonly string sources_data =
			@"";
		static readonly string destination_data =
			@"";


		// Variables to hold the data itself
		static string destination;
		static string[] folders;

		static bool is_scheduled_task = false;



		static void Print(string Str = "")
		{
			// Only bother if being executed manually
			if (is_scheduled_task == false)
			{
				Console.WriteLine(Str);
			}
		}



		static void AwaitInput()
		{
			// If being automatically executed, cannot input
			if (is_scheduled_task == false)
			{
				Console.ReadKey();
			}
		}



		static void Beep()
		{
			if (is_scheduled_task == false)
			{
				Console.Beep();
			}
		}



		static string TicksToTime(long Ticks, int MaxItems = 3)
		{
			string Result = string.Empty;
			byte ItemsAdded = 0;

			// Add each element in turn
			void Add(long TicksPerElement, string Append)
			{
				if (Ticks > TicksPerElement && ItemsAdded < MaxItems)
				{
					int Count = (int)(Math.Floor((float)(Ticks / TicksPerElement)));
					Result += $"{Count}{Append}";
					Ticks -= Count * TicksPerElement;
					ItemsAdded++;
				}
			}

			Add(TimeSpan.TicksPerDay, "d ");
			Add(TimeSpan.TicksPerHour, "h ");
			Add(TimeSpan.TicksPerMinute, "m ");
			Add(TimeSpan.TicksPerSecond, "s ");
			Add(TimeSpan.TicksPerMillisecond, "ms ");
			Add(1, " ticks ");

			return Result.TrimEnd(); // As the last one will have added a space
		}



		static void Main(string[] args)
		{
			// Check if this is an automated execution
			is_scheduled_task = args.Length > 0 && args[0] == "IsScheduledTask";

			// Get info from admin
			Print("Folders to backup will be read from:");
			Print(sources_data);
			Print("Destination folder will be read from:");
			Print(destination_data);

			// Read the data in
			try
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
				string date_string = DateTime.Now.ToShortDateString();

				destination = Path.Combine(File.ReadAllText(destination_data), date_string + _7Z_EXTENSION);
				Print($"Writing to: {destination}");

				folders = File.ReadAllLines(sources_data);
				Print($"Reading from the following: {string.Join("; ", folders)}");

				Print("Press any key to begin");
				AwaitInput();
				Print();

				// Check the destination file doesn't already exist
				if (File.Exists(destination))
				{
					Print($"Cannot start backup: destination 7z {destination} already exists");
					Beep();
					AwaitInput();
					Environment.Exit((int)ExitCode.FilesPresent);
				}

				long start_time = DateTime.Now.Ticks;
				Print("Starting...");

				// Make this iterative!!!
				void Launch7Zip(int i)
				{
					if (i == folders.Length)
					{
						// We're done
						Print("Done!");
						Beep();

						long end_time = DateTime.Now.Ticks;
						long time_elapsed = end_time - start_time;

						Print($"Took {TicksToTime(DateTime.Now.Ticks - start_time)}");
						AwaitInput();

						Environment.Exit((int)ExitCode.Success);
					}

					Process Process7z = new Process();
					Process7z.StartInfo.FileName = _7Z_EXE;
					Process7z.StartInfo.Arguments = $"a \"{destination}\" \"{folders[i]}\"" + _7Z_ARGS;

					Print($"Starting with {Process7z.StartInfo.Arguments}");
					Process7z.Start();
					Process7z.WaitForExit();

					Print($"Completed {folders[i]}");
					Launch7Zip(i + 1);
				}

				Launch7Zip(0); // Start with the first folder
			}
			catch (Exception e)
			{
				Print("An exception occured when reading in metadata: " + e);
				AwaitInput();
				Environment.Exit((int)ExitCode.Exception);
			}
		}
	}
}