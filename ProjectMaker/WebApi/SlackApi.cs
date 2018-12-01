using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Drawing;
using System.Runtime.InteropServices;

using IPA.DN.CoreUtil.Basic;
using IPA.DN.CoreUtil.Helper.Basic;
using IPA.DN.CoreUtil.Helper.SlackApi;
using System.Net.Http;

namespace IPA.DN.CoreUtil.Helper.SlackApi
{
    public static class SlackApiHelper
    {
        public static DateTime ToDateTimeOfSlack(this decimal value) => Util.UnixTimeToDateTime((uint)value);
        public static DateTime ToDateTimeOfSlack(this long value) => Util.UnixTimeToDateTime((uint)value);
        public static long ToLongDateTimeOfSlack(this DateTime dt) => Util.DateTimeToUnixTime(dt);
    }
}

namespace IPA.DN.CoreUtil.WebApi
{
    public class SlackApi : WebApi
    {
        public class Response : WebResponseBasic
        {
            public bool ok;
            public string error;

            public override void CheckError()
            {
                if (this.ok == false) throw new WebResponseException(error);
            }
        }

        public string ClientId { get; set; }
        public string AccessTokenStr { get; set; }

        public SlackApi(string client_id = "", string access_token = "") : base()
        {
            this.ClientId = client_id;
            this.AccessTokenStr = access_token;
        }

        protected override HttpRequestMessage CreateWebRequest(WebApiMethods method, string url, params (string name, string value)[] query_list)
        {
            HttpRequestMessage r = base.CreateWebRequest(method, url, query_list);

            if (this.AccessTokenStr.IsFilled())
            {
                r.Headers.Add("Authorization", $"Bearer {this.AccessTokenStr.EncodeUrl(this.RequestEncoding)}");
            }

            return r;
        }

        public string AuthGenerateAuthorizeUrl(string scope, string redirect_url, string state = "")
        {
            return "https://slack.com/oauth/authorize?" +
                BuildQueryString(
                    ("client_id", this.ClientId),
                    ("scope", scope),
                    ("redirect_uri", redirect_url),
                    ("state", state));
        }

        public class AccessToken : Response
        {
            public string access_token;
            public string scope;
            public string user_id;
            public string team_name;
            public string team_id;
        }

        public async Task<AccessToken> AuthGetAccessTokenAsync(string client_secret, string code, string redirect_url)
        {
            WebRet ret = await this.RequestWithQuery(WebApiMethods.POST, "https://slack.com/api/oauth.access",
                null,
                ("client_id", this.ClientId),
                ("client_secret", client_secret),
                ("redirect_uri", redirect_url),
                ("code", code));

            AccessToken a = ret.DeserializeAndCheckError<AccessToken>();

            return a;
        }

        public class ChannelsList : Response
        {
            public Channel[] Channels;
        }

        public class Value
        {
            public string value;
            public string creator;
            public long last_set;
        }

        public class Channel
        {
            public string id;
            public string name;
            public bool is_channel;
            public decimal created;
            public string creator;
            public string name_normalized;
            public Value purpose;
        }

        public class PostMessageData
        {
            public string channel;
            public string text;
            public bool as_user;
        }

        public async Task<ChannelsList> GetChannelsListAsync()
        {
            return (await RequestWithQuery(WebApiMethods.POST, "https://slack.com/api/channels.list")).DeserializeAndCheckError<ChannelsList>();
        }

        public async Task PostMessageAsync(string channel_id, string text, bool as_user)
        {
            PostMessageData m = new PostMessageData()
            {
                channel = channel_id,
                text = text,
                as_user = as_user,
            };

            await PostMessageAsync(m);
        }

        public async Task PostMessageAsync(PostMessageData m)
        {
            (await RequestWithJsonObject(WebApiMethods.POST, "https://slack.com/api/chat.postMessage", m)).DeserializeAndCheckError<Response>();
        }
    }
}

