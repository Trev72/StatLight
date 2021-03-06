﻿using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using StatLight.Core.WebBrowser;

#if SILVERLIGHT
using StatLight.Client.Harness.Hosts;
#endif
namespace StatLight.Core.Configuration
{
    [DataContract]
    public class ClientTestRunConfiguration
    {
        private string _tagFilters = string.Empty;
        private Collection<string> _methodsToTest;
        private Collection<string> _testAssemblyFormalNames;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "4#", Justification = "System.Uri is not DataContract serializable")]
        public ClientTestRunConfiguration(UnitTestProviderType unitTestProviderType, IEnumerable<string> methodsToTest, string tagFilters, int numberOfBrowserHosts, WebBrowserType webBrowserType, string entryPointAssembly, WindowGeometry windowGeometry, IEnumerable<string> testAssemblyFormalNames)
        {
            if (methodsToTest == null) throw new ArgumentNullException("methodsToTest");
            if (entryPointAssembly == null) throw new ArgumentNullException("entryPointAssembly");
            if (unitTestProviderType == UnitTestProviderType.Undefined)
                throw new ArgumentException("Must be defined", "unitTestProviderType");

            if (numberOfBrowserHosts <= 0)
                throw new ArgumentOutOfRangeException("numberOfBrowserHosts", "Must be greater than 0");

            //if (testAssemblyFormalNames.Count() == 0)
            //    throw new ArgumentException("must have some assemblies specified", "testAssemblyFormalNames");

            _methodsToTest = methodsToTest.ToCollection();
            _tagFilters = tagFilters ?? string.Empty;
            UnitTestProviderType = unitTestProviderType;
            NumberOfBrowserHosts = numberOfBrowserHosts;
            WebBrowserType = webBrowserType;
            EntryPointAssembly = entryPointAssembly;
            WindowGeometry = windowGeometry;
            _testAssemblyFormalNames = new Collection<string>(testAssemblyFormalNames.ToList());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [DataMember]
        public Collection<string> TestAssemblyFormalNames
        {
            get { return (_testAssemblyFormalNames ?? (_testAssemblyFormalNames = new Collection<string>())); }
            set { _testAssemblyFormalNames = value; }
        }

        [DataMember]
        public string EntryPointAssembly { get; set; }

        [DataMember]
        public WebBrowserType WebBrowserType { get; set; }

        [DataMember]
        public int NumberOfBrowserHosts { get; set; }


        [DataMember]
        public string TagFilter
        {
            get
            {
                return _tagFilters;
            }
            set
            {
                _tagFilters = (value ?? string.Empty);
            }
        }

        [DataMember]
        public UnitTestProviderType UnitTestProviderType { get; set; }

        [DataMember]
        public Collection<string> MethodsToTest
        {
            get { return (_methodsToTest ?? (_methodsToTest = new Collection<string>())); }
            set { _methodsToTest = value; }
        }

        [DataMember]
        public WindowGeometry WindowGeometry { get; set; }


#if SILVERLIGHT
        private static ClientTestRunConfiguration _currentClientTestRunConfiguration;

        public static ClientTestRunConfiguration CurrentClientTestRunConfiguration
        {
            get { return _currentClientTestRunConfiguration; }
            set
            {
                _currentClientTestRunConfiguration = value;

                //var expectedTestsToFindAndRunMessage = string.Join(
                //    " *** Method Filter: {0}".FormatWith(Environment.NewLine),
                //            _currentClientTestRunConfiguration.MethodsToTest.ToArray());
                //StatLight.Client.Harness.Server.Debug(expectedTestsToFindAndRunMessage);

            }
        }

        private static int? _instanceNumber;

        private static readonly IEqualityComparer<string> _ignoreCaseStringComparer =
            StringComparer.Create(CultureInfo.InvariantCulture, true);

        public static bool IsTestExplicit(MemberInfo memberInfo)
        {
            if (CurrentClientTestRunConfiguration.MethodsToTest.Count == 1)
                if (ContainsMethod(memberInfo))
                    return true;

            return false;
        }

        public static bool ContainsMethod(MemberInfo memberInfo)
        {
            if (CurrentClientTestRunConfiguration == null)
                return false;
            if (memberInfo == null)
                throw new ArgumentNullException("memberInfo");

            var methodName = memberInfo.FullName();
            if (CurrentClientTestRunConfiguration.MethodsToTest.Count == 0)
                return ShouldItBeRunInThisInstance(methodName);

            var containsMethod = CurrentClientTestRunConfiguration.MethodsToTest.Contains(methodName, _ignoreCaseStringComparer);
            return containsMethod;
        }

        public static int InstanceNumber
        {
            get
            {
                if (!_instanceNumber.HasValue)
                {
                    const string instanceNumber = "InstaneNumber";
#if !WINDOWS_PHONE
                    if (System.Windows.Application.Current.Host.InitParams.ContainsKey(instanceNumber))
                    {
                        string initParam = System.Windows.Application.Current.Host.InitParams[instanceNumber];
                        _instanceNumber = int.Parse(initParam);
                    }
                    else
#endif
                    {
                        _instanceNumber = 1;
                    }
                }
                return _instanceNumber.Value;
            }
        }

        private static bool ShouldItBeRunInThisInstance(string methodName)
        {
            if (CurrentClientTestRunConfiguration.NumberOfBrowserHosts <= 1)
            {
                return true;
            }

            int methodNameHashCode = Math.Abs(methodName.GetHashCode());

            int moddedHash = methodNameHashCode % CurrentClientTestRunConfiguration.NumberOfBrowserHosts;

            return moddedHash == InstanceNumber;
        }
#endif
    }
}
