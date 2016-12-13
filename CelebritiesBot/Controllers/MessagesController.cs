using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;

namespace CelebritiesBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                var cognitiveResponse = await CallCognitiveServices(activity.Attachments[0].ContentUrl);

                var score = cognitiveResponse["categories"][0]["score"].Value<double>();
                string botResponse;
                if (!cognitiveResponse["categories"][0]["detail"]["celebrities"].HasValues)
                {
                    botResponse = "Are you fucking kidding me?";
                }
                else
                {
                    var name = cognitiveResponse["categories"][0]["detail"]["celebrities"][0]["name"].Value<string>();
                    botResponse = score > 0.5 ? $"Maybe: {name}" : "I am not enough smart. Try programming me better"; ;
                }
                
                // return our reply to the user
                Activity reply = activity.CreateReply(botResponse);
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private async Task<JObject> CallCognitiveServices(string imageUrl)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");

            // Request parameters
            queryString["visualFeatures"] = "Faces";
            queryString["details"] = "Celebrities";
            queryString["language"] = "en";
            var uri = "https://api.projectoxford.ai/vision/v1.0/analyze?" + queryString;

            // Request body
            var byteData = await new HttpClient().GetByteArrayAsync(imageUrl);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = await client.PostAsync(uri, content);
                return await response.Content.ReadAsAsync<JObject>();
            }
        }
    }
}