using System;
using System.Deployment.Application;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using Denso.Properties;
using M32RR_FLASH_TOOL.Properties;
using Microsoft.Win32;
//using Settings = Denso.Properties.Settings;
using Settings = M32RR_FLASH_TOOL.Properties.Settings;

namespace M32RR_FLASH_TOOL
{

	public partial class MainWindow : Window, IComponentConnector
	{
		private M32RRTools _m32RRTools = new M32RRTools();

		private ChecksumDenso16 checksumDenso16 = new ChecksumDenso16();

		private Thread _threadM32RRWrite;

		private Thread _threadM32RRDump;

		private Thread _threadM32RRConnect;

		private Thread _threadM32RRErase;

		private Thread _threadCancel;

		private volatile bool _stopThread;

		private string _filePathDump = "";

		private string _filePathWrite = "";

		private byte[] _customPass = new byte[12];

		private long _fileSize;

		private long _checksumAddress;

		private string fileDensoID = "";

		private string fileChecksum = "";

		public MainWindow()
		{
			InitializeComponent();
			base.DataContext = _m32RRTools;
			base.Title = "EGEA ENGINEERING - DENSO M32R FLASH TOOL v" + getRunningVersion();
			_threadM32RRConnect = new Thread(M32RRPass);
			_threadM32RRDump = new Thread(M32RR_Dump);
			_threadM32RRErase = new Thread(M32RR_Erase);
			_threadM32RRWrite = new Thread(M32RR_Write);
			enableRadioBtnPassConnect();
			disableCustomPass();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte1.Text = Settings.Default.paramtextBoxPassByte1.ToString();
			textBox_Pass_byte2.Text = Settings.Default.paramtextBoxPassByte2.ToString();
			textBox_Pass_byte3.Text = Settings.Default.paramtextBoxPassByte3.ToString();
			textBox_Pass_byte4.Text = Settings.Default.paramtextBoxPassByte4.ToString();
			textBox_Pass_byte5.Text = Settings.Default.paramtextBoxPassByte5.ToString();
			textBox_Pass_byte6.Text = Settings.Default.paramtextBoxPassByte6.ToString();
			textBox_Pass_byte7.Text = Settings.Default.paramtextBoxPassByte7.ToString();
			textBox_Pass_byte8.Text = Settings.Default.paramtextBoxPassByte8.ToString();
			textBox_Pass_byte9.Text = Settings.Default.paramtextBoxPassByte9.ToString();
			textBox_Pass_byte10.Text = Settings.Default.paramtextBoxPassByte10.ToString();
			textBox_Pass_byte11.Text = Settings.Default.paramtextBoxPassByte11.ToString();
			textBox_Pass_byte12.Text = Settings.Default.paramtextBoxPassByte12.ToString();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Settings.Default.paramtextBoxPassByte1 = textBox_Pass_byte1.Text.ToString();
			Settings.Default.paramtextBoxPassByte2 = textBox_Pass_byte2.Text.ToString();
			Settings.Default.paramtextBoxPassByte3 = textBox_Pass_byte3.Text.ToString();
			Settings.Default.paramtextBoxPassByte4 = textBox_Pass_byte4.Text.ToString();
			Settings.Default.paramtextBoxPassByte5 = textBox_Pass_byte5.Text.ToString();
			Settings.Default.paramtextBoxPassByte6 = textBox_Pass_byte6.Text.ToString();
			Settings.Default.paramtextBoxPassByte7 = textBox_Pass_byte7.Text.ToString();
			Settings.Default.paramtextBoxPassByte8 = textBox_Pass_byte8.Text.ToString();
			Settings.Default.paramtextBoxPassByte9 = textBox_Pass_byte9.Text.ToString();
			Settings.Default.paramtextBoxPassByte10 = textBox_Pass_byte10.Text.ToString();
			Settings.Default.paramtextBoxPassByte11 = textBox_Pass_byte11.Text.ToString();
			Settings.Default.paramtextBoxPassByte12 = textBox_Pass_byte12.Text.ToString();
			Settings.Default.Save();
		}

