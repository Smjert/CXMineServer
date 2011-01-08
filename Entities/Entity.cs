using System;

namespace CXMineServer
{
	public abstract class Entity
	{
	
		public Chunk CurrentChunk;
		public double X;
		public double Y;
		public double Z;
        public float Yaw;
        public float Pitch;
		public int EntityID;
		
		public Entity()
		{
			CurrentChunk = null;
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
