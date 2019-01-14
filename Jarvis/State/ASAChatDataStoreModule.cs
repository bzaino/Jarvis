using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using System.Configuration;
using System.Reflection;
using Module = Autofac.Module;

namespace Jarvis.State
{
    public class ASAChatDataStoreModule : Module
    {
        public static readonly object KeyDataStore = new object();

      
        public ASAChatDataStoreModule(Assembly assembly)
        {
           
            SetField.NotNull(out assembly, nameof(assembly), assembly);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConnectorStore>()
                .AsSelf()
                .InstancePerLifetimeScope();


            ASAChatDataContext.AssertDatabaseReady();

            var store = new ASAChatDataStore(ConfigurationManager.ConnectionStrings["ASAChatDataContextConnectionString"]
                .ConnectionString);


            builder.Register(c => store)
                .Keyed<IBotDataStore<BotData>>(KeyDataStore)
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new CachingBotDataStore(c.ResolveKeyed<IBotDataStore<BotData>>(KeyDataStore),
                    CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                .As<IBotDataStore<BotData>>()
                .AsSelf()
                .InstancePerLifetimeScope();    
        }
    }
}
