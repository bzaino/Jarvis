using System.Data.Entity.Infrastructure;

namespace Jarvis.State
{
    public class ASAChatDataContextFactory : IDbContextFactory<ASAChatDataContext>
    {
        public ASAChatDataContext Create()
        {
            return new ASAChatDataContext("BotDataContextConnectionString");
        }
    }
}