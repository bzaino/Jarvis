using Autofac;
using Jarvis.BotOverrides.Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Reflection;
using Microsoft.Bot.Builder.Azure;
using System.Web.Http;

namespace Jarvis
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var store = new InMemoryDataStore();

            Conversation.UpdateContainer(
                       builder =>
                       {
                           builder.Register(c => store)
                                     .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                                     .AsSelf()
                                     .SingleInstance();

                           builder.Register(c => new CachingBotDataStore(store,
                                      CachingBotDataStoreConsistencyPolicy
                                      .ETagBasedConsistency))
                                      .As<IBotDataStore<BotData>>()
                                      .AsSelf()
                                      .InstancePerLifetimeScope();


                       });


            //Conversation.UpdateContainer(builder =>
            //{
            //    builder.RegisterModule(new DefaultExceptionMessageOverrideModule());
            //});

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
