﻿using System;

namespace StatLight.Core.Reporting.Providers.TeamCity
{
	using StatLight.Core.Reporting.Messages;

	internal class TeamCityTestResultHandler : ITestResultHandler
	{
		private readonly ICommandWriter messageWriter;
		private readonly string assemblyName;

		public TeamCityTestResultHandler(ICommandWriter messageWriter, string assemblyName)
		{
			this.messageWriter = messageWriter;
			this.assemblyName = assemblyName;
		}

		public void PublishStart()
		{
			messageWriter.Write(
				CommandFactory.TestSuiteStarted(assemblyName));
		}

		public void PublishStop()
		{
			messageWriter.Write(
				CommandFactory.TestSuiteFinished(assemblyName));
		}

		public void HandleMessage(MobilScenarioResult result)
		{
			var name = result.TestClassName + "." + result.TestName;
			var durationMilliseconds = result.TimeToComplete.Milliseconds;

			WrapMessageWithStartAndEnd(() =>
			{
				if (result.Result == TestOutcome.Failed ||
					result.Result == TestOutcome.Aborted ||
					result.Result == TestOutcome.Disconnected ||
					result.Result == TestOutcome.Error ||
					result.Result == TestOutcome.Inconclusive ||
					result.Result == TestOutcome.Timeout)
				{
					messageWriter.Write(
						CommandFactory.TestFailed(
							name,
							result.TraceMessage(),
							result.TraceMessage()));
				}
			}, name, durationMilliseconds);
		}

		private void WrapMessageWithStartAndEnd(Command command, string name, long durationMilliseconds)
		{
			WrapMessageWithStartAndEnd(() => messageWriter.Write(command), name, durationMilliseconds);
		}

		private void WrapMessageWithStartAndEnd(Action action, string name, long durationMilliseconds)
		{
			messageWriter.Write(CommandFactory.TestStarted(name));
			action();
			messageWriter.Write(CommandFactory.TestFinished(name, durationMilliseconds));
		}

		public void HandleMessage(MobilOtherMessageType result)
		{
			if (result.IsIgnoreMessage())
			{
				WrapMessageWithStartAndEnd(CommandFactory.TestIgnored(result.Message, string.Empty), result.Message, 0);
			}
			//if (otherResult.MessageType == LogMessageType.Error)
			//{
			//    messageWriter.Write(
			//        CommandFactory.TestStarted(assemblyName));

			//    if (otherResult.MessageType == LogMessageType.Error)
			//    {
			//        messageWriter.Write(
			//            CommandFactory.TestFailed(
			//                assemblyName,
			//                otherResult.TraceMessage(),
			//                otherResult.TraceMessage()));
			//    }

			//    messageWriter.Write(
			//        CommandFactory.TestFinished(assemblyName, 0));
			//}
		}



	}
}