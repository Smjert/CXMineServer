using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CXMineServer
{
	public static class CXMineServer
	{
		public const int ProtocolVersion = 8;

		//TODO: a che serve?
		//private static Random Random;
		public static Server Server;

		private static StreamWriter receiveFile;
		private static StreamWriter sendFile;
		
		public static void Main(string[] args)
		{
			Log("CXMineServer is starting...");
			
			Configuration.Load();
			
			/*if (Configuration.Defined("random-seed")) {
				Random = new Random(Configuration.GetInt("random-seed", 0));
			} else {
				Random = new Random();
			}*/
			
			Server = new Server();
			try {
				receiveFile = new StreamWriter("receive.txt");
				sendFile = new StreamWriter("send.txt");
				Server.Run();
			}
			catch (Exception e) {
				Log("Fatal uncaught exception: " + e);
				Console.ReadLine();
			}
			receiveFile.Close();
			receiveFile.Dispose();
			receiveFile = null;

			sendFile.Close();
			sendFile.Dispose();
			sendFile = null;

			Log("Bye!");
			Console.ReadLine();
		}
		
		public static void Log(string message)
		{
			Console.WriteLine(FormatTime() + "    " + message);
		}

		public static void ReceiveLogFile(string message)
		{
			lock(receiveFile)
			{
				if (receiveFile == null)
					receiveFile = new StreamWriter("receive.txt");

				receiveFile.Write(message);

				receiveFile.Flush();
			}
		}

		public static void SendLogFile(string message)
		{
			if (sendFile == null)
				sendFile = new StreamWriter("send.txt");

			sendFile.Write(message);

			sendFile.Flush();
		}
		
		public static string FormatTime()
		{
			return DateTime.Now.ToString("HH:mm:ss");
		}

		public static string MD5sum(string str)
		{
			MD5CryptoServiceProvider crypto = new MD5CryptoServiceProvider();
			byte[] data = Encoding.ASCII.GetBytes(str);
			data = crypto.ComputeHash(data);
			StringBuilder ret = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
				ret.Append(data[i].ToString("x2").ToLower());
			return ret.ToString();
		}
		
		public static string Base64Encode(string str)
		{
			return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(str));
		}
		
		public static string Base36Encode(long input)
		{
			if (input == 0) return "0";
			string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
			bool negative = (input < 0);
			StringBuilder sb = new StringBuilder();
			if (negative) {
				input = -input;
				sb.Append("-");
			}
			while (input > 0) {
				sb.Insert((negative ? 1 : 0), chars[(int)(input % 36)]);
				input /= 36;
			}
			return sb.ToString();
		}
	}
}
