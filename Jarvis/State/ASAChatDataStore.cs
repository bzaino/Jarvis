using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System;
using System.Linq;
using Jarvis.Models;

namespace Jarvis.State
{
    public class ASAChatDataStore : IBotDataStore<BotData>
    {
        private readonly string _connectionString;

        public ASAChatDataStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<BotData> LoadAsync(IAddress key, BotStoreType botStoreType, CancellationToken cancellationToken)
        {
            using (var context = new ASAChatDataContext(_connectionString))
            {
                try
                {
                    var entity = await ASAChatDataEntity.GetASAChatDataEntity(key, botStoreType, context);

                    return entity == null ? new BotData(string.Empty) : new BotData(entity.ETag, entity.GetData());

                    // return botdata 
                }
                catch (System.Data.SqlClient.SqlException err)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, err.Message);
                }
            }
        }

        public async Task SaveAsync(IAddress key, BotStoreType botStoreType, BotData botData, CancellationToken cancellationToken)
        {
            var entity = new ASAChatDataEntity(botStoreType, key.BotId, key.ChannelId, key.ConversationId, key.UserId, botData.Data)
            {
                ETag = botData.ETag,
                ServiceUrl = key.ServiceUrl
            };

            using (var context = new ASAChatDataContext(_connectionString))
            {
                try
                {
                    if (string.IsNullOrEmpty(botData.ETag))
                    {
                        context.BotData.Add(entity);
                    }
                    else if (entity.ETag == "*")
                    {
                        var foundData = await ASAChatDataEntity.GetASAChatDataEntity(key, botStoreType, context);
                        if (botData.Data != null)
                        {
                            if (foundData == null)
                                context.BotData.Add(entity);
                            else
                            {
                                foundData.Data = entity.Data;
                                foundData.ServiceUrl = entity.ServiceUrl;
                            }
                        }
                        else
                        {
                            if (foundData != null)
                                context.BotData.Remove(foundData);
                        }
                    }
                    else
                    {
                        var foundData = await ASAChatDataEntity.GetASAChatDataEntity(key, botStoreType, context);
                        if (botData.Data != null)
                        {
                            if (foundData == null)
                                context.BotData.Add(entity);
                            else
                            {
                                foundData.Data = entity.Data;
                                foundData.ServiceUrl = entity.ServiceUrl;
                                foundData.ETag = entity.ETag;
                            }
                        }
                        else
                        {
                            if (foundData != null)
                                context.BotData.Remove(foundData);
                        }
                    }
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (System.Data.SqlClient.SqlException err)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, err.Message);
                }
            }
        }

        public Task<bool> FlushAsync(IAddress key, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public async Task LogActivity(Activity activity)
        {
            using (var context = new ASAChatDataContext(_connectionString))
            {
                int maxLength = Math.Min(activity.Text.Length, 500);

                // Create a new UserLog object
                UserLog newUserLog = new UserLog();
                // Set the properties on the UserLog object
                newUserLog.Channel = activity.ChannelId;
                newUserLog.UserID = activity.From.Id;
                newUserLog.UserName = activity.From.Name;
                newUserLog.created = DateTime.UtcNow;
                newUserLog.Message = activity.Text.Substring(0, maxLength);

                context.SaveChanges();
                // Add the UserLog object to UserLogs
                //db.UserLogs.Add(newUserLog);
                //// Save the changes to the database
                //db.SaveChanges();
            }
        }

        
    }
}