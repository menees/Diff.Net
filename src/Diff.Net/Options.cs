namespace Diff.Net
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using System.Windows.Forms;
	using Menees;
	using Menees.Diffs;
	using Menees.Windows.Forms;

	#endregion

	internal static class Options
	{
		#region Private Data Members

		private const float DefaultFontSize = 9.75F;

		private static readonly IList<string> CustomFilters = new List<string>();
		private static bool changed;
		private static bool checkDirExists = true;
		private static bool checkFileExists = true;
		private static bool showWSInLineDiff = true;
		private static bool showWSInMainDiff;
		private static bool showMdiTabs = true;
		private static HashType hashType = HashType.HashCode;
		private static int updateLevel;
		private static Font viewFont = new Font("Courier New", DefaultFontSize, GraphicsUnit.Point);

		#endregion

		#region Public Events

		public static event EventHandler OptionsChanged;

		#endregion

		#region Non-Event-Firing Public Properties

		public static int BinaryFootprintLength { get; set; } = 8;

		public static bool CheckDirExists => checkDirExists;

		public static bool CheckFileExists => checkFileExists;

		public static CompareType CompareType { get; set; }

		public static DirectoryDiffFileFilter FileFilter { get; set; }

		public static bool GoToFirstDiff { get; set; } = true;

		public static HashType HashType => hashType;

		public static bool IgnoreCase { get; set; }

		public static bool IgnoreDirectoryComparison { get; set; } = true;

		public static bool IgnoreTextWhitespace { get; set; }

		public static bool IgnoreXmlWhitespace { get; set; }

		public static string LastDirA { get; set; } = string.Empty;

		public static string LastDirB { get; set; } = string.Empty;

		public static string LastFileA { get; set; } = string.Empty;

		public static string LastFileB { get; set; } = string.Empty;

		public static string LastTextA { get; set; } = string.Empty;

		public static string LastTextB { get; set; } = string.Empty;

		public static int LineDiffHeight { get; set; }

		public static bool OnlyShowDirDialogIfShiftPressed { get; set; }

		public static bool OnlyShowFileDialogIfShiftPressed { get; set; }

		public static bool Recursive { get; set; } = true;

		public static bool ShowChangeAsDeleteInsert { get; set; }

		public static bool ShowDifferent { get; set; } = true;

		public static bool ShowOnlyInA { get; set; } = true;

		public static bool ShowOnlyInB { get; set; } = true;

		public static bool ShowSame { get; set; } = true;

		#endregion

		#region Event-Firing Public Properties

		public static bool ShowWSInLineDiff
		{
			get
			{
				return showWSInLineDiff;
			}

			set
			{
				SetValue(ref showWSInLineDiff, value);
			}
		}

		public static bool ShowWSInMainDiff
		{
			get
			{
				return showWSInMainDiff;
			}

			set
			{
				SetValue(ref showWSInMainDiff, value);
			}
		}

		public static Font ViewFont
		{
			get
			{
				return viewFont;
			}

			set
			{
				SetValue(ref viewFont, value);
			}
		}

		public static bool ShowMdiTabs
		{
			get
			{
				return showMdiTabs;
			}

			set
			{
				SetValue(ref showMdiTabs, value);
			}
		}

		#endregion

		#region Public Methods

		public static bool IsShiftPressed
			/* We have to use Keys.Shift instead of Keys.ShiftKey because "Shift"
			is "The SHIFT modifier key", and it's what ModifierKeys returns. */
			=> (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

		public static void AddCustomFilter(string filter)
		{
			// Case-insensitively look for the current filter
			int index = -1;
			for (int i = 0; i < CustomFilters.Count; i++)
			{
				if (string.Compare(CustomFilters[i], filter, true) == 0)
				{
					index = i;
					break;
				}
			}

			// Remove the old filter if necessary and add it
			// back at the beginning of the list.
			if (index >= 0)
			{
				CustomFilters.RemoveAt(index);
			}

			CustomFilters.Insert(0, filter);

			// Limit the history count to 20.
			const int MaxFilters = 20;
			while (CustomFilters.Count > MaxFilters)
			{
				CustomFilters.RemoveAt(CustomFilters.Count - 1);
			}
		}

		public static void BeginUpdate()
		{
			updateLevel++;
		}

		public static bool DirExists(string directoryName)
			=> CheckDirExists ? Directory.Exists(directoryName) : true;

		public static void EndUpdate()
		{
			updateLevel--;

			if (updateLevel == 0 && changed)
			{
				changed = false;
				OptionsChanged?.Invoke(null, EventArgs.Empty);
			}
		}

		public static bool FileExists(string fileName)
			=> CheckFileExists ? File.Exists(fileName) : true;

		public static string[] GetCustomFilters()
		{
			int numFilters = CustomFilters.Count;
			string[] filters = new string[numFilters];
			for (int i = 0; i < numFilters; i++)
			{
				filters[i] = CustomFilters[i];
			}

			return filters;
		}

		public static void Load(ISettingsNode node)
		{
			showWSInMainDiff = node.GetValue(nameof(ShowWSInMainDiff), false);
			showWSInLineDiff = node.GetValue(nameof(ShowWSInLineDiff), true);
			IgnoreCase = node.GetValue(nameof(IgnoreCase), IgnoreCase);
			IgnoreTextWhitespace = node.GetValue(nameof(IgnoreTextWhitespace), IgnoreTextWhitespace);
			CompareType = node.GetValue(nameof(CompareType), CompareType);
			ShowOnlyInA = node.GetValue(nameof(ShowOnlyInA), ShowOnlyInA);
			ShowOnlyInB = node.GetValue(nameof(ShowOnlyInB), ShowOnlyInB);
			ShowDifferent = node.GetValue(nameof(ShowDifferent), ShowDifferent);
			ShowSame = node.GetValue(nameof(ShowSame), ShowSame);
			Recursive = node.GetValue(nameof(Recursive), Recursive);
			IgnoreDirectoryComparison = node.GetValue(nameof(IgnoreDirectoryComparison), IgnoreDirectoryComparison);
			OnlyShowFileDialogIfShiftPressed = node.GetValue(nameof(OnlyShowFileDialogIfShiftPressed), OnlyShowFileDialogIfShiftPressed);
			OnlyShowDirDialogIfShiftPressed = node.GetValue(nameof(OnlyShowDirDialogIfShiftPressed), OnlyShowDirDialogIfShiftPressed);
			GoToFirstDiff = node.GetValue(nameof(GoToFirstDiff), GoToFirstDiff);
			checkFileExists = node.GetValue(nameof(CheckFileExists), true);
			checkDirExists = node.GetValue(nameof(CheckDirExists), true);
			ShowChangeAsDeleteInsert = node.GetValue(nameof(ShowChangeAsDeleteInsert), ShowChangeAsDeleteInsert);
			showMdiTabs = node.GetValue(nameof(ShowMdiTabs), true);

			hashType = node.GetValue<HashType>(nameof(HashType), HashType.HashCode);
			IgnoreXmlWhitespace = node.GetValue(nameof(IgnoreXmlWhitespace), IgnoreXmlWhitespace);
			LineDiffHeight = node.GetValue(nameof(LineDiffHeight), LineDiffHeight);
			BinaryFootprintLength = node.GetValue(nameof(BinaryFootprintLength), BinaryFootprintLength);

			LastFileA = node.GetValue(nameof(LastFileA), LastFileA);
			LastFileB = node.GetValue(nameof(LastFileB), LastFileB);
			LastDirA = node.GetValue(nameof(LastDirA), LastDirA);
			LastDirB = node.GetValue(nameof(LastDirB), LastDirB);
			/* Note: We don't save or load the last text. */

			// Consolas has been around for 5+ years now, and it renders without misaligned hatch brushes when scrolling.
			string fontName = GetInstalledFontName(node.GetValue("FontName", "Consolas"), "Consolas", "Courier New", FontFamily.GenericMonospace.Name);
			FontStyle fontStyle = node.GetValue<FontStyle>("FontStyle", FontStyle.Regular);
			float fontSize = float.Parse(node.GetValue("FontSize", "9.75"));
			viewFont = new Font(fontName, fontSize, fontStyle, GraphicsUnit.Point);

			// Load custom filters
			CustomFilters.Clear();
			node = node.GetSubNode("Custom Filters", false);
			if (node == null)
			{
				// It appears to be the first time the program has run,
				// so add in some default filters.
				CustomFilters.Add("*.cs");
				CustomFilters.Add("*.cpp;*.h;*.idl;*.rc;*.c;*.inl");
				CustomFilters.Add("*.vb");
				CustomFilters.Add("*.xml");
				CustomFilters.Add("*.htm;*.html");
				CustomFilters.Add("*.txt");
				CustomFilters.Add("*.sql");
				CustomFilters.Add("*.obj;*.pdb;*.exe;*.dll;*.cache;*.tlog;*.trx;*.FileListAbsolute.txt");
			}
			else
			{
				IList<string> names = node.GetSettingNames();
				for (int i = 0; i < names.Count; i++)
				{
					CustomFilters.Add(node.GetValue(names[i], string.Empty));
				}
			}
		}

		public static void Save(ISettingsNode node)
		{
			node.SetValue(nameof(CompareType), CompareType);

			node.SetValue(nameof(ShowWSInMainDiff), showWSInMainDiff);
			node.SetValue(nameof(ShowWSInLineDiff), showWSInLineDiff);
			node.SetValue(nameof(IgnoreCase), IgnoreCase);
			node.SetValue(nameof(IgnoreTextWhitespace), IgnoreTextWhitespace);
			node.SetValue(nameof(ShowOnlyInA), ShowOnlyInA);
			node.SetValue(nameof(ShowOnlyInB), ShowOnlyInB);
			node.SetValue(nameof(ShowDifferent), ShowDifferent);
			node.SetValue(nameof(ShowSame), ShowSame);
			node.SetValue(nameof(Recursive), Recursive);
			node.SetValue(nameof(IgnoreDirectoryComparison), IgnoreDirectoryComparison);
			node.SetValue(nameof(OnlyShowFileDialogIfShiftPressed), OnlyShowFileDialogIfShiftPressed);
			node.SetValue(nameof(OnlyShowDirDialogIfShiftPressed), OnlyShowDirDialogIfShiftPressed);
			node.SetValue(nameof(GoToFirstDiff), GoToFirstDiff);
			node.SetValue(nameof(CheckFileExists), checkFileExists);
			node.SetValue(nameof(CheckDirExists), checkDirExists);
			node.SetValue(nameof(ShowChangeAsDeleteInsert), ShowChangeAsDeleteInsert);
			node.SetValue(nameof(IgnoreXmlWhitespace), IgnoreXmlWhitespace);
			node.SetValue(nameof(ShowMdiTabs), showMdiTabs);

			node.SetValue(nameof(HashType), hashType);
			node.SetValue(nameof(LineDiffHeight), LineDiffHeight);
			node.SetValue(nameof(BinaryFootprintLength), BinaryFootprintLength);

			node.SetValue(nameof(LastFileA), LastFileA);
			node.SetValue(nameof(LastFileB), LastFileB);
			node.SetValue(nameof(LastDirA), LastDirA);
			node.SetValue(nameof(LastDirB), LastDirB);
			/* Note: We don't save or load the last text. */

			node.SetValue("FontName", viewFont.Name);
			node.SetValue("FontStyle", viewFont.Style);
			node.SetValue("FontSize", Convert.ToString(viewFont.SizeInPoints));

			// Save custom filters
			if (node.GetSubNode("Custom Filters", false) != null)
			{
				node.DeleteSubNode("Custom Filters");
			}

			if (CustomFilters.Count > 0)
			{
				node = node.GetSubNode("Custom Filters", true);
				for (int i = 0; i < CustomFilters.Count; i++)
				{
					node.SetValue(i.ToString(), CustomFilters[i]);
				}
			}
		}

		#endregion

		#region Private Methods

		private static string GetInstalledFontName(params string[] fontNames)
		{
			string result = null;

			foreach (string fontName in fontNames)
			{
				// Set result here, so if none of the fonts are installed,
				// then we'll at least return the last name passed in.
				result = fontName;

				// http://stackoverflow.com/questions/113989/test-if-a-font-is-installed
				using (Font font = new Font(fontName, DefaultFontSize))
				{
					if (font.Name == fontName)
					{
						break;
					}
				}
			}

			return result;
		}

		private static void SetValue<T>(ref T member, T value)
		{
			if (!object.Equals(member, value))
			{
				BeginUpdate();
				member = value;
				changed = true;
				EndUpdate();
			}
		}

		#endregion
	}
}
