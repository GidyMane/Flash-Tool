using System;
using System.IO;

namespace M32RR_FLASH_TOOL
{

	internal class ChecksumDenso16
	{
		private long OffsetReader;

		private string CurrentDataString = "";

		private int CurrentData;

		private int ChkCalc;

		private short WriteData;

		public string NewChk { get; private set; }

		public string OldChk { get; private set; }

		public bool Calculate(long startAddress, long endAddress, long checksumAddress, string file)
		{
			ChkCalc = 23205;
			try
			{
				BinaryReader binaryReader = new BinaryReader(new FileStream(file, FileMode.Open));
				binaryReader.BaseStream.Position = startAddress;
				for (OffsetReader = binaryReader.BaseStream.Position; OffsetReader < endAddress; OffsetReader = binaryReader.BaseStream.Position)
				{
					CurrentDataString = BitConverter.ToString(binaryReader.ReadBytes(2)).Replace("-", "");
					CurrentData = Convert.ToInt32(CurrentDataString, 16);
					if (OffsetReader != checksumAddress)
					{
						ChkCalc += 65536;
						ChkCalc -= CurrentData;
						if (ChkCalc >= 65536)
						{
							ChkCalc -= 65536;
						}
					}
					else
					{
						OldChk = CurrentData.ToString("X4");
					}
				}
				binaryReader.Close();
				NewChk = ChkCalc.ToString("X4");
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool Write(long checksumAddress, string file)
		{
			try
			{
				BinaryWriter binaryWriter = new BinaryWriter(new FileStream(file, FileMode.Open));
				WriteData = Convert.ToInt16(NewChk, 16);
				binaryWriter.BaseStream.Position = checksumAddress;
				binaryWriter.Write(BigEndianConvert(WriteData));
				binaryWriter.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}

		private byte[] BigEndianConvert(short data)
		{
			byte[] bytes = BitConverter.GetBytes(data);
			Array.Reverse(bytes);
			return bytes;
		}
	}
}