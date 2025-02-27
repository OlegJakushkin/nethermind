// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Nethermind.Api;
using Nethermind.Consensus.Processing;
using Nethermind.Core.Collections;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.KeyStore.Config;
using Nethermind.Network.Config;
using Nethermind.Network.Discovery;
using Nethermind.Stats;
using NUnit.Framework;

namespace Nethermind.Config.Test
{
    [TestFixture]
    public class JsonConfigProviderTests
    {
        private JsonConfigProvider _configProvider = null!;

        [SetUp]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public void Initialize()
        {
            KeyStoreConfig keystoreConfig = new();
            NetworkConfig networkConfig = new();
            JsonRpcConfig jsonRpcConfig = new();
            StatsParameters statsConfig = StatsParameters.Instance;

            _configProvider = new JsonConfigProvider("SampleJson/SampleJsonConfig.cfg");
        }

        [TestCase(12ul, typeof(BlocksConfig), nameof(BlocksConfig.SecondsPerSlot))]
        [TestCase(false, typeof(BlocksConfig), nameof(BlocksConfig.RandomizedBlocks))]
        [TestCase("chainspec/foundation.json", typeof(InitConfig), nameof(InitConfig.ChainSpecPath))]
        [TestCase(DumpOptions.Receipts, typeof(InitConfig), nameof(InitConfig.AutoDump))]
        public void Test_getDefaultValue<T>(T expected, Type type, string propName)
        {
            IConfig config = Activator.CreateInstance(type) as IConfig ?? throw new Exception("type is not IConfig");
            T actual = config.GetDefaultValue<T>(propName);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Provides_helpful_error_message_when_file_does_not_exist()
        {
            Assert.Throws<IOException>(() => _configProvider = new JsonConfigProvider("SampleJson.cfg"));
        }

        [Test]
        public void Can_load_config_from_file()
        {
            IKeyStoreConfig? keystoreConfig = _configProvider.GetConfig<IKeyStoreConfig>();
            IDiscoveryConfig? networkConfig = _configProvider.GetConfig<IDiscoveryConfig>();
            IJsonRpcConfig? jsonRpcConfig = _configProvider.GetConfig<IJsonRpcConfig>();

            Assert.AreEqual(100, keystoreConfig.KdfparamsDklen);
            Assert.AreEqual("test", keystoreConfig.Cipher);

            Assert.AreEqual(2, jsonRpcConfig.EnabledModules.Count());

            void CheckIfEnabled(string x)
            {
                Assert.IsTrue(jsonRpcConfig.EnabledModules.Contains(x));
            }

            new[] { ModuleType.Eth, ModuleType.Debug }.ForEach(CheckIfEnabled);

            Assert.AreEqual(4, networkConfig.Concurrency);
        }

        [Test]
        public void Can_load_raw_value()
        {
            Assert.AreEqual("100", _configProvider.GetRawValue("KeyStoreConfig", "KdfparamsDklen"));
        }
    }
}
