using NAudio.Wave;
using OpenLocoTool.DatFileParsing;
using OpenLocoTool.Headers;
using OpenLocoTool.Objects;
using OpenLocoToolCommon;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenLocoToolGui
{
	public partial class MainForm : Form
	{
		private ILogger logger;
		private SawyerStreamReader reader;
		private SawyerStreamWriter writer;
		private const string IndexFilename = "ObjectIndex.json";

		public MainForm()
		{
			InitializeComponent();

			logger = new Logger
			{
				Level = LogLevel.Debug2
			};
			((Logger)logger).LogAdded += (s, e) => lbLogs.Items.Insert(0, e.Log.ToString());

			reader = new SawyerStreamReader(logger);
			writer = new SawyerStreamWriter(logger);
		}

		Dictionary<string, ObjectHeader> headerIndex = new(); // key is full path/filename
		Dictionary<string, ILocoObject> objectCache = new(); // key is full path/filename
		string currentDir;

		private void MainForm_Load(object sender, EventArgs e)
		{
			//Init(BaseDirectory);

		}

		void Init(string directory)
		{
			currentDir = directory;
			InitialiseIndex();
			// SerialiseHeaderIndexToFile(); // optional - index creation is so fast it's not really necessary to cache this
			InitFileTreeView();
			InitCategoryTreeView();
		}

		void InitialiseIndex()
		{
			if (!Directory.Exists(currentDir))
			{
				btnSetDirectory.PerformClick();
				return;
			}

			var allFiles = Directory.GetFiles(currentDir, "*.dat", SearchOption.AllDirectories);
			headerIndex.Clear();
			foreach (var file in allFiles)
			{
				var objectHeader = reader.LoadHeader(file);
				if (!headerIndex.TryAdd(file, objectHeader))
				{
					logger.Warning($"Didn't add file {file} - already exists (how???)");
				}
			}
		}

		void SerialiseHeaderIndexToFile()
		{
			var json = JsonSerializer.Serialize(headerIndex, new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() }, });
			File.WriteAllText(Path.Combine(currentDir, IndexFilename), json);
		}

		void InitFileTreeView(string fileFilter = "")
		{
			tvFileTree.SuspendLayout();
			tvFileTree.Nodes.Clear();
			var filteredFiles = headerIndex.Where(hdr => hdr.Key.Contains(fileFilter, StringComparison.InvariantCultureIgnoreCase));

			foreach (var obj in filteredFiles)
			{
				var relative = Path.GetRelativePath(currentDir, obj.Key);
				tvFileTree.Nodes.Add(obj.Key, relative);
			}

			tvFileTree.ResumeLayout(true);
		}

		void InitCategoryTreeView(string fileFilter = "")
		{
			tvObjType.SuspendLayout();
			tvObjType.Nodes.Clear();
			var filteredFiles = headerIndex.Where(hdr => hdr.Key.Contains(fileFilter, StringComparison.InvariantCultureIgnoreCase));

			var nodesToAdd = new List<TreeNode>();
			foreach (var group in filteredFiles.GroupBy(kvp => kvp.Value.ObjectType))
			{
				var typeNode = new TreeNode(group.Key.ToString());
				foreach (var obj in group)
				{
					typeNode.Nodes.Add(obj.Key, obj.Value.Name);
				}

				nodesToAdd.Add(typeNode);
			}

			nodesToAdd.Sort((a, b) => a.Text.CompareTo(b.Text));
			tvObjType.Nodes.AddRange(nodesToAdd.ToArray());

			tvObjType.ResumeLayout(true);
		}

		// note: doesn't work atm
		private void btnSaveChanges_Click(object sender, EventArgs e)
		{
			var obj = (ILocoObject)pgObject.SelectedObject;
			saveFileDialog1.InitialDirectory = currentDir;
			saveFileDialog1.DefaultExt = "dat";
			saveFileDialog1.Filter = "Locomotion DAT files (.dat)|*.dat";
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				var filename = saveFileDialog1.FileName;

				try
				{
					//writer.Save(path, obj);
					MessageBox.Show($"File \"{filename}\" saved successfully");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error saving \"{filename}\": " + ex.Message);
				}
			}
		}

		private void btnSetDirectory_Click(object sender, EventArgs e)
		{
			if (objectDirBrowser.ShowDialog(this) == DialogResult.OK)
			{
				Init(objectDirBrowser.SelectedPath);
			}
		}

		private void tbFileFilter_TextChanged(object sender, EventArgs e)
		{
			InitFileTreeView();
			InitCategoryTreeView();
		}

		private void tv_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node == null)
			{
				return;
			}

			var obj = LoadAndCacheObject(e.Node.Name);

			if (obj != null && obj.G1Elements != null && obj.G1Header != null && obj.G1Header.TotalSize != 0 && obj.G1Elements.Count != 0)
			{
				CreateImages(obj);
			}

			if (obj != null && obj.Object is SoundObject soundObject)
			{
				CreateSounds(soundObject);
			}

			pgObject.SelectedObject = obj;
		}

		private void CreateSounds(SoundObject soundObject)
		{
			flpImageTable.SuspendLayout();
			flpImageTable.Controls.Clear();

			var pcmHeader = soundObject.SoundObjectData.PcmHeader;

			var soundButton = new Button
			{
				Size = new Size(100, 100),
				Text = "Play sound",
			};

			soundButton.Click += (args, sender) =>
			{
				Task.Run(() =>
				{
					using (var ms = new MemoryStream(soundObject.RawPcmData))
					using (var rs = new RawSourceWaveStream(ms, new WaveFormat(pcmHeader.SamplesPerSecond, 16, pcmHeader.NumberChannels)))
					using (var wo = new WaveOutEvent())
					{
						wo.Init(rs);
						wo.Play();
						while (wo.PlaybackState == PlaybackState.Playing)
						{
							Thread.Sleep(50);
						}
					}
				});
			};

			flpImageTable.Controls.Add(soundButton);

			flpImageTable.ResumeLayout(true);
		}

		private void CreateImages(ILocoObject obj)
		{
			flpImageTable.SuspendLayout();
			flpImageTable.Controls.Clear();

			// todo: add user to supply this file
			const string path = "../../../../palette.png";
			var paletteBitmap = new Bitmap(path);
			var palette = PaletteFromBitmap(paletteBitmap);

			for (var i = 0; i < obj.G1Elements.Count; ++i)
			{
				var currElement = obj.G1Elements[i];
				var imageData = currElement.ImageData;

				if (currElement.ImageData.Length == 0 || currElement.flags.HasFlag(G1ElementFlags.IsR8G8B8Palette))
				{
					logger.Info($"skipped loading g1 element {i} with flags {currElement.flags}");
					continue;
				}

				var dstImg = new Bitmap(currElement.width, currElement.height);
				var rect = new Rectangle(0, 0, currElement.width, currElement.height);
				var dstImgData = dstImg.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				for (var y = 0; y < currElement.height; ++y)
				{
					for (var x = 0; x < currElement.width; ++x)
					{
						var paletteIndex = imageData[(y * currElement.width) + x];

						// the issue with greyscale here is it isn't normalised so all heightmaps are really dark and hard to see
						//var colour = obj.Object is HillShapesObject
						//	? Color.FromArgb(paletteIndex, paletteIndex, paletteIndex) // for hillshapes, its just a heightmap so lets put it in greyscale
						//	: palette[paletteIndex];

						var colour = palette[paletteIndex];
						SetPixel(dstImgData, x, y, colour);
					}
				}

				dstImg.UnlockBits(dstImgData);

				var pb = new PictureBox
				{
					Image = dstImg,
					BorderStyle = BorderStyle.FixedSingle,
					SizeMode = PictureBoxSizeMode.AutoSize,
				};
				flpImageTable.Controls.Add(pb);
			}

			flpImageTable.ResumeLayout(true);
		}

		ILocoObject? LoadAndCacheObject(string filename)
		{
			if (!objectCache.ContainsKey(filename) && !string.IsNullOrEmpty(filename) && filename.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase))
			{
				objectCache.Add(filename, reader.LoadFull(filename));
			}

			if (objectCache.ContainsKey(filename))
			{
				return objectCache[filename];
			}

			return null;
		}

		public Color[] PaletteFromBitmap(Bitmap img)
		{
			var palette = new Color[256];
			var rect = new Rectangle(0, 0, img.Width, img.Height);
			var imgData = img.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			for (var y = 0; y < 16; ++y)
			{
				for (var x = 0; x < 16; ++x)
				{
					var pixel = GetPixel(imgData, x * 36, y * 11);
					palette[(y * 16) + x] = pixel;
				}
			}

			img.UnlockBits(imgData);
			return palette;
		}

		public unsafe Color GetPixel(BitmapData d, int X, int Y)
		{
			var ptr = GetPtrToFirstPixel(d, X, Y);
			return Color.FromArgb(ptr[2], ptr[1], ptr[0]); // alpha is ptr[3]);
		}

		public unsafe void SetPixel(BitmapData d, Point p, Color c)
			=> SetPixel(d, p.X, p.Y, c);

		public unsafe void SetPixel(BitmapData d, int X, int Y, Color c)
			=> SetPixel(GetPtrToFirstPixel(d, X, Y), c);

		private static unsafe byte* GetPtrToFirstPixel(BitmapData d, int X, int Y)
			=> (byte*)d.Scan0.ToPointer() + (Y * d.Stride) + (X * (Image.GetPixelFormatSize(d.PixelFormat) / 8));

		//private static unsafe void SetPixel(byte* ptr, Color c)
		//{
		//	ptr[0] = c.B; // Blue
		//	ptr[1] = c.G; // Green
		//	ptr[2] = c.R; // Red
		//	ptr[3] = c.A; // Alpha
		//}

		private static unsafe void SetPixel(byte* ptr, Color c)
		{
			ptr[0] = (byte)(c.B); // Blue
			ptr[1] = (byte)(c.G); // Green
			ptr[2] = (byte)(c.R); // Red
			ptr[3] = 255; // (byte)(c.A * 255); // Alpha
		}
	}
}