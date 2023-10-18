using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using FTD2XX_NET;

namespace M32RR_FLASH_TOOL
{
	internal class M32RRTools : INotifyPropertyChanged
	{
		private FTDI _myFtdiDevice = new FTDI();

		private FTDI.FT_STATUS _ftStatus;

		private byte[] _byteStart = new byte[1] { 112 };

		private byte[] _byteConnect = new byte[1];

		private byte[] _bytePassRequest = new byte[6] { 112, 245, 132, 0, 0, 12 };

		private byte[] _byteErase = new byte[2] { 167, 208 };

		private byte[] _byteIsBlank = new byte[5] { 224, 0, 0, 255, 15 };

		private byte[] _byteCksRequest = new byte[5] { 225, 0, 0, 255, 15 };

		private byte[] _byteOK = new byte[1] { 6 };

		private int _readDataInt;

		private long _binSize;

		private volatile bool _stopThread;

		private byte[] _byteDataRead;

		private string _readDataConvert;

		public byte[] _byteSwSerial = new byte[10];

		public string softwareSerial;

		public byte[] _passwordBytes = new byte[12];

		public byte[] _byteEcuChecksum = new byte[2];

		public bool _isEcuUnlocked;

		public bool _isECUStillUnlocked;

		public bool _isBlank = true;

		public bool _FTDIConnected;

		public string _passwordString;

		public string ecuStatus = "";

		public string _log = "";

		public long _queryProgress;

		public string LogProperty
		{
			get
			{
				return _log;
			}
			set
			{
				_log = value;
				OnPropertyChanged("LogProperty");
			}
		}

		public long QueryProgress
		{
			get
			{
				return _queryProgress;
			}
			set
			{
				_queryProgress = value;
				OnPropertyChanged("QueryProgress");
			}
		}

		public string SoftwareSerial
		{
			get
			{
				return softwareSerial;
			}
			set
			{
				softwareSerial = value;
				OnPropertyChanged("SoftwareSerial");
			}
		}

		public string EcuStatus
		{
			get
			{
				return ecuStatus;
			}
			set
			{
				ecuStatus = value;
				OnPropertyChanged("EcuStatus");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string info)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
		}

