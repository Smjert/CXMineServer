using System;

namespace CXMineServer
{
	public abstract class Entity
	{
	
		public Chunk CurrentChunk{get;protected set;}
		public double X{
			get;
			set;
		}
		public double Y{
			get;
			set;
		}
		public double Z{
			get;
			set;
		}
        public float Yaw{
			get;
			set;
		}
        public float Pitch{
			get;
			set;
		}
		public int EntityID{get;private set;}
		
		public Entity()
		{
			EntityID = Server.getEID();
		}
		
		virtual public void Update()
		{
			Chunk oldChunk = CurrentChunk;
			Chunk newChunk = CXMineServer.Server.World.GetChunkAt((int) X, (int) Z);
			if (oldChunk != newChunk) {
				if (oldChunk != null) oldChunk.Entities.Remove(this);
				newChunk.Entities.Add(this);
				CurrentChunk = newChunk;
			}
		}
		
		public void Remove()
		{
			if (CurrentChunk != null) CurrentChunk.Entities.Remove(this);
		}
		
		override public string ToString()
		{
			return "[Entity " + EntityID + "]";
		}
		
	}
}
