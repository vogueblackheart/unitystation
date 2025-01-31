using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TileManagement;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace TileMap.Behaviours
{

	public abstract class ItemMatrixSystemInit : NetworkBehaviour, IInitialiseSystem
	{

		public virtual int Priority => 0;

		public virtual void Initialize() { }

		[NonSerialized] protected MetaTileMap MetaTileMap;
		[NonSerialized] protected MatrixSystemManager subsystemManager;
		[NonSerialized] protected TileChangeManager tileChangeManager;
		[NonSerialized] protected NetworkedMatrix NetworkedMatrix;
		public virtual void Awake()
		{
			MetaTileMap = GetComponentInParent<MetaTileMap>();
			tileChangeManager = GetComponentInParent<TileChangeManager>();
			subsystemManager = GetComponentInParent<MatrixSystemManager>();
			NetworkedMatrix = GetComponentInParent<NetworkedMatrix>();
			subsystemManager.Register(this);
		}

		public virtual void OnDestroy()
		{
			MetaTileMap = null;
			tileChangeManager = null;
			NetworkedMatrix = null;
			subsystemManager = null;

		}
	}

}
