﻿
using StatLight.Client.Harness.Events;
using StatLight.Core.Events.Aggregation;

namespace StatLight.Core.Runners
{
    using System;
    using System.Threading;
    using StatLight.Core.Common;
    using StatLight.Core.Reporting;
    using StatLight.Core.WebBrowser;
    using StatLight.Core.WebServer;
    using StatLight.Core.Events;

    internal class OnetimeRunner : IRunner, IDisposable,
        IListener<TestRunCompletedServerEvent>
    {
        private readonly ILogger logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWebServer statLightServiceHost;
        private readonly IBrowserFormHost browserFormHost;
        private readonly TestResultAggregator testResultAggregator;
        readonly AutoResetEvent _browserThreadWaitHandle = new AutoResetEvent(false);


        internal OnetimeRunner(
            ILogger logger,
            IEventAggregator eventAggregator,
            IWebServer statLightServiceHost,
            IBrowserFormHost browserFormHost)
        {
            this.logger = logger;
            _eventAggregator = eventAggregator;
            this.statLightServiceHost = statLightServiceHost;
            this.browserFormHost = browserFormHost;

            testResultAggregator = new TestResultAggregator(logger, eventAggregator);
            eventAggregator.AddListener(testResultAggregator);
            eventAggregator.AddListener(this);
        }

        public virtual TestReport Run()
        {

            logger.Information("{1}{1}Starting Test Run: {0}{1}{1}"
                .FormatWith(DateTime.Now, Environment.NewLine));

            statLightServiceHost.Start();
            browserFormHost.Start();
            _browserThreadWaitHandle.WaitOne();
            browserFormHost.Stop();
            statLightServiceHost.Stop();

            logger.Information("{1}{1}--- Completed Test Run: {0}{1}{1}"
                .FormatWith(DateTime.Now, Environment.NewLine));

            return testResultAggregator.CurrentReport;
        }

        protected virtual void Dispose(bool disposing)
        {
            browserFormHost.Dispose();
            _eventAggregator.RemoveListener(this);
            if (disposing)
            {
                testResultAggregator.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Handle(TestRunCompletedServerEvent message)
        {
            _browserThreadWaitHandle.Set();
        }
    }
}
