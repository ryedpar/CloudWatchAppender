using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using log4net.Util;
using SQSAppender.Model;
using SQSAppender.Parsers;
using PatternParser = AWSAppender.Core.Services.PatternParser;

namespace SQSAppender.Services
{
#if NET35
    public interface IEventProcessor<T>
#else
    public interface IEventProcessor<T>
#endif
    {
        //to core
        IEnumerable<T> ProcessEvent(LoggingEvent loggingEvent, string renderedString);
        IEventMessageParser<T> EventMessageParser { get; set; }
    }

    public class SQSEventProcessor : IEventProcessor<SQSDatum>
    {
        private bool _dirtyParsedProperties = true;
        private string _parsedQueueName;
        private string _parsedMessage;
        private DateTime? _dateTimeOffset;
        private readonly string _queueName;
        private readonly string _message;
        private string _id;
        private string _parsedId;

        public SQSEventProcessor(string queueName, string message)
        {
            _queueName = queueName;
            _message = message;
        }


        public IEnumerable<SQSDatum> ProcessEvent(LoggingEvent loggingEvent, string renderedString)
        {
            var patternParser = new PatternParser(loggingEvent);

            if (renderedString.Contains("%"))
                renderedString = patternParser.Parse(renderedString);

            LogLog.Debug(_declaringType, string.Format("RenderedString: {0}", renderedString));

            if (_dirtyParsedProperties)
            {
                ParseProperties(patternParser);

                if (!loggingEvent.Properties.GetKeys().Any(key => key.StartsWith("IsqsAppender.MetaData.") && key.EndsWith(".Error")))
                    _dirtyParsedProperties = false;
            }

            var eventMessageParser = EventMessageParser as ISQSEventMessageParser;

            eventMessageParser.DefaultQueueName = _parsedQueueName;
            eventMessageParser.DefaultMessage = _parsedMessage;
            eventMessageParser.DefaultID = _parsedId;

            return eventMessageParser.Parse(renderedString);
        }

        public IEventMessageParser<SQSDatum> EventMessageParser { get; set; }

        private void ParseProperties(PatternParser patternParser)
        {
            _parsedQueueName = string.IsNullOrEmpty(_queueName)
                ? null
                : patternParser.Parse(_queueName);

            _parsedMessage = string.IsNullOrEmpty(_message)
                ? null
                : patternParser.Parse(_message);

            _parsedId = string.IsNullOrEmpty(_id)
                ? Guid.NewGuid().ToString()
                : patternParser.Parse(_id);
        }

        private readonly static Type _declaringType = typeof(SQSEventProcessor);
    }
}