﻿using System.Net.Http;
using Ray.Serilog.Sinks.Batched;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
namespace Ray.Serilog.Sinks.PushPlusBatched
{
    public class PushPlusApiClient : PushService
    {
        //http://www.pushplus.plus/doc/

        private const string Host = "http://www.pushplus.plus/send";

        private readonly Uri _apiUrl;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _token;
        private readonly string _topic;
        private readonly string _channel;
        private readonly string _webhook;

        public PushPlusApiClient(
            string token,
            string topic = null,
            string channel = "",
            string webhook = ""
            )
        {
            _apiUrl = new Uri(Host);
            _token = token;
            _topic = topic;
            _channel = channel;
            _webhook = webhook;
        }

        public override string ClientName => "PushPlus";

        private PushPlusChannelType ChannelType
        {
            get
            {
                var re = PushPlusChannelType.wechat;

                if (_channel.IsNullOrEmpty()) return re;

                bool suc = Enum.TryParse<PushPlusChannelType>(_channel, true, out PushPlusChannelType channel);
                if (suc) re = channel;

                return re;
            }
        }

        protected override string NewLineStr => "<br/>";

        public override HttpResponseMessage DoSend()
        {
            //提取字数 防止超越字数限制
            int amount=20000;
            Msg = Msg.Replace(" ", "");
            Msg = Msg.Replace("　", "");
            Msg = Msg.Replace("\r\n", "\n");
            Msg = Msg.Replace("\n\n", "\n");
            amount = Msg.Length>amount?amount:Msg.Length;
            
            //debug
            SelfLog.WriteLine(Msg.Length.ToString());
            SelfLog.WriteLine(amount.ToString());
            SelfLog.WriteLine(Msg.Substring(0,amount));
            var json = new
            {
                token = _token,

                topic = _topic,
                channel = this.ChannelType.ToString(),
                webhook = _webhook,

                title = Title,
                content = Msg.Substring(0,amount),

                template = PushPlusMsgType.html.ToString()
            }.ToJson();

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync(_apiUrl, content).GetAwaiter().GetResult();
            return response;
        }
    }

    public enum PushPlusMsgType
    {
        html,
        json,
        markdown,
        cloudMonitor,
        jenkins,
        route
    }

    public enum PushPlusChannelType
    {
        wechat,
        webhook,
        cp,
        sms,
        mail
    }
}
