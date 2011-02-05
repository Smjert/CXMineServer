using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CXMineServer.Utils
{
	public static class Utility
	{
		public static bool IsInRange(Player p, Item i, int range)
		{
			int fromX = (int)p.X * 32;
			int fromZ = (int)p.Z * 32;

			return IsInRange(fromX, fromZ, i.X, i.Z, range);
		}

		public static bool IsInRange(int fX, int fZ, int tX, int tZ, int range)
		{
			return (fX >= (tX - range))
				&& (fX <= (tX + range))
				&& (fZ >= (tZ - range))
				&& (fZ <= (tZ + range));
		}

		public static bool IsInRange(Player p, Item i, int range, out int distance)
		{
			int fromX = (int)p.X * 32;
			int fromZ = (int)p.Z * 32;

			return IsInRange(fromX, fromZ, i.X, i.Z, range, out distance);
		}

		public static bool IsInRange(Player p, int toX, int toZ, int range, out int distance)
		{
			int fromX = (int)(p.X * 32.0);
			int fromZ = (int)(p.Z * 32.0);

			return IsInRange(fromX, fromZ, toX, toZ, range, out distance);
		}

		public static bool IsInRange(int fX, int fZ, int tX, int tZ, int range, out int distance)
		{
			int distanceX = Math.Abs(tX - fX);
			int distanceZ = Math.Abs(tZ - fZ);

			distance = distanceX + distanceZ;

			return (fX >= (tX - range))
				&& (fX <= (tX + range))
				&& (fZ >= (tZ - range))
				&& (fZ <= (tZ + range));
		}

		public static int DistanceBetweenEntities(Player p, int toX, int toZ)
		{
			int fromX = (int)(p.X * 32.0);
			int fromZ = (int)(p.Z * 32.0);

			return DistanceBetweenEntities(fromX, fromZ, toX, toZ);
		}

		public static int DistanceBetweenEntities(int fX, int fZ, int tX, int tZ)
		{
			int distanceX = Math.Abs(tX - fX);
			int distanceZ = Math.Abs(tZ - fZ);

			return distanceX*distanceX + distanceZ*distanceZ;
		}
	}
}