		public void checkThread()
		{
			if (!_threadM32RRConnect.IsAlive)
			{
				_threadM32RRConnect = new Thread(M32RRPass);
				_threadM32RRConnect.IsBackground = true;
				_threadM32RRConnect.Start();
			}
			if (!_threadM32RRErase.IsAlive)
			{
				_threadM32RRErase = new Thread(M32RR_Erase);
				_threadM32RRErase.IsBackground = true;
				_threadM32RRErase.Start();
			}
			if (!_threadM32RRDump.IsAlive)
			{
				_threadM32RRDump = new Thread(M32RR_Dump);
				_threadM32RRDump.IsBackground = true;
				_threadM32RRDump.Start();
			}
			if (!_threadM32RRWrite.IsAlive)
			{
				_threadM32RRWrite = new Thread(M32RR_Write);
				_threadM32RRWrite.IsBackground = true;
				_threadM32RRWrite.Start();
			}
		}

		private void button_Connect_Click(object sender, RoutedEventArgs e)
		{
			disableRadioBtnPassConnect();
			disableCustomPass();
			bool? isChecked = radioBtn_Susuki_Pass.IsChecked;
			bool flag = true;
			if (isChecked.GetValueOrDefault() == flag && isChecked.HasValue && !_threadM32RRConnect.IsAlive)
			{
				_threadM32RRConnect = new Thread(SuzukiPass);
				_threadM32RRConnect.IsBackground = true;
				_threadM32RRConnect.Start();
			}
			isChecked = radioBtn_Kawasaki_Pass.IsChecked;
			flag = true;
			if (isChecked.GetValueOrDefault() == flag && isChecked.HasValue && !_threadM32RRConnect.IsAlive)
			{
				_threadM32RRConnect = new Thread(KawasakiPass);
				_threadM32RRConnect.IsBackground = true;
				_threadM32RRConnect.Start();
			}
			isChecked = radioBtn_Pass_List.IsChecked;
			flag = true;
			if (isChecked.GetValueOrDefault() == flag && isChecked.HasValue && !_threadM32RRConnect.IsAlive)
			{
				_threadM32RRConnect = new Thread(M32RRPass);
				_threadM32RRConnect.IsBackground = true;
				_threadM32RRConnect.Start();
			}
			isChecked = radioBtn_Pass_Custom.IsChecked;
			flag = true;
			if (isChecked.GetValueOrDefault() == flag && isChecked.HasValue && !_threadM32RRConnect.IsAlive)
			{
				_threadM32RRConnect = new Thread(CustomPass);
				_threadM32RRConnect.IsBackground = true;
				_threadM32RRConnect.Start();
			}
		}

		private void button_Disconnect_Click(object sender, RoutedEventArgs e)
		{
			_m32RRTools.FtdiClose();
			enableRadioBtnPassConnect();
			if (radioBtn_Pass_Custom.IsChecked == true)
			{
				enableCustomPass();
			}
			textBlock_ECU_Info1.Text = "";
			groupBox_ECUTool.IsEnabled = false;
		}

