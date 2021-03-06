﻿
namespace StatLight.Core.Reporting.Providers.TFS.TFS2010
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using StatLight.Core.Events;

    public class MSGenericTestXmlReport : IXmlReport
    {
        private readonly TestReportCollection _report;

        public MSGenericTestXmlReport(TestReportCollection report)
        {
            if (report == null)
                throw new ArgumentNullException("report");

            _report = report;
        }

        public void WriteXmlReport(string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                writer.Write(GetXmlReport());
            }
        }

        public string GetXmlReport()
        {
            Func<IEnumerable<TestCaseResultServerEvent>, IEnumerable<XElement>> getInnerTests = result =>
                result.Select(s =>
                    new XElement("InnerTest",
                        new XElement("TestName", s.FullMethodName()),
                        new XElement("TestResult", GetTestResult(s.ResultType)),
                        new XElement("ErrorMessage", GetErrorMessage(s))
                        ));

            var firstReport = _report.First();
            var report = new XElement("SummaryResult",
                                      new XElement("TestName", "StatLight Tests"),
                                      new XElement("TestResult", GetFinalResult()),
                                      new XElement("InnerTests", getInnerTests(firstReport.TestResults)));
            return report.ToString();
        }

        private static TestResultType GetTestResult(ResultType resultType)
        {
            switch (resultType)
            {
                case ResultType.Passed:
                    return TestResultType.Passed;
                case ResultType.Ignored:
                    return TestResultType.NotExecuted;
                default:
                    return TestResultType.Failed;
            }
        }

        private TestResultType GetFinalResult()
        {
            if (_report.FinalResult == RunCompletedState.Failure)
            {
                return TestResultType.Failed;
            }
            return TestResultType.Passed;
        }

        private static string GetErrorMessage(TestCaseResultServerEvent resultServerEvent)
        {
            var sb = new StringBuilder();
            if (resultServerEvent.ExceptionInfo != null)
                sb.Append(resultServerEvent.ExceptionInfo.FullMessage);

            if (!string.IsNullOrEmpty(resultServerEvent.OtherInfo))
            {
                sb.Append(Environment.NewLine);
                sb.Append(resultServerEvent.OtherInfo);
            }

            var rtn = sb.ToString();

            return string.IsNullOrEmpty(rtn) ? null : rtn;
        }
    }
}