		public bool FtdiConnect(uint baudRate)
		{
			try
			{
				uint devcount = 0u;
				_ftStatus = FTDI.FT_STATUS.FT_OK;
				_ftStatus = _myFtdiDevice.GetNumberOfDevices(ref devcount);
				if (_ftStatus == FTDI.FT_STATUS.FT_OK)
				{
					updateLog("Number of FTDI devices: " + devcount + "\n");
					if (devcount == 0)
					{
						updateLog("Failed to get number of devices (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					FTDI.FT_DEVICE_INFO_NODE[] array = new FTDI.FT_DEVICE_INFO_NODE[devcount];
					_ftStatus = _myFtdiDevice.GetDeviceList(array);
					if (_ftStatus == FTDI.FT_STATUS.FT_OK)
					{
						for (uint num = 0u; num < devcount; num++)
						{
							updateLog("Device Index: " + num + "\n");
							updateLog("Serial Number: " + array[num].SerialNumber.ToString() + "\n");
							updateLog("Description: " + array[num].Description.ToString() + "\n");
						}
					}
					_ftStatus = _myFtdiDevice.OpenBySerialNumber(array[0].SerialNumber);
					if (_ftStatus != 0)
					{
						updateLog("Failed to open device (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					_ftStatus = _myFtdiDevice.SetBaudRate(baudRate);
					if (_ftStatus != 0)
					{
						updateLog("Failed to set Baud rate (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					updateLog("Baudrate set to: " + baudRate + "\n");
					_ftStatus = _myFtdiDevice.SetDataCharacteristics(8, 0, 0);
					if (_ftStatus != 0)
					{
						updateLog("Failed to set data characteristics (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					_ftStatus = _myFtdiDevice.SetFlowControl(0, 17, 19);
					if (_ftStatus != 0)
					{
						updateLog("Failed to set flow control (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					_ftStatus = _myFtdiDevice.SetTimeouts(15000u, 0u);
					if (_ftStatus != 0)
					{
						updateLog("Failed to set timeouts (error " + _ftStatus.ToString() + ")\n");
						return false;
					}
					return true;
				}
				updateLog("Failed to get number of devices (error " + _ftStatus.ToString() + ")\n");
				return false;
			}
			catch
			{
				updateLog("Problem with settings FTDI...\n");
				return false;
			}
		}

		public bool FtdiClose()
		{
			try
			{
				_ftStatus = _myFtdiDevice.Close();
				updateLog("FTDI device closed\n\n");
				if (_ftStatus != 0)
				{
					updateLog("Failed to close device (error " + _ftStatus.ToString() + ")\n");
					return false;
				}
				_FTDIConnected = false;
				SoftwareSerial = "";
				EcuStatus = "";
				updateQueryProgress(0L, 100L);
				return true;
			}
			catch
			{
				updateLog("Ftdi close device problem..\n");
				return false;
			}
		}

		public bool M32RR_RequestConnect()
		{
			try
			{
				if (!M32RR_SendData(_byteStart))
				{
					return false;
				}
				updateLog("\nConnecting to ECU...\n");
				if (M32R_DataAvailable(1, 200) == 0)
				{
					for (int i = 0; i < 16; i++)
					{
						M32RR_SendData(_byteConnect);
						if (M32R_DataAvailable(1, 200) != 0)
						{
							M32R_EcuResponse();
							if (_readDataConvert == "06")
							{
								updateLog("Sync'd\n\n");
								return true;
							}
							if (_readDataConvert == "80")
							{
								M32R_EcuResponse();
								updateLog("Sync'd\n\n");
								return true;
							}
						}
					}
				}
				else if (M32R_DataAvailable(1, 200) != 0)
				{
					M32R_EcuResponse();
					if (_readDataConvert == "06")
					{
						updateLog("Sync'd\n\n");
						return true;
					}
					if (_readDataConvert == "80")
					{
						M32R_EcuResponse();
						updateLog("Sync'd\n\n");
						return true;
					}
				}
				updateLog("No ECU Answer.. Check the wiring..\n\n");
				return false;
			}
			catch
			{
				updateLog("There is something wrong with the connect request\n");
				return false;
			}
		}

		public bool M32RR_RequestPassword(byte[] pass)
		{
			_passwordString = pass[0].ToString("X2") + " " + pass[1].ToString("X2") + " " + pass[2].ToString("X2") + " " + pass[3].ToString("X2") + " " + pass[4].ToString("X2") + " " + pass[5].ToString("X2") + " " + pass[6].ToString("X2") + " " + pass[7].ToString("X2") + " " + pass[8].ToString("X2") + " " + pass[9].ToString("X2") + " " + pass[10].ToString("X2") + " " + pass[11].ToString("X2") + " \n";
			_isEcuUnlocked = false;
			_isECUStillUnlocked = false;
			try
			{
				if (!M32RR_SendData(_byteStart))
				{
					return false;
				}
				if (!M32R_EcuResponse(2u))
				{
					updateLog("No ECU answer ..\n");
					return false;
				}
				if (pass.Length != 12)
				{
					updateLog("Please enter 12 bytes Hex pass");
					return false;
				}
				if (_readDataConvert == "8080" || _readDataConvert == "8084")
				{
					if (!M32RR_SendData(_bytePassRequest))
					{
						return false;
					}
					if (!M32RR_SendData(pass))
					{
						return false;
					}
					updateLog("Send password " + _passwordString);
					if (!M32R_EcuResponse(3u))
					{
						updateLog("No ECU answer password send ..\n");
						return false;
					}
					if (_readDataConvert == "808006" || _readDataConvert == "808406")
					{
						_isEcuUnlocked = true;
						EcuStatus = "Unlocked";
						updateLog("ECU Unlocked\n\n");
						updateLog("ANSI Pass: " + Encoding.GetEncoding(1252).GetString(pass) + "\n");
						updateLog("HEX Pass: " + _passwordString + "\n\n");
						M32RR_isBlank();
					}
					else if (_readDataConvert == "808015" || _readDataConvert == "808415")
					{
						_isEcuUnlocked = false;
						updateLog("ECU locked\n");
					}
				}
				else if (_readDataConvert == "808C")
				{
					_isEcuUnlocked = true;
					_isECUStillUnlocked = true;
					EcuStatus = "Still Unlocked";
					updateLog("ECU Still Unlocked\n\n");
					M32RR_isBlank();
				}
				return true;
			}
			catch
			{
				updateLog("There is something wrong with the pass request\n");
				return false;
			}
		}

		public bool M32RR_HackPassList(string filePath)
		{
			try
			{
				int num = 0;
				_isEcuUnlocked = false;
				int num2 = File.ReadAllLines(filePath).Length;
				QueryProgress = 0L;
				if (!new FileInfo(filePath).Exists)
				{
					File.Create(filePath);
					updateLog("password.txt missing...\n");
					FtdiClose();
					return false;
				}
				if (new FileInfo(filePath).Length == 0L)
				{
					updateLog("password.txt is empty\n");
				}
				StreamReader streamReader = new StreamReader(filePath);
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					if (!_isEcuUnlocked)
					{
						string[] array = text.Split(',');
						for (int i = 0; i < array.Length; i++)
						{
							_passwordBytes[i] = Convert.ToByte(array[i], 16);
						}
						M32RR_RequestPassword(_passwordBytes);
					}
					num++;
					updateQueryProgress(num, num2);
					if (!_isEcuUnlocked)
					{
						EcuStatus = "Password List: " + _queryProgress + "%";
					}
				}
				streamReader.Close();
				if (!_isEcuUnlocked)
				{
					updateLog("No Password Founded\n");
					QueryProgress = 0L;
					FtdiClose();
					return false;
				}
				QueryProgress = 0L;
				return true;
			}
			catch
			{
				updateLog("File password.txt read problem\n");
				return false;
			}
		}

		public bool SuzukiPass()
		{
			try
			{
				_isEcuUnlocked = false;
				QueryProgress = 0L;
				_passwordBytes[0] = 83;
				_passwordBytes[1] = 85;
				_passwordBytes[2] = 69;
				_passwordBytes[3] = 70;
				_passwordBytes[4] = 73;
				_passwordBytes[5] = 77;
				_passwordBytes[6] = byte.MaxValue;
				_passwordBytes[7] = byte.MaxValue;
				_passwordBytes[8] = byte.MaxValue;
				_passwordBytes[9] = byte.MaxValue;
				_passwordBytes[10] = 86;
				_passwordBytes[11] = 48;
				M32RR_RequestPassword(_passwordBytes);
				QueryProgress = 50L;
				if (!_isEcuUnlocked)
				{
					_passwordBytes[0] = 83;
					_passwordBytes[1] = 85;
					_passwordBytes[2] = 69;
					_passwordBytes[3] = 84;
					_passwordBytes[4] = 86;
					_passwordBytes[5] = 77;
					_passwordBytes[6] = byte.MaxValue;
					_passwordBytes[7] = byte.MaxValue;
					_passwordBytes[8] = byte.MaxValue;
					_passwordBytes[9] = byte.MaxValue;
					_passwordBytes[10] = 86;
					_passwordBytes[11] = 48;
					M32RR_RequestPassword(_passwordBytes);
					QueryProgress = 100L;
				}
				if (!_isEcuUnlocked)
				{
					updateLog("Suzuki Password Fail\n\n");
					QueryProgress = 0L;
					FtdiClose();
					return false;
				}
				QueryProgress = 0L;
				return true;
			}
			catch
			{
				updateLog("Suzuki Pass problem\n");
				return false;
			}
		}

		public bool KawasakiPass()
		{
			try
			{
				_isEcuUnlocked = false;
				QueryProgress = 0L;
				_passwordBytes[0] = 75;
				_passwordBytes[1] = 65;
				_passwordBytes[2] = 69;
				_passwordBytes[3] = 70;
				_passwordBytes[4] = 73;
				_passwordBytes[5] = 77;
				_passwordBytes[6] = byte.MaxValue;
				_passwordBytes[7] = byte.MaxValue;
				_passwordBytes[8] = byte.MaxValue;
				_passwordBytes[9] = byte.MaxValue;
				_passwordBytes[10] = 86;
				_passwordBytes[11] = 48;
				M32RR_RequestPassword(_passwordBytes);
				QueryProgress = 50L;
				if (!_isEcuUnlocked)
				{
					_passwordBytes[0] = 75;
					_passwordBytes[1] = 65;
					_passwordBytes[2] = 69;
					_passwordBytes[3] = 84;
					_passwordBytes[4] = 86;
					_passwordBytes[5] = 77;
					_passwordBytes[6] = byte.MaxValue;
					_passwordBytes[7] = byte.MaxValue;
					_passwordBytes[8] = byte.MaxValue;
					_passwordBytes[9] = byte.MaxValue;
					_passwordBytes[10] = 86;
					_passwordBytes[11] = 48;
					M32RR_RequestPassword(_passwordBytes);
					QueryProgress = 100L;
				}
				if (!_isEcuUnlocked)
				{
					updateLog("Kawasaki Password Fail\n\n");
					QueryProgress = 0L;
					FtdiClose();
					return false;
				}
				QueryProgress = 0L;
				return true;
			}
			catch
			{
				updateLog("Kawasaki Pass problem\n");
				return false;
			}
		}

		public bool CustomPass(byte[] customPass)
		{
			try
			{
				_isEcuUnlocked = false;
				QueryProgress = 0L;
				M32RR_RequestPassword(customPass);
				QueryProgress = 100L;
				if (!_isEcuUnlocked)
				{
					updateLog("Custom Password Fail\n\n");
					QueryProgress = 0L;
					FtdiClose();
					return false;
				}
				QueryProgress = 0L;
				return true;
			}
			catch
			{
				updateLog("Custom Pass problem\n");
				return false;
			}
		}

		public bool M32RR_isConnected()
		{
			try
			{
				if (!M32RR_SendData(_byteStart))
				{
					return false;
				}
				if (!M32R_EcuResponse(2u))
				{
					updateLog("No ECU answer ..\n");
					return false;
				}
				if (_readDataConvert == "808C")
				{
					return true;
				}
				return false;
			}
			catch
			{
				updateLog("Request is connected problem...\n");
				return false;
			}
		}

		public bool M32RR_Erase()
		{
			try
			{
				_stopThread = false;
				if (M32RR_isConnected())
				{
					Stopwatch stopwatch = Stopwatch.StartNew();
					while (!_stopThread)
					{
						if (!M32RR_SendData(_byteErase))
						{
							return false;
						}
						updateLog("Send request to erase ECU...\n");
						int i = 0;
						int num = 0;
						for (; i < 200; i++)
						{
							if (M32R_DataAvailable(1, 180) >= 1)
							{
								M32R_EcuResponse();
								updateQueryProgress(100L, 100L);
								EcuStatus = "Erase: " + _queryProgress + "%";
								break;
							}
							updateQueryProgress(num, 100L);
							EcuStatus = "Erase: " + _queryProgress + "%";
							if (num < 100)
							{
								num++;
							}
							if (i == 199)
							{
								updateLog("No erase confirm by the ecu... There is something wrong");
								return false;
							}
						}
						if (_readDataConvert == "06")
						{
							_isBlank = true;
							long num2 = stopwatch.ElapsedMilliseconds / 1000;
							long num3 = num2 % 60;
							long num4 = num2 / 60;
							updateLog("ECU erased in " + num4 + "m " + num3 + "s\n\n");
							EcuStatus = "Erased";
							SoftwareSerial = "";
						}
						else if (_readDataConvert == "15")
						{
							_isBlank = false;
							updateLog("ECU not Erased....\n\n");
							EcuStatus = "Not Erased !";
						}
						_stopThread = true;
					}
					stopwatch.Stop();
					return true;
				}
				return false;
			}
			catch
			{
				updateLog("Erase problem....\n");
				return false;
			}
		}

		public bool M32RR_RequestEcuChecksum()
		{
			try
			{
				if (M32RR_isConnected())
				{
					if (!M32RR_SendData(_byteCksRequest))
					{
						return false;
					}
					if (!M32R_EcuResponse(2u))
					{
						updateLog("No ECU answer ..\n");
						return false;
					}
					int value = Convert.ToUInt16(_readDataConvert, 16);
					_byteEcuChecksum = BitConverter.GetBytes(value);
					return true;
				}
				return false;
			}
			catch
			{
				updateLog("Request Ecu checksum problem....\n");
				return false;
			}
		}

		public bool M32RR_isBlank()
		{
			try
			{
				if (!M32RR_SendData(_byteIsBlank))
				{
					return false;
				}
				Thread.Sleep(200);
				if (!M32RR_EcuAnswer())
				{
					updateLog("No ECU answer is blank..\n");
					return false;
				}
				if (_readDataInt.ToString("X2") == "6000000")
				{
					_isBlank = true;
				}
				else if (_readDataInt.ToString("X2") == "15000000")
				{
					_isBlank = false;
					M32RR_RequestEcuChecksum();
					return false;
				}
				return true;
			}
			catch
			{
				updateLog("Read isBlank problem....\n");
				return false;
			}
		}

		private bool M32RR_SendData(byte[] data)
		{
			try
			{
				uint numBytesWritten = 0u;
				_ftStatus = _myFtdiDevice.Write(data, data.Length, ref numBytesWritten);
				if (_ftStatus != 0)
				{
					updateLog("Failed to write to device (error " + _ftStatus.ToString() + ")\n");
					return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool M32RR_EcuAnswer()
		{
			uint RxQueue = 0u;
			int num = 0;
			do
			{
				_ftStatus = _myFtdiDevice.GetRxBytesAvailable(ref RxQueue);
				if (_ftStatus != 0)
				{
					updateLog("Failed to get number of bytes available to read (error " + _ftStatus.ToString() + ")\n");
					return false;
				}
				Thread.Sleep(10);
				num++;
			}
			while (RxQueue < 1 && num > 3);
			uint numBytesRead = 0u;
			byte[] array = new byte[4];
			int num2 = 0;
			if (RxQueue >= 1)
			{
				_ftStatus = _myFtdiDevice.Read(array, RxQueue, ref numBytesRead);
				if (_ftStatus != 0)
				{
					updateLog("Failed to read data (error " + _ftStatus.ToString() + ")\n");
					return false;
				}
				num2 = Convert.ToInt32(BitConverter.ToString(array).Replace("-", ""), 16);
				_readDataInt = num2;
				return true;
			}
			return false;
		}

		private bool M32R_EcuResponse(uint dataSizeRequest = 1u)
		{
			uint numBytesRead = 0u;
			_byteDataRead = new byte[dataSizeRequest];
			if (dataSizeRequest >= 1)
			{
				_ftStatus = _myFtdiDevice.Read(_byteDataRead, dataSizeRequest, ref numBytesRead);
				if (_ftStatus != 0)
				{
					updateLog("Failed to read data (error " + _ftStatus.ToString() + ")\n");
					return false;
				}
				if (_ftStatus == FTDI.FT_STATUS.FT_OK && numBytesRead < dataSizeRequest)
				{
					updateLog("Timeout read. Bytes read: " + numBytesRead + " Bytes need: " + dataSizeRequest + "\n");
					return false;
				}
				_readDataConvert = BitConverter.ToString(_byteDataRead).Replace("-", "");
				return true;
			}
			return false;
		}

		private uint M32R_DataAvailable(int dataSizeRequest, int timeOut)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			uint RxQueue = 0u;
			do
			{
				_ftStatus = _myFtdiDevice.GetRxBytesAvailable(ref RxQueue);
				if (_ftStatus != 0)
				{
					updateLog("Failed to get number of bytes available to read (error " + _ftStatus.ToString() + ")\n");
					stopwatch.Stop();
					return 0u;
				}
				if (stopwatch.ElapsedMilliseconds > timeOut)
				{
					stopwatch.Stop();
					return 0u;
				}
			}
			while (RxQueue < dataSizeRequest);
			stopwatch.Stop();
			return RxQueue;
		}

		private bool getSwSerial(byte[] byteOffset)
		{
			try
			{
				if (M32RR_isConnected())
				{
					if (!M32RR_SendData(byteOffset))
					{
						return false;
					}
					if (!M32R_EcuResponse(256u))
					{
						updateLog("No ECU answer ..\n");
						return false;
					}
					if (!M32RR_SendData(_byteOK))
					{
						return false;
					}
					int num = 0;
					for (int i = 240; i < 250; i++)
					{
						_byteSwSerial[num] = _byteDataRead[i];
						num++;
					}
				}
				return true;
			}
			catch
			{
				updateLog("Problem with read serial ...\n");
				return false;
			}
		}

		public bool checkSwSerial()
		{
			try
			{
				byte[] byteOffset = new byte[3] { 255, 255, 15 };
				byte[] byteOffset2 = new byte[3] { 255, 255, 7 };
				getSwSerial(byteOffset);
				for (int i = 0; i < _byteSwSerial.Length; i++)
				{
					if (_byteSwSerial[i] != byte.MaxValue)
					{
						SoftwareSerial = "Denso ID: " + Encoding.GetEncoding(1252).GetString(_byteSwSerial).Remove(8, 2);
						return true;
					}
				}
				getSwSerial(byteOffset2);
				for (int j = 0; j < _byteSwSerial.Length; j++)
				{
					if (_byteSwSerial[j] != byte.MaxValue)
					{
						SoftwareSerial = "Denso ID: " + Encoding.GetEncoding(1252).GetString(_byteSwSerial).Remove(8, 2);
						return true;
					}
				}
				SoftwareSerial = "Denso ID: not found";
				return false;
			}
			catch
			{
				return false;
			}
		}

		public bool M32RR_Dump(long offsetAddress, string file)
		{
			try
			{
				BinaryWriter binaryWriter = new BinaryWriter(new FileStream(file, FileMode.Create));
				binaryWriter.BaseStream.Position = offsetAddress;
				byte[] array = new byte[3] { 255, 0, 0 };
				_ = new byte[256];
				Stopwatch stopwatch = Stopwatch.StartNew();
				_stopThread = false;
				if (M32RR_isConnected())
				{
					updateLog("ECU reading ...\n");
					while (binaryWriter.BaseStream.Position <= 1048575 && !_stopThread)
					{
						if (!M32RR_SendData(array))
						{
							return false;
						}
						if (!M32R_EcuResponse(256u))
						{
							updateLog("Problem to get data buffer..\n");
							binaryWriter.Close();
							if (File.Exists(file))
							{
								File.Delete(file);
							}
							return false;
						}
						if (!M32RR_SendData(_byteOK))
						{
							binaryWriter.Close();
							return false;
						}
						binaryWriter.Write(_byteDataRead);
						if (array[1] == byte.MaxValue)
						{
							array[2]++;
							array[1] = 0;
						}
						else
						{
							array[1]++;
						}
						updateQueryProgress(binaryWriter.BaseStream.Position, 1048575L);
						EcuStatus = "Reading: " + _queryProgress + "%";
						if (binaryWriter.BaseStream.Position >= 1048575)
						{
							_stopThread = true;
						}
					}
				}
				if (binaryWriter.BaseStream.Position < 1048575)
				{
					updateLog("Dump canceled\n\n");
					binaryWriter.Close();
					if (File.Exists(file))
					{
						File.Delete(file);
					}
				}
				else
				{
					updateLog("Bin Saved to: " + file + "\n");
					long num = stopwatch.ElapsedMilliseconds / 1000;
					long num2 = num % 60;
					long num3 = num / 60;
					updateLog("Dumped in " + num3 + "m " + num2 + "s\n\n");
					binaryWriter.Close();
				}
				stopwatch.Stop();
				return true;
			}
			catch
			{
				updateLog("There is a problem with Dump, contact the developper..\n");
				return false;
			}
		}

		public bool M32RR_Write(long offsetAddress, string file)
		{
			try
			{
				BinaryReader binaryReader = new BinaryReader(new FileStream(file, FileMode.Open));
				binaryReader.BaseStream.Position = offsetAddress;
				_binSize = new FileInfo(file).Length;
				_binSize /= 1024L;
				byte[] array = new byte[3] { 65, 0, 0 };
				byte[] array2 = new byte[256];
				long num = 0L;
				Stopwatch stopwatch = Stopwatch.StartNew();
				_stopThread = false;
				if (_binSize == 1024)
				{
					num = 1048575L;
				}
				if (_binSize == 512)
				{
					num = 524287L;
				}
				if (M32RR_isConnected())
				{
					if (!M32RR_isBlank())
					{
						updateLog("Ecu is not blank, please erase first\n");
						return false;
					}
					updateLog("Bin loaded: " + file + "\n\n");
					updateLog("ECU programming ...\n");
					while (binaryReader.BaseStream.Position <= num && !_stopThread)
					{
						array2 = binaryReader.ReadBytes(256);
						if (!isEmptyBlock(array2))
						{
							if (!M32RR_SendData(array))
							{
								return false;
							}
							if (!M32RR_SendData(array2))
							{
								return false;
							}
							if (!M32R_EcuResponse())
							{
								updateLog("No ECU answer ..\n");
								return false;
							}
							if (_readDataConvert != "06")
							{
								updateLog("Sorry, there is problem to write the ecu...\n");
								updateLog(_readDataConvert);
								return false;
							}
						}
						if (array[1] == byte.MaxValue)
						{
							array[2]++;
							array[1] = 0;
						}
						else
						{
							array[1]++;
						}
						updateQueryProgress(binaryReader.BaseStream.Position, num);
						EcuStatus = "Programming: " + _queryProgress + "%";
						if (binaryReader.BaseStream.Position >= num)
						{
							_stopThread = true;
						}
					}
				}
				_isBlank = false;
				stopwatch.Stop();
				if (binaryReader.BaseStream.Position < num)
				{
					updateLog("Programming canceled\n\n");
				}
				else
				{
					long num2 = stopwatch.ElapsedMilliseconds / 1000;
					long num3 = num2 % 60;
					long num4 = num2 / 60;
					updateLog("Programmed in " + num4 + "m " + num3 + "s\n\n");
				}
				binaryReader.Close();
				M32RR_isBlank();
				checkSwSerial();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void operationStop()
		{
			_stopThread = true;
		}

		public bool isEmptyBlock(byte[] data)
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
				updateLog("Problem with check empty block write\n");
				return false;
			}
		}

		private byte[] BigEndianConvert(int data)
		{
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			return bytes;
		}

		private void updateQueryProgress(long progress, long end)
		{
			QueryProgress = progress * 100 / end;
		}

		private void updateLog(string message)
		{
			LogProperty += message;
		}
	}
}