		public void SuzukiPass()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Disconnect.IsEnabled = false;
				button_LoadFile.IsEnabled = false;
			});
			if (_m32RRTools.FtdiConnect(38400u))
			{
				ScrollBottom();
				if (_m32RRTools.M32RR_RequestConnect())
				{
					ScrollBottom();
					if (_m32RRTools.SuzukiPass())
					{
						ScrollBottom();
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							button_Disconnect.IsEnabled = true;
							button_LoadFile.IsEnabled = true;
							groupBox_ECUTool.IsEnabled = true;
						});
						M32RR_isBlank();
					}
					else
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							enableRadioBtnPassConnect();
						});
					}
				}
				else
				{
					_m32RRTools.FtdiClose();
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						enableRadioBtnPassConnect();
					});
				}
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					enableRadioBtnPassConnect();
				});
			}
		}

		public void KawasakiPass()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Disconnect.IsEnabled = false;
				button_LoadFile.IsEnabled = false;
			});
			if (_m32RRTools.FtdiConnect(38400u))
			{
				ScrollBottom();
				if (_m32RRTools.M32RR_RequestConnect())
				{
					ScrollBottom();
					if (_m32RRTools.KawasakiPass())
					{
						ScrollBottom();
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							button_Disconnect.IsEnabled = true;
							button_LoadFile.IsEnabled = true;
							groupBox_ECUTool.IsEnabled = true;
						});
						M32RR_isBlank();
					}
					else
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							enableRadioBtnPassConnect();
						});
					}
				}
				else
				{
					_m32RRTools.FtdiClose();
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						enableRadioBtnPassConnect();
					});
				}
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					enableRadioBtnPassConnect();
				});
			}
		}

		public void M32RRPass()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Disconnect.IsEnabled = false;
				button_LoadFile.IsEnabled = false;
			});
			if (_m32RRTools.FtdiConnect(38400u))
			{
				ScrollBottom();
				if (_m32RRTools.M32RR_RequestConnect())
				{
					ScrollBottom();
					if (_m32RRTools.M32RR_HackPassList("password.txt"))
					{
						ScrollBottom();
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							button_Disconnect.IsEnabled = true;
							button_LoadFile.IsEnabled = true;
							groupBox_ECUTool.IsEnabled = true;
						});
						M32RR_isBlank();
					}
					else
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							enableRadioBtnPassConnect();
						});
					}
				}
				else
				{
					_m32RRTools.FtdiClose();
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						enableRadioBtnPassConnect();
					});
				}
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					enableRadioBtnPassConnect();
				});
			}
		}

		public void CustomPass()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Disconnect.IsEnabled = false;
				button_LoadFile.IsEnabled = false;
			});
			if (_m32RRTools.FtdiConnect(38400u))
			{
				ScrollBottom();
				if (_m32RRTools.M32RR_RequestConnect())
				{
					ScrollBottom();
					if (_m32RRTools.CustomPass(_customPass))
					{
						ScrollBottom();
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							button_Disconnect.IsEnabled = true;
							button_LoadFile.IsEnabled = true;
							groupBox_ECUTool.IsEnabled = true;
						});
						M32RR_isBlank();
					}
					else
					{
						base.Dispatcher.BeginInvoke((Action)delegate
						{
							enableRadioBtnPassConnect();
							enableCustomPass();
						});
					}
				}
				else
				{
					_m32RRTools.FtdiClose();
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						enableRadioBtnPassConnect();
						enableCustomPass();
					});
				}
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					enableRadioBtnPassConnect();
				});
			}
		}

		private void button_Erase_Click(object sender, RoutedEventArgs e)
		{
			if (System.Windows.Forms.MessageBox.Show("YOU WILL ERASE THE ECU, ARE YOU SURE ???", "WARNING", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes && !_threadM32RRErase.IsAlive)
			{
				_threadM32RRErase = new Thread(M32RR_Erase);
				_threadM32RRErase.IsBackground = true;
				_threadM32RRErase.Start();
			}
		}

		public void M32RR_Erase()
		{
			startProcessing();
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Cancel.IsEnabled = false;
			});
			_m32RRTools.M32RR_Erase();
			M32RR_isBlank();
		}

		public void startProcessing()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				button_Disconnect.IsEnabled = false;
				button_Read.IsEnabled = false;
				button_Write.IsEnabled = false;
				button_Erase.IsEnabled = false;
				button_LoadFile.IsEnabled = false;
				button_Cancel.IsEnabled = true;
			});
		}

		public void ScrollBottom()
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				scrollViewer_Log.ScrollToBottom();
			});
		}

		public void M32RR_isBlank()
		{
			if (_m32RRTools._isBlank)
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					button_Disconnect.IsEnabled = true;
					button_LoadFile.IsEnabled = true;
					button_Erase.IsEnabled = true;
					button_Read.IsEnabled = false;
					button_Cancel.IsEnabled = false;
					textBlock_ECU_Info1.Text = "ECU is blank !";
					if (_filePathWrite != "" || _filePathWrite != null)
					{
						if (_fileSize == 1024 || _fileSize == 512)
						{
							button_Write.IsEnabled = true;
						}
						else
						{
							button_Write.IsEnabled = false;
						}
					}
					scrollViewer_Log.ScrollToBottom();
				});
			}
			else
			{
				_m32RRTools.checkSwSerial();
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					button_Disconnect.IsEnabled = true;
					button_LoadFile.IsEnabled = true;
					button_Erase.IsEnabled = true;
					button_Read.IsEnabled = true;
					button_Write.IsEnabled = false;
					button_Cancel.IsEnabled = false;
					textBlock_ECU_Info1.Text = "Checksum: " + BitConverter.ToString(_m32RRTools._byteSwSerial).Replace("-", "").Remove(0, 16) + " " + BitConverter.ToString(_m32RRTools._byteEcuChecksum).Replace("-", "").Remove(4, 4);
					scrollViewer_Log.ScrollToBottom();
				});
			}
		}

		public void enableRadioBtnPassConnect()
		{
			button_Connect.IsEnabled = true;
			button_Disconnect.IsEnabled = false;
			button_LoadFile.IsEnabled = false;
			radioBtn_Susuki_Pass.IsEnabled = true;
			radioBtn_Kawasaki_Pass.IsEnabled = true;
			radioBtn_Pass_List.IsEnabled = true;
			radioBtn_Pass_Custom.IsEnabled = true;
		}

		public void disableRadioBtnPassConnect()
		{
			button_Connect.IsEnabled = false;
			button_Disconnect.IsEnabled = true;
			button_LoadFile.IsEnabled = true;
			radioBtn_Susuki_Pass.IsEnabled = false;
			radioBtn_Kawasaki_Pass.IsEnabled = false;
			radioBtn_Pass_List.IsEnabled = false;
			radioBtn_Pass_Custom.IsEnabled = false;
		}

		public void M32RR_Dump()
		{
			startProcessing();
			_m32RRTools.M32RR_Dump(0L, _filePathDump);
			M32RR_isBlank();
		}

		public void M32RR_Write()
		{
			startProcessing();
			_m32RRTools.M32RR_Write(0L, _filePathWrite);
			M32RR_isBlank();
		}

		public void UpdateChecksum()
		{
			new Thread(calculateChecksum).Start();
		}

		private void button_Read_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			saveFileDialog.Filter = "BIN Files (.bin)|*.bin|All Files (*.*)|*.*";
			saveFileDialog.Title = "Save bin file";
			saveFileDialog.InitialDirectory = "";
			if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				_filePathDump = saveFileDialog.FileName;
				if (!_threadM32RRDump.IsAlive)
				{
					_threadM32RRDump = new Thread(M32RR_Dump);
					_threadM32RRDump.IsBackground = true;
					_threadM32RRDump.Start();
				}
			}
		}

		private void button_Write_Click(object sender, RoutedEventArgs e)
		{
			_threadM32RRWrite = new Thread(M32RR_Write);
			_threadM32RRWrite.IsBackground = true;
			_threadM32RRWrite.Start();
		}

		private void button_LoadFile_Click(object sender, RoutedEventArgs e)
		{
			textBlock_FileName.Text = "";
			textBlock_FileSize.Text = "";
			textBlock_FileDensoID.Text = "";
			textBlock_FileChecksum.Text = "";
			button_CorrectionFile.Visibility = Visibility.Hidden;
			System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
			openFileDialog.Filter = "BIN Files (.bin)|*.bin|All Files (*.*)|*.*";
			openFileDialog.Title = "Open a bin file";
			openFileDialog.InitialDirectory = "";
			if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				_filePathWrite = openFileDialog.FileName.ToString();
				_fileSize = new FileInfo(_filePathWrite).Length;
				_fileSize /= 1024L;
				string text = openFileDialog.SafeFileName.ToString();
				if (openFileDialog.SafeFileName.Length >= 45)
				{
					text = text.Remove(45, text.Length - 45);
					text += " ...";
				}
				textBlock_FileName.Text = "File: " + text;
				textBlock_FileSize.Text = "Size: " + _fileSize + "KB";
				if (_fileSize == 1024 || _fileSize == 512)
				{
					new Thread(calculateChecksum).Start();
					textBlock_FileSize.Foreground = new SolidColorBrush(Colors.Black);
					button_Write.IsEnabled = true;
				}
				else
				{
					textBlock_FileSize.Foreground = new SolidColorBrush(Colors.Red);
					button_Write.IsEnabled = false;
				}
				M32RR_isBlank();
			}
			else
			{
				button_Write.IsEnabled = false;
			}
		}

		public void calculateChecksum()
		{
			BinaryReader binaryReader = new BinaryReader(new FileStream(_filePathWrite, FileMode.Open));
			binaryReader.BaseStream.Position = 1048560L;
			_ = binaryReader.BaseStream.Position;
			byte[] serialDenso = new byte[10] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
			long startAddress = 0L;
			long endAddress = 1048575L;
			long checksumAddress = 1048568L;
			if (_fileSize >= 1024)
			{
				for (int i = 0; i < 10; i++)
				{
					serialDenso[i] = binaryReader.ReadByte();
				}
			}
			if (isEmptySerial(serialDenso))
			{
				binaryReader.BaseStream.Position = 524272L;
				for (int j = 0; j < 10; j++)
				{
					serialDenso[j] = binaryReader.ReadByte();
				}
				endAddress = 524287L;
				checksumAddress = 524280L;
			}
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				textBlock_FileDensoID.Text = "Denso ID: " + Encoding.GetEncoding(1252).GetString(serialDenso, 0, 8);
				textBlock_FileChecksum.Text = "Checksum: " + serialDenso[8].ToString("X2") + serialDenso[9].ToString("X2");
			});
			binaryReader.Close();
			_checksumAddress = checksumAddress;
			if (!checksumDenso16.Calculate(startAddress, endAddress, checksumAddress, _filePathWrite))
			{
				return;
			}
			if (checksumDenso16.OldChk == checksumDenso16.NewChk)
			{
				if (checksumDenso16.OldChk == "FFFF")
				{
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						textBlock_FileChecksum.Text += " ( File need check )";
						textBlock_FileChecksum.Foreground = new SolidColorBrush(Colors.Purple);
						button_CorrectionFile.Visibility = Visibility.Hidden;
					});
				}
				else
				{
					base.Dispatcher.BeginInvoke((Action)delegate
					{
						textBlock_FileChecksum.Text += " ( Valid )";
						textBlock_FileChecksum.Foreground = new SolidColorBrush(Colors.Green);
						button_CorrectionFile.Visibility = Visibility.Hidden;
					});
				}
			}
			else
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					textBlock_FileChecksum.Text += " ( Not Valid )";
					textBlock_FileChecksum.Foreground = new SolidColorBrush(Colors.Red);
					button_CorrectionFile.Visibility = Visibility.Visible;
				});
			}
		}

		private bool isEmptySerial(byte[] data)
		{
			try
			{
				for (int i = 0; i < data.Length; i++)
				{
					if (data[i] != byte.MaxValue)
					{
						return false;
					}
				}
				return true;
			}
			catch
			{
				_m32RRTools.LogProperty += "Problem with check empty serial\n";
				return false;
			}
		}

		private void button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			_threadCancel = new Thread(operationCancel);
			_threadCancel.Start();
		}

		public void operationCancel()
		{
			Thread.Sleep(1);
			_m32RRTools.operationStop();
			Thread.Sleep(500);
			_m32RRTools.EcuStatus = "Operation Canceled";
			Thread.Sleep(500);
			_m32RRTools.QueryProgress = 0L;
			M32RR_isBlank();
		}

		private void radioBtn_Pass_Custom_Unchecked(object sender, RoutedEventArgs e)
		{
			disableCustomPass();
		}

		private void radioBtn_Pass_Custom_Checked(object sender, RoutedEventArgs e)
		{
			enableCustomPass();
		}

		public void enableCustomPass()
		{
			textBlock_CustomPass_ANSI.IsEnabled = true;
			textBox_Pass_byte1.IsEnabled = true;
			textBox_Pass_byte2.IsEnabled = true;
			textBox_Pass_byte3.IsEnabled = true;
			textBox_Pass_byte4.IsEnabled = true;
			textBox_Pass_byte5.IsEnabled = true;
			textBox_Pass_byte6.IsEnabled = true;
			textBox_Pass_byte7.IsEnabled = true;
			textBox_Pass_byte8.IsEnabled = true;
			textBox_Pass_byte9.IsEnabled = true;
			textBox_Pass_byte10.IsEnabled = true;
			textBox_Pass_byte11.IsEnabled = true;
			textBox_Pass_byte12.IsEnabled = true;
			button_up_byte1.IsEnabled = true;
			button_up_byte2.IsEnabled = true;
			button_up_byte3.IsEnabled = true;
			button_up_byte4.IsEnabled = true;
			button_up_byte5.IsEnabled = true;
			button_up_byte6.IsEnabled = true;
			button_up_byte7.IsEnabled = true;
			button_up_byte8.IsEnabled = true;
			button_up_byte9.IsEnabled = true;
			button_up_byte10.IsEnabled = true;
			button_up_byte11.IsEnabled = true;
			button_up_byte12.IsEnabled = true;
			button_down_byte1.IsEnabled = true;
			button_down_byte2.IsEnabled = true;
			button_down_byte3.IsEnabled = true;
			button_down_byte4.IsEnabled = true;
			button_down_byte5.IsEnabled = true;
			button_down_byte6.IsEnabled = true;
			button_down_byte7.IsEnabled = true;
			button_down_byte8.IsEnabled = true;
			button_down_byte9.IsEnabled = true;
			button_down_byte10.IsEnabled = true;
			button_down_byte11.IsEnabled = true;
			button_down_byte12.IsEnabled = true;
		}

		public void disableCustomPass()
		{
			textBlock_CustomPass_ANSI.IsEnabled = false;
			textBox_Pass_byte1.IsEnabled = false;
			textBox_Pass_byte2.IsEnabled = false;
			textBox_Pass_byte3.IsEnabled = false;
			textBox_Pass_byte4.IsEnabled = false;
			textBox_Pass_byte5.IsEnabled = false;
			textBox_Pass_byte6.IsEnabled = false;
			textBox_Pass_byte7.IsEnabled = false;
			textBox_Pass_byte8.IsEnabled = false;
			textBox_Pass_byte9.IsEnabled = false;
			textBox_Pass_byte10.IsEnabled = false;
			textBox_Pass_byte11.IsEnabled = false;
			textBox_Pass_byte12.IsEnabled = false;
			button_up_byte1.IsEnabled = false;
			button_up_byte2.IsEnabled = false;
			button_up_byte3.IsEnabled = false;
			button_up_byte4.IsEnabled = false;
			button_up_byte5.IsEnabled = false;
			button_up_byte6.IsEnabled = false;
			button_up_byte7.IsEnabled = false;
			button_up_byte8.IsEnabled = false;
			button_up_byte9.IsEnabled = false;
			button_up_byte10.IsEnabled = false;
			button_up_byte11.IsEnabled = false;
			button_up_byte12.IsEnabled = false;
			button_down_byte1.IsEnabled = false;
			button_down_byte2.IsEnabled = false;
			button_down_byte3.IsEnabled = false;
			button_down_byte4.IsEnabled = false;
			button_down_byte5.IsEnabled = false;
			button_down_byte6.IsEnabled = false;
			button_down_byte7.IsEnabled = false;
			button_down_byte8.IsEnabled = false;
			button_down_byte9.IsEnabled = false;
			button_down_byte10.IsEnabled = false;
			button_down_byte11.IsEnabled = false;
			button_down_byte12.IsEnabled = false;
		}

		public void calcPassANSI()
		{
			if (textBox_Pass_byte1.Text != string.Empty)
			{
				_customPass[0] = (byte)Convert.ToInt16(textBox_Pass_byte1.Text.ToString(), 16);
			}
			if (textBox_Pass_byte2.Text != string.Empty)
			{
				_customPass[1] = (byte)Convert.ToInt16(textBox_Pass_byte2.Text.ToString(), 16);
			}
			if (textBox_Pass_byte3.Text != string.Empty)
			{
				_customPass[2] = (byte)Convert.ToInt16(textBox_Pass_byte3.Text.ToString(), 16);
			}
			if (textBox_Pass_byte4.Text != string.Empty)
			{
				_customPass[3] = (byte)Convert.ToInt16(textBox_Pass_byte4.Text.ToString(), 16);
			}
			if (textBox_Pass_byte5.Text != string.Empty)
			{
				_customPass[4] = (byte)Convert.ToInt16(textBox_Pass_byte5.Text.ToString(), 16);
			}
			if (textBox_Pass_byte6.Text != string.Empty)
			{
				_customPass[5] = (byte)Convert.ToInt16(textBox_Pass_byte6.Text.ToString(), 16);
			}
			if (textBox_Pass_byte7.Text != string.Empty)
			{
				_customPass[6] = (byte)Convert.ToInt16(textBox_Pass_byte7.Text.ToString(), 16);
			}
			if (textBox_Pass_byte8.Text != string.Empty)
			{
				_customPass[7] = (byte)Convert.ToInt16(textBox_Pass_byte8.Text.ToString(), 16);
			}
			if (textBox_Pass_byte9.Text != string.Empty)
			{
				_customPass[8] = (byte)Convert.ToInt16(textBox_Pass_byte9.Text.ToString(), 16);
			}
			if (textBox_Pass_byte10.Text != string.Empty)
			{
				_customPass[9] = (byte)Convert.ToInt16(textBox_Pass_byte10.Text.ToString(), 16);
			}
			if (textBox_Pass_byte11.Text != string.Empty)
			{
				_customPass[10] = (byte)Convert.ToInt16(textBox_Pass_byte11.Text.ToString(), 16);
			}
			if (textBox_Pass_byte12.Text != string.Empty)
			{
				_customPass[11] = (byte)Convert.ToInt16(textBox_Pass_byte12.Text.ToString(), 16);
			}
			textBlock_CustomPass_ANSI.Text = Encoding.GetEncoding(1252).GetString(_customPass);
		}

		private Version getRunningVersion()
		{
			try
			{
				return ApplicationDeployment.CurrentDeployment.CurrentVersion;
			}
			catch (Exception)
			{
				return Assembly.GetExecutingAssembly().GetName().Version;
			}
		}

		private void textBox_Pass_byte1_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte1.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte1.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte1.SelectionStart = textBox_Pass_byte1.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte1_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte1.Text = hexByteUp(textBox_Pass_byte1.Text.ToString()).ToString("X2");
		}

		private void button_down_byte1_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte1.Text = hexByteDown(textBox_Pass_byte1.Text.ToString()).ToString("X2");
		}

		public int hexByteUp(string hexString)
		{
			if (hexString == "")
			{
				hexString = "00";
			}
			int num = Convert.ToInt16(hexString, 16);
			num++;
			if (num >= 255)
			{
				num = 255;
			}
			return num;
		}

		public int hexByteDown(string hexString)
		{
			if (hexString == "")
			{
				hexString = "FF";
			}
			int num = Convert.ToInt16(hexString, 16);
			num--;
			if (num <= 0)
			{
				num = 0;
			}
			return num;
		}

		private void textBox_Pass_byte2_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte2.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte2.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte2.SelectionStart = textBox_Pass_byte2.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte2_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte2.Text = hexByteUp(textBox_Pass_byte2.Text.ToString()).ToString("X2");
		}

		private void button_down_byte2_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte2.Text = hexByteDown(textBox_Pass_byte2.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte3_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte3.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte3.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte3.SelectionStart = textBox_Pass_byte3.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte3_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte3.Text = hexByteUp(textBox_Pass_byte3.Text.ToString()).ToString("X2");
		}

		private void button_down_byte3_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte3.Text = hexByteDown(textBox_Pass_byte3.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte4_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte4.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte4.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte4.SelectionStart = textBox_Pass_byte4.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte4_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte4.Text = hexByteUp(textBox_Pass_byte4.Text.ToString()).ToString("X2");
		}

		private void button_down_byte4_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte4.Text = hexByteDown(textBox_Pass_byte4.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte5_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte5.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte5.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte5.SelectionStart = textBox_Pass_byte5.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte5_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte5.Text = hexByteUp(textBox_Pass_byte5.Text.ToString()).ToString("X2");
		}

		private void button_down_byte5_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte5.Text = hexByteDown(textBox_Pass_byte5.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte6_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte6.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte6.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte6.SelectionStart = textBox_Pass_byte6.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte6_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte6.Text = hexByteUp(textBox_Pass_byte6.Text.ToString()).ToString("X2");
		}

		private void button_down_byte6_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte6.Text = hexByteDown(textBox_Pass_byte6.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte7_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte7.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte7.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte7.SelectionStart = textBox_Pass_byte7.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte7_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte7.Text = hexByteUp(textBox_Pass_byte7.Text.ToString()).ToString("X2");
		}

		private void button_down_byte7_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte7.Text = hexByteDown(textBox_Pass_byte7.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte8_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte8.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte8.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte8.SelectionStart = textBox_Pass_byte8.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte8_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte8.Text = hexByteUp(textBox_Pass_byte8.Text.ToString()).ToString("X2");
		}

		private void button_down_byte8_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte8.Text = hexByteDown(textBox_Pass_byte8.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte9_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte9.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte9.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte9.SelectionStart = textBox_Pass_byte9.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte9_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte9.Text = hexByteUp(textBox_Pass_byte9.Text.ToString()).ToString("X2");
		}

		private void button_down_byte9_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte9.Text = hexByteDown(textBox_Pass_byte9.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte10_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte10.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte10.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte10.SelectionStart = textBox_Pass_byte10.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte10_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte10.Text = hexByteUp(textBox_Pass_byte10.Text.ToString()).ToString("X2");
		}

		private void button_down_byte10_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte10.Text = hexByteDown(textBox_Pass_byte10.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte11_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte11.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte11.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte11.SelectionStart = textBox_Pass_byte11.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte11_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte11.Text = hexByteUp(textBox_Pass_byte11.Text.ToString()).ToString("X2");
		}

		private void button_down_byte11_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte11.Text = hexByteDown(textBox_Pass_byte11.Text.ToString()).ToString("X2");
		}

		private void textBox_Pass_byte12_TextChanged(object sender, TextChangedEventArgs e)
		{
			string text = textBox_Pass_byte12.Text;
			int result = 0;
			if (!int.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out result) && text != string.Empty)
			{
				textBox_Pass_byte12.Text = text.Remove(text.Length - 1, 1);
				textBox_Pass_byte12.SelectionStart = textBox_Pass_byte12.Text.Length;
			}
			calcPassANSI();
		}

		private void button_up_byte12_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte12.Text = hexByteUp(textBox_Pass_byte12.Text.ToString()).ToString("X2");
		}

		private void button_down_byte12_Click(object sender, RoutedEventArgs e)
		{
			textBox_Pass_byte12.Text = hexByteDown(textBox_Pass_byte12.Text.ToString()).ToString("X2");
		}

		private void btn_Exit_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Application.Current.Shutdown();
		}

		private void btn_About_Click(object sender, RoutedEventArgs e)
		{
			new WindowAbout().ShowDialog();
		}

		private void button_CorrectionFile_Click(object sender, RoutedEventArgs e)
		{
			if (System.Windows.Forms.MessageBox.Show("New Checksum: " + checksumDenso16.NewChk + "\nOld Checksum: " + checksumDenso16.OldChk, "Write the new checksum ?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
			{
				if (checksumDenso16.Write(_checksumAddress, _filePathWrite))
				{
					UpdateChecksum();
				}
				else
				{
					System.Windows.MessageBox.Show("Write problem, check address", "Error");
				}
			}
		}
	}
}
