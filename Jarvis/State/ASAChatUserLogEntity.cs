using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Jarvis.State
{
    public class ASAChatUserLogEntity
    {
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };
        internal ASAChatUserLogEntity() { created = DateTime.UtcNow; }
        internal ASAChatUserLogEntity(string userId, string userName, string channel, string message)
        {
            UserID = userId;
            UserName = userName;
            Channel = channel;
            Message = message;
            //Data = Serialize(data);
            created = DateTime.UtcNow;
        }


        #region Fields

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(150)]
        public string UserID { get; set; }
        [MaxLength(150)]
        public string UserName { get; set; }

        [MaxLength(150)]
        public string Channel { get; set; }

        public DateTime created { get; set; }

        [MaxLength(500)]
        public string Message { get; set; }


        #endregion Fields

        #region Methods

        private static byte[] Serialize(object data)
        {
            using (var cmpStream = new MemoryStream())
            using (var stream = new GZipStream(cmpStream, CompressionMode.Compress))
            using (var streamWriter = new StreamWriter(stream))
            {
                var serializedJSon = JsonConvert.SerializeObject(data, SerializationSettings);
                streamWriter.Write(serializedJSon);
                streamWriter.Close();
                stream.Close();
                return cmpStream.ToArray();
            }
        }

        private static object Deserialize(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var gz = new GZipStream(stream, CompressionMode.Decompress))
            using (var streamReader = new StreamReader(gz))
            {
                return JsonConvert.DeserializeObject(streamReader.ReadToEnd());
            }
        }

        //internal TObject GetData<TObject>()
        //{
        //    return ((JObject)Deserialize(Data)).ToObject<TObject>();
        //}

        //internal object GetData()
        //{
        //    return Deserialize(Data);
        //}
        //internal static async Task<ASAChatUserLogEntity> GetASAChatDataEntity(IAddress key, BotStoreType botStoreType, ASAChatDataContext context)
        //{
        //    ASAChatUserLogEntity entity = null;
        //    var query = context.BotData.OrderByDescending(d => d.Timestamp);
        //    switch (botStoreType)
        //    {
        //        case BotStoreType.BotConversationData:
        //            entity = await query.FirstOrDefaultAsync(d => d.BotStoreType == botStoreType
        //                                            && d.ChannelId == key.ChannelId
        //                                            && d.ConversationId == key.ConversationId);
        //            break;
        //        case BotStoreType.BotUserData:
        //            entity = await query.FirstOrDefaultAsync(d => d.BotStoreType == botStoreType
        //                                            && d.ChannelId == key.ChannelId
        //                                            && d.UserId == key.UserId);
        //            break;
        //        case BotStoreType.BotPrivateConversationData:
        //            entity = await query.FirstOrDefaultAsync(d => d.BotStoreType == botStoreType
        //                                            && d.ChannelId == key.ChannelId
        //                                            && d.ConversationId == key.ConversationId
        //                                            && d.UserId == key.UserId);
        //            break;
        //        default:
        //            throw new ArgumentException("Unsupported bot store type!");
        //    }

        //    return entity;
        //}
        #endregion
    }
}
