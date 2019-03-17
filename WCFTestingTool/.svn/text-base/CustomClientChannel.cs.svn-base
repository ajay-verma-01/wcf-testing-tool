using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

namespace WCFTestingTool
{
    /// <summary>
    /// Custom client channel. Allows to specify a different configuration file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CustomClientChannel<T> : ChannelFactory<T>
    {
        readonly string _configurationPath;
        readonly string _endpointConfigurationName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(string configurationPath)
            : base(typeof(T))
        {
            _configurationPath = configurationPath;
            InitializeEndpoint((string)null, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(Binding binding, string configurationPath)
            : this(binding, (EndpointAddress)null, configurationPath)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceEndpoint"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(ServiceEndpoint serviceEndpoint, string configurationPath)
            : base(typeof(T))
        {
            _configurationPath = configurationPath;
            InitializeEndpoint(serviceEndpoint);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpointConfigurationName"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(string endpointConfigurationName, string configurationPath)
            : this(endpointConfigurationName, null, configurationPath)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="endpointAddress"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(Binding binding, EndpointAddress endpointAddress, string configurationPath)
            : base(typeof(T))
        {
            _configurationPath = configurationPath;
            InitializeEndpoint(binding, endpointAddress);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="remoteAddress"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(Binding binding, string remoteAddress, string configurationPath)
            : this(binding, new EndpointAddress(remoteAddress), configurationPath)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpointConfigurationName"></param>
        /// <param name="endpointAddress"></param>
        /// <param name="configurationPath"></param>
        public CustomClientChannel(string endpointConfigurationName, EndpointAddress endpointAddress, string configurationPath)
            : base(typeof(T))
        {
            _configurationPath = configurationPath;
            _endpointConfigurationName = endpointConfigurationName;
            InitializeEndpoint(endpointConfigurationName, endpointAddress);
        }

        /// <summary>
        /// Loads the serviceEndpoint description from the specified configuration file
        /// </summary>
        /// <returns></returns>
        protected override ServiceEndpoint CreateDescription()
        {
            var serviceEndpoint = base.CreateDescription();

            if (_endpointConfigurationName != null)
                serviceEndpoint.Name = _endpointConfigurationName;

            var map = new ExeConfigurationFileMap {ExeConfigFilename = _configurationPath};

            var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            var group = ServiceModelSectionGroup.GetSectionGroup(config);

            ChannelEndpointElement selectedEndpoint = null;

            if (group != null)
            {
                foreach (ChannelEndpointElement endpoint in group.Client.Endpoints)
                {
                    if (endpoint.Contract.Substring(endpoint.Contract.LastIndexOf('.') + 1) !=
                        serviceEndpoint.Contract.ConfigurationName ||
                        (_endpointConfigurationName != null && _endpointConfigurationName != endpoint.Name)) continue;
                    selectedEndpoint = endpoint;
                    break;
                }

                if (selectedEndpoint != null)
                {
                    if (serviceEndpoint.Binding == null)
                    {
                        serviceEndpoint.Binding = CreateBinding(selectedEndpoint, group);
                    }

                    if (serviceEndpoint.Address == null)
                    {
                        serviceEndpoint.Address = new EndpointAddress(selectedEndpoint.Address, GetIdentity(selectedEndpoint.Identity), selectedEndpoint.Headers.Headers);
                    }

                    if (serviceEndpoint.Behaviors.Count == 0 && selectedEndpoint.BehaviorConfiguration != null)
                    {
                        AddBehaviors(selectedEndpoint.BehaviorConfiguration, serviceEndpoint, group);
                    }

                    serviceEndpoint.Name = selectedEndpoint.Contract;
                }
            }
            return serviceEndpoint;
        }

        /// <summary>
        /// Configures the binding for the selected endpoint
        /// </summary>
       /// <param name="serviceEndPoint"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        private static Binding CreateBinding(ChannelEndpointElement serviceEndPoint, ServiceModelSectionGroup group)
        {
            var bindingElementCollection = group.Bindings[serviceEndPoint.Binding];
            if (bindingElementCollection.ConfiguredBindings.Count > 0)
            {
                var be = bindingElementCollection.ConfiguredBindings[0];

                var binding = GetBinding(be);
                if (be != null)
                {
                    be.ApplyConfiguration(binding);
                }

                return binding;
            }

            return null;
        }

        /// <summary>
        /// Helper method to create the right binding depending on the configuration element
        /// </summary>
        /// <param name="configurationElement"></param>
        /// <returns></returns>
        private static Binding GetBinding(IBindingConfigurationElement configurationElement)
        {
            if (configurationElement is CustomBindingElement)
                return new CustomBinding();
            if (configurationElement is BasicHttpBindingElement)
                return new BasicHttpBinding();
            if (configurationElement is NetMsmqBindingElement)
                return new NetMsmqBinding();
            if (configurationElement is NetNamedPipeBindingElement)
                return new NetNamedPipeBinding();
            if (configurationElement is NetPeerTcpBindingElement)
                return new NetPeerTcpBinding();
            if (configurationElement is NetTcpBindingElement)
                return new NetTcpBinding();
            if (configurationElement is WSDualHttpBindingElement)
                return new WSDualHttpBinding();
            if (configurationElement is WSHttpBindingElement)
                return new WSHttpBinding();
            if (configurationElement is WSFederationHttpBindingElement)
                return new WSFederationHttpBinding();

            return null;
        }

        /// <summary>
        /// Adds the configured behavior to the selected endpoint
        /// </summary>
        /// <param name="behaviorConfiguration"></param>
        /// <param name="serviceEndpoint"></param>
        /// <param name="group"></param>
        private static void AddBehaviors(string behaviorConfiguration, ServiceEndpoint serviceEndpoint, ServiceModelSectionGroup group)
        {
            if (behaviorConfiguration == "")
                return;
            var behaviorElement = group.Behaviors.EndpointBehaviors[behaviorConfiguration];
            for (var i = 0; i < behaviorElement.Count; i++)
            {
                var behaviorExtension = behaviorElement[i];
                var extension = behaviorExtension.GetType().InvokeMember("CreateBehavior",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, behaviorExtension, null);
                if (extension != null)
                {
                    serviceEndpoint.Behaviors.Add((IEndpointBehavior)extension);
                }
            }
        }

        /// <summary>
        /// Gets the endpoint identity from the configuration file
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static EndpointIdentity GetIdentity(IdentityElement element)
        {
            const EndpointIdentity identity = null;
            var properties = element.ElementInformation.Properties;
            if (properties != null)
            {
                if (properties["userPrincipalName"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    return EndpointIdentity.CreateUpnIdentity(element.UserPrincipalName.Value);
                }
                if (properties["servicePrincipalName"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    return EndpointIdentity.CreateSpnIdentity(element.ServicePrincipalName.Value);
                }
                if (properties["dns"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    return EndpointIdentity.CreateDnsIdentity(element.Dns.Value);
                }
                if (properties["rsa"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    return EndpointIdentity.CreateRsaIdentity(element.Rsa.Value);
                }
                if (properties["certificate"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    var supportingCertificates = new X509Certificate2Collection();
                    supportingCertificates.Import(Convert.FromBase64String(element.Certificate.EncodedValue));
                    if (supportingCertificates.Count == 0)
                    {
                        throw new InvalidOperationException("UnableToLoadCertificateIdentity");
                    }
                    var primaryCertificate = supportingCertificates[0];
                    supportingCertificates.RemoveAt(0);
                    return EndpointIdentity.CreateX509CertificateIdentity(primaryCertificate, supportingCertificates);
                }
            }

            return identity;
        }

        protected override void ApplyConfiguration(string configurationName)
        {
           
        }
    }
}
