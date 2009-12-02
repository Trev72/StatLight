﻿
using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StatLight.Client.Silverlight.UnitTestProviders.NUnit;

namespace StatLight.Client.Silverlight.Tests.UnitTestProviders.NUnit
{
	[TestClass]
	[Ignore]
	public class NUnitTestProviderTests : FixtureBase
	{
		IUnitTestProvider provider;
		protected override void Before_each_test()
		{
			base.Before_each_test();

			provider = new NUnitTestProvider();
		}

		[TestMethod]
		public void provider_should_support_MethodCanIgnore()
		{
			provider
				.HasCapability(UnitTestProviderCapabilities.MethodCanIgnore)
				.ShouldBeTrue();
		}

		[TestMethod]
		public void provider_should_support_MethodCanHaveTimeout()
		{
			provider
				.HasCapability(UnitTestProviderCapabilities.MethodCanHaveTimeout)
				.ShouldBeTrue();
		}
	}
}