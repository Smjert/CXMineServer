using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CXMineServer
{
	public class Item
	{
		private Chunk _ParentChunk;
		public Chunk ParentChunk
		{
			get { return _ParentChunk; }
			set { _ParentChunk = value;}
		}

		public int Type
		{
			get;
			set;
		}

		public int Uses
		{
			get;
			set;
		}

		public int EId
		{
			get;
			set;
		}
		public int X
		{
			get;
			set;
		}
		public int Y
		{
			get;
			set;
		}
		public int Z
		{
			get;
			set;
		}
		public float Yaw
		{
			get;
			set;
		}
		public float Pitch
		{
			get;
			set;
		}

		public Item(Chunk c)
		{
			_ParentChunk = c;
		}

		public void Delete()
		{
			_ParentChunk.DeleteItem(this);
		}
	}
}
