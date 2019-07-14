﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Grpc.Core;
using P4TLBMasterServer;
using project.Messages;
using StackExchange.Redis;

namespace project
{
	class Program
	{
		static void Main(string[] args)
		{
			var port = 4242;

			var mapInstanceImpl = new Dictionary<Type, object>();
			var world = new World(mapInstanceImpl);
			
			var implementations = SearchServiceImplementations(mapInstanceImpl);

			var server = new Server
			{
				Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
			};
			foreach (var impl in implementations)
			{
				server.Services.Add(impl);
			}

			world.GetOrCreateManager<ClientManager>();
			var dbMgr = world.GetOrCreateManager<DatabaseManager>();
			{
				var conf = ConfigurationOptions.Parse("localhost");
				//conf.Password = "topkek";
				
				dbMgr.SetConnection(ConnectionMultiplexer.Connect(conf));
			}
			world.GetImplInstance<AuthenticationImpl>().World = world;

			server.Start();

			FindOrCreateAccount(world);

			Console.WriteLine("The server is currently listening...\nPress a key to exit.");
			Console.ReadKey();
		}

		// TEST
		private static void FindOrCreateAccount(World world)
		{
			var             userDbMgr = world.GetOrCreateManager<UserDatabaseManager>();
			DataUserAccount account;
			
			// no account with login 'guerro' found
			if (userDbMgr.GetIdFromLogin("guerro") == 0)
			{
				// So create one...
				account = userDbMgr.CreateAccount("guerro", out var success);
				
				Console.WriteLine($"New Account Data: {account}");
				
				return;
			}

			account = userDbMgr.FindById(userDbMgr.GetIdFromLogin("guerro"));
			
			Console.Write($"Existing Account Data: {account}");
		}

		private static IEnumerable<ServerServiceDefinition> SearchServiceImplementations(Dictionary<Type, object> mapInstanceImpl)
		{
			//var result = new List<(Type binder, Type impl)>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			return
			(
				from types
					in assemblies.Select(a => a.DefinedTypes)
				from type
					in types
				let attr = type.GetCustomAttribute<ImplementationAttribute>()
				where attr != null
				let binder = attr.Binder
				let bindMethod = binder.GetMethods().First(m => m.Name == "BindService" && m.GetParameters().Length == 1)
				let instance = mapInstanceImpl[type] = Activator.CreateInstance(type)
				select (ServerServiceDefinition) bindMethod.Invoke(null, new[] {instance})
			).ToList();
		}
	}
}