﻿using System;
using System.Linq;
using System.Reflection;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using AWSAppender.CloudWatchLogs.Model;
using AWSAppender.CloudWatchLogs.Parsers;
using AWSAppender.CloudWatchLogs.Services;
using AWSAppender.Core;
using AWSAppender.Core.Layout;
using AWSAppender.Core.Services;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace AWSAppender.CloudWatchLogs
{
    public class CloudWatchLogsAppender : AWSAppenderBase<LogDatum>, ICloudWatchLogsAppender
    {
        private CloudWatchLogsClientWrapper _client;
        private readonly static Type _declaringType = typeof(CloudWatchLogsAppender);

        private AmazonCloudWatchLogsConfig _clientConfig;

        protected override ClientConfig ClientConfig
        {
            get { return _clientConfig ?? (_clientConfig = new AmazonCloudWatchLogsConfig()); }
        }

        public override IEventProcessor<LogDatum> EventProcessor
        {
            get { return _eventProcessor; }
            set { _eventProcessor = value; }
        }

        protected override void ResetClient()
        {
            _client = null;
        }

        public string GroupName { get; set; }

        public string StreamName { get; set; }

        public string Message { get; set; }

        public new string Timestamp { get; set; }

        private IEventProcessor<LogDatum> _eventProcessor;

        public CloudWatchLogsAppender()
        {
            if (Assembly.GetEntryAssembly() != null)
                GroupName = Assembly.GetEntryAssembly().GetName().Name;
            else
                GroupName = "unspecified";

            StreamName = "unspecified";

            var hierarchy = ((Hierarchy)log4net.LogManager.GetRepository());
            var logger = hierarchy.GetLogger("Amazon") as Logger;
            logger.Level = Level.Off;

            hierarchy.AddRenderer(typeof(LogDatum), new LogDatumRenderer());


        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            EventMessageParser = EventMessageParser ?? new LogsEventMessageParser(ConfigOverrides);

            _client = new CloudWatchLogsClientWrapper(EndPoint, AccessKey, Secret, ClientConfig);

            _eventProcessor = new LogEventProcessor(GroupName, StreamName, Timestamp, Message)
                              {
                                  EventMessageParser = EventMessageParser
                              };

            if (Layout == null)
                Layout = new PatternLayout("%message");

        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            LogLog.Debug(_declaringType, "Appending");

            if (!EventRateLimiter.Request(loggingEvent.TimeStamp))
            {
                LogLog.Debug(_declaringType, "Appending denied due to event limiter saturated.");
                return;
            }

            var logDatum = _eventProcessor.ProcessEvent(loggingEvent, RenderLoggingEvent(loggingEvent)).Single();

            _client.AddLogRequest(new PutLogEventsRequest(logDatum.GroupName, logDatum.StreamName, new[] { new InputLogEvent
                                                                                                      {
                                                                                                          Timestamp = logDatum.Timestamp.Value.ToUniversalTime(),
                                                                                                          Message = logDatum.Message
                                                                                                      } }.ToList()));
        }


    }
}