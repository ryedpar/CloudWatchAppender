using System;
using System.IO;
using System.Runtime.CompilerServices;
using CloudWatchAppender.Services;
using log4net.Core;
using log4net.Layout.Pattern;

[assembly: InternalsVisibleTo("MetaDataTester")]

namespace CloudWatchAppender.PatternConverter
{
    internal sealed class InstanceMetaDataPatternConverter : PatternLayoutConverter, IOptionHandler
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (string.IsNullOrEmpty(Option))
                throw new InvalidOperationException("The option must be set. Example: metadata{instanceid}.");

            bool error;
            var s = InstanceMetaDataReader.Instance.GetMetaData(Option, out error);

            if (error)
                loggingEvent.Properties["IsqsAppender.MetaData." + MetaDataKeys.instanceid + ".Error"] = "error";

            if (string.IsNullOrEmpty(s))
                writer.Write(Option + ":error");

            writer.Write(s);
        }

        public void ActivateOptions()
        {
            var p = Option;
        }
    }
}