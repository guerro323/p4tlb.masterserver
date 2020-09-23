﻿using System;
using System.Collections.Generic;
using System.Threading;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Execution;
using GameHost.Threading;
using GameHost.Worlds;

namespace project.Core.Systems
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class AddApplicationSystem : AppSystem
	{
		private GlobalWorld             globalWorld;

		public AddApplicationSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref globalWorld);
		}
		
		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			AddApp("MasterServer", new MasterServerApplication(globalWorld, null!));
		}

		private Entity AddApp<T>(string name, T app)
			where T : class, IApplication, IListener
		{
			var listener = World.Mgr.CreateEntity();
			listener.Set<ListenerCollectionBase>(new ListenerCollection());

			var systems = new List<Type>();
			AppSystemResolver.ResolveFor<T>(systems);

			foreach (var type in systems)
				app.Data.Collection.GetOrCreate(type);

			var applicationEntity = World.Mgr.CreateEntity();
			applicationEntity.Set<IListener>(app);
			applicationEntity.Set(new PushToListenerCollection(listener));
			return applicationEntity;
		}
	}
}