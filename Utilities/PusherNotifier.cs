﻿using PusherServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class PusherNotifier
    {
        private Pusher _pusher { get; set; }

        public PusherNotifier()
        {
            const string appId = "148580";
            const string appKey = "123dd339500fed0ddd78";
            const string appSecret = "598b1fdcdf903325d520";
            _pusher = new Pusher(appId, appKey, appSecret, new PusherOptions() { Encrypted = true });
        }

        public void Notify(object message)
        {
            var result = _pusher.Trigger("test_channel", "my_event", message);
        }
    }
}
