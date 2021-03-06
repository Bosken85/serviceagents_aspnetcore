﻿using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Digipolis.ServiceAgents.Settings;
using Digipolis.ServiceAgents.UnitTests.Utilities;
using Xunit;
using Digipolis.Errors.Exceptions;
using Digipolis.ServiceAgents.OAuth;
using Digipolis.ServiceAgents.Models;

namespace Digipolis.ServiceAgents.UnitTests.HttpClientFactoryTests
{
    public class HttpClientFactoryTests
    {
        [Fact]
        public void CreateDefaultClient()
        {
            var serviceAgentSettings = new ServiceAgentSettings();
            var settings = new ServiceSettings { Scheme = HttpSchema.Http, Host = "test.be", Path = "api"};
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("http://test.be/api/", client.BaseAddress.AbsoluteUri);
            Assert.Equal("application/json", client.DefaultRequestHeaders.Accept.Single().MediaType);
            Assert.Null(client.DefaultRequestHeaders.Authorization);
        }

        [Fact]
        public void CreateClientWithBearerAuth()
        {
            var serviceAgentSettings = new ServiceAgentSettings();
            var settings = new ServiceSettings {  AuthScheme = AuthScheme.Bearer, Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("http://test.be/api/", client.BaseAddress.AbsoluteUri);
            Assert.Equal("application/json", client.DefaultRequestHeaders.Accept.Single().MediaType);
            Assert.Equal(AuthScheme.Bearer, client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal("TokenValue", client.DefaultRequestHeaders.Authorization.Parameter);
        }

        [Fact]
        public void CreateClientWithLocalApiKeyByDefault()
        {
            var serviceAgentSettings = new ServiceAgentSettings();
            var settings = new ServiceSettings { AuthScheme = AuthScheme.ApiKey, ApiKey = "localapikey", Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("localapikey", client.DefaultRequestHeaders.First(h => h.Key == AuthScheme.ApiKey).Value.First());
        }

        [Fact]
        public void CreateClientWithCustomApiKeyHeader()
        {
            var serviceAgentSettings = new ServiceAgentSettings();
            var settings = new ServiceSettings { AuthScheme = AuthScheme.ApiKey, ApiKeyHeaderName = "api-key", ApiKey = "localapikey", Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("localapikey", client.DefaultRequestHeaders.First(h => h.Key == "api-key").Value.First());
        }

        [Fact]
        public void CreateClientWithGlobalApiKey()
        {
            var serviceAgentSettings = new ServiceAgentSettings { GlobalApiKey = "globalapikey" };
            var settings = new ServiceSettings { AuthScheme = AuthScheme.ApiKey, ApiKey = "localapikey", UseGlobalApiKey = true, Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("globalapikey", client.DefaultRequestHeaders.First(h => h.Key == AuthScheme.ApiKey).Value.First());
        }

        [Fact]
        public void CreateClientWithGlobalApiKeyWithCustomHeader()
        {
            var serviceAgentSettings = new ServiceAgentSettings { GlobalApiKey = "globalapikey" };
            var settings = new ServiceSettings { AuthScheme = AuthScheme.ApiKey, ApiKeyHeaderName = "api-key", ApiKey = "localapikey", UseGlobalApiKey = true, Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal("globalapikey", client.DefaultRequestHeaders.First(h => h.Key == "api-key").Value.First());
        }

        [Fact]
        public void CreateClientWithBasicAuthentication()
        {
            var serviceAgentSettings = new ServiceAgentSettings {  };
            var settings = new ServiceSettings { AuthScheme = AuthScheme.Basic, BasicAuthUserName = "Aladdin", BasicAuthPassword = "OpenSesame", Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal(AuthScheme.Basic, client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal("QWxhZGRpbjpPcGVuU2VzYW1l", client.DefaultRequestHeaders.Authorization.Parameter);
        }

        [Fact]
        public void CreateClientWithOAuthClientCredentials()
        {
            var serviceAgentSettings = new ServiceAgentSettings { };
            var settings = new ServiceSettings { AuthScheme = AuthScheme.OAuthClientCredentials, OAuthClientId = "clientId", OAuthClientSecret = "clientSecret", Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            var client = clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(client);
            Assert.Equal(AuthScheme.Bearer, client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal("AccessToken", client.DefaultRequestHeaders.Authorization.Parameter);
        }

        [Fact]
        public void ThrowExceptionWhenNonHttpsSchemeUsedWithBasicAuthentication()
        {
            var serviceAgentSettings = new ServiceAgentSettings { };
            var settings = new ServiceSettings { AuthScheme = AuthScheme.Basic, BasicAuthUserName = "Aladdin", BasicAuthPassword = "OpenSesame", Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));

            Assert.Throws<ServiceAgentException>(() => clientFactory.CreateClient(serviceAgentSettings, settings));
        }

        [Fact]
        public void AfterClientCreatedGetsRaised()
        {
            var serviceAgentSettings = new ServiceAgentSettings();
            var settings = new ServiceSettings { AuthScheme = AuthScheme.Bearer, Scheme = HttpSchema.Http, Host = "test.be", Path = "api" };
            var clientFactory = new HttpClientFactory(CreateServiceProvider(settings));
            HttpClient passedClient = null;
            clientFactory.AfterClientCreated += (sp,c) => passedClient = c;

            clientFactory.CreateClient(serviceAgentSettings, settings);

            Assert.NotNull(passedClient);
        }

        private IServiceProvider CreateServiceProvider(ServiceSettings settings)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();

            if (settings != null)
                serviceProviderMock.Setup(p => p.GetService(typeof(IOptions<ServiceSettings>))).Returns(Options.Create(settings));

            var authContextMock = new Mock<IAuthContext>();
            authContextMock.Setup(c => c.UserToken).Returns("TokenValue");

            serviceProviderMock.Setup(p => p.GetService(typeof(IAuthContext))).Returns(authContextMock.Object);

            var mockTokenHelper = new Mock<ITokenHelper>();
            mockTokenHelper.Setup(h => h.ReadOrRetrieveToken(settings))
                .ReturnsAsync(new TokenReply {  access_token = "AccessToken", expires_in = 7200 });

            serviceProviderMock.Setup(p => p.GetService(typeof(ITokenHelper))).Returns(mockTokenHelper.Object);

            return serviceProviderMock.Object;
        }
    }
}
