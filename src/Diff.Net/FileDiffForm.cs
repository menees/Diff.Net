namespace Diff.Net
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.IO;
	using System.Text;
	using System.Windows.Forms;
	using System.Xml;
	using Menees;
	using Menees.Diffs;
	using Menees.Diffs.Windows.Forms;
	using Menees.Windows.Forms;

	#endregion

	internal sealed partial class FileDiffForm : ExtendedForm, IDifferenceForm
	{
		#region Private Data Members

		private ShowDiffArgs? currentDiffArgs;

		#endregion

		#region Constructors

		public FileDiffForm()
		{
			this.InitializeComponent();

			Options.OptionsChanged += this.OptionsChanged;
		}

		#endregion

		#region Public Properties

		public string? ToolTipText
		{
			get
			{
				string? result = null;

				if (this.currentDiffArgs != null)
				{
					result = this.currentDiffArgs.A + Environment.NewLine + this.currentDiffArgs.B;
				}

				return result;
			}
		}

		#endregion

		#region Public Methods

		public void ShowDifferences(ShowDiffArgs e)
		{
			string textA = e.A;
			string textB = e.B;
			DiffType diffType = e.DiffType;

			IList<string>? a, b;
			int leadingCharactersToIgnore = 0;
			bool fileNames = diffType == DiffType.File;
			if (fileNames)
			{
				GetFileLines(textA, textB, out a, out b, out leadingCharactersToIgnore);
			}
			else
			{
				GetTextLines(textA, textB, out a, out b);
			}

			bool isBinaryCompare = leadingCharactersToIgnore > 0;
			bool ignoreCase = !isBinaryCompare && Options.IgnoreCase;
			bool ignoreTextWhitespace = !isBinaryCompare && Options.IgnoreTextWhitespace;
			TextDiff diff = new(Options.HashType, ignoreCase, ignoreTextWhitespace, leadingCharactersToIgnore, !Options.ShowChangeAsDeleteInsert);
			EditScript script = diff.Execute(a, b);

			string captionA = string.Empty;
			string captionB = string.Empty;
			if (fileNames)
			{
				captionA = textA;
				captionB = textB;
				this.Text = string.Format("{0} : {1}", Path.GetFileName(textA), Path.GetFileName(textB));
			}
			else
			{
				this.Text = "Text Comparison";
			}

			// Apply options first since SetData needs to know things
			// like SpacesPerTab and ShowWhitespace up front, so it
			// can build display lines, determine scroll bounds, etc.
			this.ApplyOptions();

			this.DiffCtrl.SetData(a, b, script, captionA, captionB, ignoreCase, ignoreTextWhitespace, isBinaryCompare);

			if (Options.LineDiffHeight != 0)
			{
				this.DiffCtrl.LineDiffHeight = Options.LineDiffHeight;
			}

			this.Show();

			this.currentDiffArgs = e;
		}

		public void UpdateUI()
		{
			this.mnuViewFile.Enabled = this.DiffCtrl.CanViewFile;
			this.mnuCopy.Enabled = this.DiffCtrl.CanCopy;
			this.mnuCompareSelectedText.Enabled = this.DiffCtrl.CanCompareSelectedText;
			this.mnuFind.Enabled = this.DiffCtrl.CanFind;
			this.mnuFindNext.Enabled = this.DiffCtrl.CanFindNext;
			this.mnuFindPrevious.Enabled = this.DiffCtrl.CanFindPrevious;

			this.mnuGoToFirstDiff.Enabled = this.DiffCtrl.CanGoToFirstDiff;
			this.mnuGoToNextDiff.Enabled = this.DiffCtrl.CanGoToNextDiff;
			this.mnuGoToPreviousDiff.Enabled = this.DiffCtrl.CanGoToPreviousDiff;
			this.mnuGoToLastDiff.Enabled = this.DiffCtrl.CanGoToLastDiff;

			this.mnuGoToLine.Enabled = this.DiffCtrl.CanGoToLine;
			this.mnuRecompare.Enabled = this.DiffCtrl.CanRecompare;
		}

		#endregion

		#region Private Methods

		private static void GetFileLines(string fileNameA, string fileNameB, out IList<string> a, out IList<string> b, out int leadingCharactersToIgnore)
		{
			IList<string>? aLines = null;
			IList<string>? bLines = null;
			leadingCharactersToIgnore = 0;
			CompareType compareType = Options.CompareType;
			bool isAuto = compareType == CompareType.Auto;

			if (compareType == CompareType.Binary ||
				(isAuto && (DiffUtility.IsBinaryFile(fileNameA) || DiffUtility.IsBinaryFile(fileNameB))))
			{
				using (FileStream fileA = File.OpenRead(fileNameA))
				using (FileStream fileB = File.OpenRead(fileNameB))
				{
					BinaryDiff diff = new()
					{
						FootprintLength = Options.BinaryFootprintLength,
					};
					AddCopyCollection addCopy = diff.Execute(fileA, fileB);

					BinaryDiffLines lines = new(fileA, addCopy, Options.BinaryFootprintLength);
					aLines = lines.BaseLines;
					bLines = lines.VersionLines;
					leadingCharactersToIgnore = BinaryDiffLines.PrefixLength;
				}
			}

			if (compareType == CompareType.Xml || (isAuto && (aLines == null || bLines == null)))
			{
				aLines = TryGetXmlLines(DiffUtility.GetXmlTextLines, fileNameA, fileNameA, !isAuto);

				// If A failed to parse with Auto, then there's no reason to try B.
				if (aLines != null)
				{
					bLines = TryGetXmlLines(DiffUtility.GetXmlTextLines, fileNameB, fileNameB, !isAuto);
				}

				// If we get here and the compare type was XML, then both
				// inputs parsed correctly, and both lists should be non-null.
				// If we get here and the compare type was Auto, then one
				// or both lists may be null, so we'll fallthrough to the text
				// handling logic.
			}

			if (aLines == null || bLines == null)
			{
				aLines = DiffUtility.GetFileTextLines(fileNameA);
				bLines = DiffUtility.GetFileTextLines(fileNameB);
			}

			a = aLines;
			b = bLines;
		}

		private static void GetTextLines(string textA, string textB, out IList<string> a, out IList<string> b)
		{
			IList<string>? aLines = null;
			IList<string>? bLines = null;
			CompareType compareType = Options.CompareType;
			bool isAuto = compareType == CompareType.Auto;

			if (compareType == CompareType.Xml || isAuto)
			{
				aLines = TryGetXmlLines(DiffUtility.GetXmlTextLinesFromXml, "the left side text", textA, !isAuto);

				// If A failed to parse with Auto, then there's no reason to try B.
				if (aLines != null)
				{
					bLines = TryGetXmlLines(DiffUtility.GetXmlTextLinesFromXml, "the right side text", textB, !isAuto);
				}

				// If we get here and the compare type was XML, then both
				// inputs parsed correctly, and both lists should be non-null.
				// If we get here and the compare type was Auto, then one
				// or both lists may be null, so we'll fallthrough to the text
				// handling logic.
			}

			if (aLines == null || bLines == null)
			{
				aLines = DiffUtility.GetStringTextLines(textA);
				bLines = DiffUtility.GetStringTextLines(textB);
			}

			a = aLines;
			b = bLines;
		}

		private static IList<string>? TryGetXmlLines(
			Func<string, bool, IList<string>> converter,
			string name,
			string input,
			bool throwOnError)
		{
			IList<string>? result = null;
			try
			{
				result = converter(input, Options.IgnoreXmlWhitespace);
			}
			catch (XmlException ex)
			{
				if (throwOnError)
				{
					StringBuilder sb = new("An XML comparison was attempted, but an XML exception occurred while parsing ");
					sb.Append(name).AppendLine(".").AppendLine();
					sb.AppendLine("Exception Message:").Append(ex.Message);
					throw new XmlException(sb.ToString(), ex);
				}
			}

			return result;
		}

		private void ApplyOptions()
		{
			this.DiffCtrl.ShowWhiteSpaceInMainDiff = Options.ShowWSInMainDiff;
			this.DiffCtrl.ShowWhiteSpaceInLineDiff = Options.ShowWSInLineDiff;
			this.DiffCtrl.ViewFont = Options.ViewFont;
		}

		private void DiffCtrl_LineDiffSizeChanged(object? sender, EventArgs e)
		{
			if (this.Visible)
			{
				Options.LineDiffHeight = this.DiffCtrl.LineDiffHeight;
			}
		}

		private void DiffCtrl_RecompareNeeded(object? sender, EventArgs e)
		{
			if (this.currentDiffArgs != null)
			{
				using (WaitCursor wc = new(this))
				{
					this.ShowDifferences(this.currentDiffArgs);
					this.GoToFirstDiff();
				}
			}
		}

		private void DiffCtrl_ShowTextDifferences(object sender, DifferenceEventArgs e)
		{
			MainForm frmMain = (MainForm)this.MdiParent;
			frmMain.ShowTextDifferences(e.ItemA, e.ItemB);
		}

		private void FileDiffForm_Closed(object? sender, EventArgs e)
		{
			Options.OptionsChanged -= this.OptionsChanged;
		}

		private void FileDiffForm_Load(object? sender, EventArgs e)
		{
			// http://stackoverflow.com/questions/888865/problem-with-icon-on-creating-new-maximized-mdi-child-form-in-net
			this.Icon = (Icon)this.Icon.Clone();
		}

		private void FileDiffForm_Shown(object? sender, EventArgs e)
		{
			this.GoToFirstDiff();
		}

		private void GoToFirstDiff()
		{
			if (Options.GoToFirstDiff)
			{
				this.DiffCtrl.GoToFirstDiff();
			}
		}

		private void MainMenu_ItemRemoved(object sender, ToolStripItemEventArgs e)
		{
			// This is needed in .NET 2.0 to make the empty, merged menustrip disappear.
			this.MainMenu.Visible = this.MainMenu.Items.Count > 0;
		}

		private void CompareSelectedText_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.CompareSelectedText();
		}

		private void Copy_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.Copy();
		}

		private void Find_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.Find();
		}

		private void FindNext_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.FindNext();
		}

		private void FindPrevious_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.FindPrevious();
		}

		private void GoToFirstDiff_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.GoToFirstDiff();
		}

		private void GoToLastDiff_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.GoToLastDiff();
		}

		private void GoToLine_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.GoToLine();
		}

		private void GoToNextDiff_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.GoToNextDiff();
		}

		private void GoToPreviousDiff_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.GoToPreviousDiff();
		}

		private void Recompare_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.Recompare();
		}

		private void ViewFile_Click(object? sender, EventArgs e)
		{
			this.DiffCtrl.ViewFile();
		}

		private void OptionsChanged(object? sender, EventArgs e)
		{
			this.ApplyOptions();
		}

		#endregion
	}
}
